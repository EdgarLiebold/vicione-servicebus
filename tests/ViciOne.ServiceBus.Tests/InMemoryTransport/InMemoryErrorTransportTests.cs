using System.Runtime.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryErrorTransportTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ERROR-TRANSPORT", "complete-envelope-moves-once")]
    public async Task SerializationFailure_MovesOneCompleteEnvelopeToTheErrorQueueAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("error-move", timeout);
        var moved = new TaskCompletionSource<ConsumeContext<ErrorMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var movedCount = 0;
        string errorQueueName = $"{harness.InputQueueName}_error";
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Handler<ErrorMessage>(_ => Task.FromException(new SerializationException("intentional error move")));
        harness.InMemoryBusConfiguring += configurator =>
            configurator.ReceiveEndpoint(errorQueueName, endpoint => endpoint.Handler<ErrorMessage>(context =>
            {
                Interlocked.Increment(ref movedCount);
                moved.TrySetResult(context);
                return Task.CompletedTask;
            }));
        Guid correlationId = Guid.Parse("60f7a13b-e346-4c85-96b5-c6a91a207684");
        Uri? destinationAddress = null;
        Uri? busAddress = null;
        bool started = false;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            busAddress = harness.BusAddress;
            await harness.InputQueueSendEndpoint.SendAsync(
                    new ErrorMessage(correlationId),
                    context =>
                    {
                        context.CorrelationId = correlationId;
                        destinationAddress = context.DestinationAddress;
                        context.ResponseAddress = busAddress;
                        context.FaultAddress = busAddress;
                    },
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            ConsumeContext<ErrorMessage> actual = await moved.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.Equal(new Uri(harness.BaseAddress, errorQueueName), actual.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(busAddress, actual.SourceAddress);
            Assert.Equal(harness.InputQueueAddress, Assert.IsType<Uri>(destinationAddress));
            Assert.Equal(harness.InputQueueAddress, actual.DestinationAddress);
            Assert.Equal(busAddress, actual.ResponseAddress);
            Assert.Equal(busAddress, actual.FaultAddress);
            Assert.Equal(correlationId, actual.Message.CorrelationId);
            Assert.Equal(1, Volatile.Read(ref movedCount));
        }
        finally
        {
            if (started)
                await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-FAULT-PUBLICATION", "disabled-retries-and-moves-without-fault")]
    public async Task FaultPublishingDisabled_RetriesAndMovesOnceWithoutPublishingAFaultAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("fault-publication-disabled", timeout);
        var moved = new TaskCompletionSource<ConsumeContext<DisabledFaultMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var movedCount = 0;
        string errorQueueName = $"{harness.InputQueueName}_error";
        var attempts = 0;
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.PublishFaults = false;
            endpoint.UseMessageRetry(retry => retry.Immediate(5));
            endpoint.Handler<DisabledFaultMessage>(_ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromException(new DisabledFaultException());
            });
        };
        harness.InMemoryBusConfiguring += configurator =>
            configurator.ReceiveEndpoint(errorQueueName, endpoint => endpoint.Handler<DisabledFaultMessage>(context =>
            {
                Interlocked.Increment(ref movedCount);
                moved.TrySetResult(context);
                return Task.CompletedTask;
            }));
        var message = new DisabledFaultMessage(Guid.Parse("190764d1-494a-43d5-9ea4-fae23af31492"));
        bool started = false;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken).WaitAsync(timeout, cancellationToken);
            ConsumeContext<DisabledFaultMessage> actual = await moved.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            started = false;

            using var completed = new CancellationTokenSource();
            completed.Cancel();
            IPublishedMessage<Fault<DisabledFaultMessage>>[] faults = harness.Published
                .Select<Fault<DisabledFaultMessage>>(completed.Token)
                .ToArray();

            Assert.Equal(6, Volatile.Read(ref attempts));
            Assert.Equal(1, Volatile.Read(ref movedCount));
            Assert.Equal(message, actual.Message);
            Assert.Equal(new Uri(harness.BaseAddress, errorQueueName), actual.Advanced().ReceiveContext.InputAddress);
            Assert.Empty(faults);
        }
        finally
        {
            if (started)
                await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(string purpose, TimeSpan timeout) =>
        new($"inmemory-{purpose}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed record ErrorMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record DisabledFaultMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class DisabledFaultException : Exception;
}
