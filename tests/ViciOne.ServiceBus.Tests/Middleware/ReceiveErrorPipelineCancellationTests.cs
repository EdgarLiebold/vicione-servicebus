using System.Net.Mime;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class ReceiveErrorPipelineCancellationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "receive-fault-publication-observes-delivery-cancellation")]
    public async Task ReceiveFaultPublication_UsesDeliveryCancellationAtEveryStageAsync(int cancelStage)
    {
        using var delivery = new CancellationTokenSource();
        if (cancelStage == 0)
            await delivery.CancelAsync();

        var receiveFailure = new InvalidOperationException("invalid envelope");
        var resolverCalls = 0;
        var sendCalls = 0;
        var notificationCalls = 0;
        var downstreamCalls = 0;
        ISendEndpoint endpoint = RethrowErrorTransportFilterTests.StrictProxy.Create<ISendEndpoint>((method, args) => method.Name switch
        {
            "SendAsync" => SendFault(args),
            _ => throw new NotSupportedException(method.Name),
        });
        IPublishEndpointProvider publisher =
            RethrowErrorTransportFilterTests.StrictProxy.Create<IPublishEndpointProvider>((method, args) => method.Name switch
            {
                "GetPublishSendEndpointAsync" => ResolveFaultEndpoint(args),
                _ => throw new NotSupportedException(method.Name),
            });
        ExceptionReceiveContext context =
            RethrowErrorTransportFilterTests.StrictProxy.Create<ExceptionReceiveContext>((method, args) => method.Name switch
            {
                "get_CancellationToken" => delivery.Token,
                "get_IsFaulted" => false,
                "get_Exception" => receiveFailure,
                "get_PublishFaults" => true,
                "get_TransportHeaders" => EmptyHeaders.Instance,
                "get_ContentType" => new ContentType("application/json"),
                "get_PublishEndpointProvider" => publisher,
                "TryGetPayload" => NoPayload(args),
                "NotifyFaultedAsync" => NotifyFaulted(args),
                _ => throw new NotSupportedException(method.Name),
            });
        var filter = new GenerateFaultFilter();
        IPipe<ExceptionReceiveContext> next = Pipe.Execute<ExceptionReceiveContext>(_ => downstreamCalls++);

        if (cancelStage is 0 or 1)
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => filter.SendAsync(context, next));
            Assert.Equal(delivery.Token, actual.CancellationToken);
            Assert.Equal(0, notificationCalls);
            Assert.Equal(0, downstreamCalls);
        }
        else
        {
            await filter.SendAsync(context, next);
            Assert.Equal(1, notificationCalls);
            Assert.Equal(1, downstreamCalls);
        }

        Assert.Equal(cancelStage == 0 ? 0 : 1, resolverCalls);
        Assert.Equal(cancelStage == 0 ? 0 : 1, sendCalls);

        Task<ISendEndpoint> ResolveFaultEndpoint(object?[]? args)
        {
            resolverCalls++;
            Assert.Equal(delivery.Token, Assert.IsType<CancellationToken>(args![0]));
            if (cancelStage == 1)
                delivery.Cancel();
            return Task.FromResult(endpoint);
        }

        Task SendFault(object?[]? args)
        {
            sendCalls++;
            ReceiveFault fault = Assert.IsAssignableFrom<ReceiveFault>(args![0]);
            Assert.Null(fault.FaultedMessageId);
            CancellationToken received = Assert.IsType<CancellationToken>(args[1]);
            Assert.Equal(delivery.Token, received);
            return received.IsCancellationRequested ? Task.FromCanceled(received) : Task.CompletedTask;
        }

        Task NotifyFaulted(object?[]? args)
        {
            notificationCalls++;
            Assert.Same(receiveFailure, args![0]);
            Assert.Equal(delivery.Token, Assert.IsType<CancellationToken>(args[1]));
            return Task.CompletedTask;
        }

        static bool NoPayload(object?[]? args)
        {
            args![0] = null;
            return false;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "already-faulted-receive-still-rejects-cancellation")]
    public async Task AlreadyFaultedReceive_RejectsCancellationBeforeContinuingAsync()
    {
        using var delivery = new CancellationTokenSource();
        await delivery.CancelAsync();
        var downstreamCalls = 0;
        ExceptionReceiveContext context = RethrowErrorTransportFilterTests.StrictProxy.Create<ExceptionReceiveContext>((method, _) =>
            method.Name switch
            {
                "get_CancellationToken" => delivery.Token,
                "get_IsFaulted" => true,
                _ => throw new NotSupportedException(method.Name),
            });
        IPipe<ExceptionReceiveContext> next = Pipe.Execute<ExceptionReceiveContext>(_ => downstreamCalls++);

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GenerateFaultFilter().SendAsync(context, next));

        Assert.Equal(delivery.Token, actual.CancellationToken);
        Assert.Equal(0, downstreamCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "error-transport-observes-delivery-cancellation")]
    public async Task ErrorTransport_UsesDeliveryCancellationBeforeMovingTheMessageAsync(int cancelStage)
    {
        using var delivery = new CancellationTokenSource();
        if (cancelStage == 0)
            await delivery.CancelAsync();

        var lookupCalls = 0;
        var transportCalls = 0;
        var downstreamCalls = 0;
        ExceptionReceiveContext context = null!;
        IErrorTransport transport = RethrowErrorTransportFilterTests.StrictProxy.Create<IErrorTransport>((method, args) => method.Name switch
        {
            "SendAsync" => Move(args),
            _ => throw new NotSupportedException(method.Name),
        });
        context = RethrowErrorTransportFilterTests.StrictProxy.Create<ExceptionReceiveContext>((method, args) => method.Name switch
        {
            "TryGetPayload" => GetTransport(args),
            "get_CancellationToken" => delivery.Token,
            _ => throw new NotSupportedException(method.Name),
        });
        IFilter<ExceptionReceiveContext> filter = new ErrorTransportFilter();
        IPipe<ExceptionReceiveContext> next = Pipe.Execute<ExceptionReceiveContext>(_ => downstreamCalls++);

        if (cancelStage is 0 or 1)
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => filter.SendAsync(context, next));
            Assert.Equal(delivery.Token, actual.CancellationToken);
            Assert.Equal(0, downstreamCalls);
        }
        else
        {
            await filter.SendAsync(context, next);
            Assert.Equal(1, downstreamCalls);
        }
        Assert.Equal(cancelStage == 0 ? 0 : 1, lookupCalls);
        Assert.Equal(cancelStage == 0 ? 0 : 1, transportCalls);

        Task Move(object?[]? args)
        {
            transportCalls++;
            Assert.Same(context, args![0]);
            CancellationToken received = Assert.IsType<CancellationToken>(args[1]);
            Assert.Equal(delivery.Token, received);
            return received.IsCancellationRequested ? Task.FromCanceled(received) : Task.CompletedTask;
        }

        bool GetTransport(object?[]? args)
        {
            lookupCalls++;
            args![0] = transport;
            if (cancelStage == 1)
                delivery.Cancel();
            return true;
        }
    }
}
