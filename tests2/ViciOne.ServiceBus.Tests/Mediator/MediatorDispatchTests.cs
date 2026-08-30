using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorDispatchTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "dynamic-handler-connect-disconnect")]
    public async Task DynamicHandler_ReceivesBeforeDisconnectAndNotAfterDisconnect()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = Bus.Factory.CreateMediator(_ => { });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        var received = 0;
        var first = new DispatchMessage("first");
        var second = new DispatchMessage("second");

        using (ConnectHandle handle = mediator.ConnectHandler<DispatchMessage>(context =>
               {
                   Assert.Same(first, context.Message);
                   Interlocked.Increment(ref received);
                   return Task.CompletedTask;
               }))
        {
            await mediator.Publish(first, cancellationToken).WaitAsync(timeout, cancellationToken);
            Assert.Equal(1, Volatile.Read(ref received));
        }

        await mediator.Publish(second, cancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(1, Volatile.Read(ref received));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-and-publish-exactly-once")]
    public async Task SendAndPublish_DeliverTheirExactMessagesExactlyOnce()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sentCount = 0;
        var publishedCount = 0;
        var sent = new SentMessage(NewId.NextGuid());
        var published = new PublishedMessage(NewId.NextGuid());
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Handler<SentMessage>(context =>
            {
                Assert.Same(sent, context.Message);
                Interlocked.Increment(ref sentCount);
                return Task.CompletedTask;
            });
            configurator.Handler<PublishedMessage>(context =>
            {
                Assert.Same(published, context.Message);
                Interlocked.Increment(ref publishedCount);
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        await mediator.Send(sent, cancellationToken).WaitAsync(timeout, cancellationToken);
        await mediator.Publish(published, cancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(1, Volatile.Read(ref sentCount));
        Assert.Equal(1, Volatile.Read(ref publishedCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-original-exception")]
    public async Task Send_PropagatesTheOriginalHandlerException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new MediatorDispatchException("handler failed");
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
            configurator.Handler<DispatchMessage>(_ => throw expected));
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        MediatorDispatchException actual = await Assert.ThrowsAsync<MediatorDispatchException>(() =>
            mediator.Send(new DispatchMessage("fault"), cancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-caller-cancellation")]
    public async Task Send_PropagatesRequestedCallerCancellation()
    {
        TimeSpan timeout = OperationTimeout();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var entered = NewSignal();
        var never = NewSignal();
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
            configurator.Handler<DispatchMessage>(async context =>
            {
                entered.TrySetResult();
                await never.Task.WaitAsync(context.CancellationToken);
            }));
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        Task send = mediator.Send(new DispatchMessage("cancel"), source.Token);
        await entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        Assert.Equal(source.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "request-caller-cancellation")]
    public async Task Request_PropagatesRequestedCallerCancellation()
    {
        TimeSpan timeout = OperationTimeout();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var entered = NewSignal();
        var never = NewSignal();
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
            configurator.Handler<RequestMessage>(async context =>
            {
                entered.TrySetResult();
                await never.Task.WaitAsync(context.CancellationToken);
                await context.RespondAsync(new ResponseMessage(context.Message.CorrelationId));
            }));
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        IRequestClient<RequestMessage> client = mediator.CreateRequestClient<RequestMessage>(RequestTimeout.After(m: 1));
        var request = new RequestMessage(NewId.NextGuid());

        Task<Response<ResponseMessage>> response = client.GetResponse<ResponseMessage>(request, source.Token);
        await entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => response);
        Assert.True(source.IsCancellationRequested);
        Assert.True(response.IsCanceled);
        Assert.True(exception.CancellationToken.CanBeCanceled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "missing-consumer-mandatory-boundary")]
    public async Task PublishWithoutConsumer_UsesTheMandatoryBoundary(bool mandatory)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = Bus.Factory.CreateMediator(_ => { });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        Task publish = mediator.Publish(
            new DispatchMessage(mandatory ? "mandatory" : "optional"),
            context => { context.Mandatory = mandatory; },
            cancellationToken);

        if (mandatory)
        {
            MessageNotConsumedException exception =
                await Assert.ThrowsAsync<MessageNotConsumedException>(() => publish);
            Assert.Contains("not consumed", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            await publish;
            Assert.True(publish.IsCompletedSuccessfully);
        }
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record DispatchMessage(string Value);

    private sealed record SentMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record PublishedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record RequestMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record ResponseMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class MediatorDispatchException(string message) : Exception(message);
}
