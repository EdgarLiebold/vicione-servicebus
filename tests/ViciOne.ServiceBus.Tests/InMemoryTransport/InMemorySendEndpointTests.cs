using System.Collections.Concurrent;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemorySendEndpointTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-OVERLOADS", "seven-runtime-interface-and-context-paths")]
    public async Task EverySendOverload_DeliversItsRuntimeContractAndContextExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"send-overloads-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var recorder = new SendRecorder(expectedCount: 7);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Handler<InterfaceSent>(recorder.RecordInterfaceAsync);
            endpoint.Handler<DynamicSent>(recorder.RecordDynamicAsync);
            endpoint.Handler<ConcreteSent>(recorder.RecordConcreteAsync);
        };
        Guid typedCallbackRequestId = Guid.Parse("19420342-92cf-4f2d-84af-61f7948ca0aa");
        Guid explicitTypeRequestId = Guid.Parse("c6c937c6-b7ef-475f-a534-991249f02b8a");
        Guid objectCallbackRequestId = Guid.Parse("ea81c69b-214e-4eb4-80a7-b24723f23c8f");
        bool started = false;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = harness.InputQueueSendEndpoint;

            await endpoint.SendAsync(new InterfaceConcreteSent(1), cancellationToken);
            await endpoint.SendAsync<DynamicSent>(new { Sequence = 2 }, cancellationToken);

            object runtimeObject = new ConcreteSent(3);
            await endpoint.Advanced().SendAsync(runtimeObject, cancellationToken);
            await endpoint.SendAsync(new ConcreteSent(4), cancellationToken);
            await endpoint.SendAsync(
                new ConcreteSent(5),
                context => { context.RequestId = typedCallbackRequestId; },
                cancellationToken);

            object explicitTypeObject = new ConcreteSent(6);
            await endpoint.Advanced().SendAsync(
                explicitTypeObject,
                typeof(ConcreteSent),
                ((Action<SendContext>)(context => context.RequestId = explicitTypeRequestId)).ToPipe(),
                cancellationToken);

            object callbackObject = new ConcreteSent(7);
            await endpoint.Advanced().SendAsync(
                callbackObject,
                ((Action<SendContext>)(context => context.RequestId = objectCallbackRequestId)).ToPipe(),
                cancellationToken);

            await recorder.Completed.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            started = false;

            SendObservation[] observations = recorder.Observations;
            Assert.Equal(7, observations.Length);
            Assert.Equal(Enumerable.Range(1, 7), observations.Select(x => x.Sequence).Order());
            Assert.Equal("interface", Assert.Single(observations, x => x.Sequence == 1).Contract);
            Assert.Equal("dynamic", Assert.Single(observations, x => x.Sequence == 2).Contract);
            Assert.Null(Assert.Single(observations, x => x.Sequence == 3).RequestId);
            Assert.Null(Assert.Single(observations, x => x.Sequence == 4).RequestId);
            Assert.Equal(typedCallbackRequestId, Assert.Single(observations, x => x.Sequence == 5).RequestId);
            Assert.Equal(explicitTypeRequestId, Assert.Single(observations, x => x.Sequence == 6).RequestId);
            Assert.Equal(objectCallbackRequestId, Assert.Single(observations, x => x.Sequence == 7).RequestId);
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

    public interface InterfaceSent
    {
        int Sequence { get; }
    }

    public interface DynamicSent
    {
        int Sequence { get; }
    }

    private sealed record InterfaceConcreteSent(int Sequence) : InterfaceSent;

    private sealed record ConcreteSent(int Sequence);

    private sealed record SendObservation(int Sequence, Guid? RequestId, string Contract);

    private sealed class SendRecorder(int expectedCount)
    {
        private readonly TaskCompletionSource<bool> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<SendObservation> _observations = new();
        private int _count;

        public Task Completed => _completed.Task;

        public SendObservation[] Observations => _observations.ToArray();

        public Task RecordInterfaceAsync(ConsumeContext<InterfaceSent> context) =>
            RecordAsync(new SendObservation(context.Message.Sequence, context.RequestId, "interface"));

        public Task RecordDynamicAsync(ConsumeContext<DynamicSent> context) =>
            RecordAsync(new SendObservation(context.Message.Sequence, context.RequestId, "dynamic"));

        public Task RecordConcreteAsync(ConsumeContext<ConcreteSent> context) =>
            RecordAsync(new SendObservation(context.Message.Sequence, context.RequestId, "concrete"));

        private Task RecordAsync(SendObservation observation)
        {
            _observations.Enqueue(observation);
            if (Interlocked.Increment(ref _count) == expectedCount)
                _completed.TrySetResult(true);

            return Task.CompletedTask;
        }
    }
}
