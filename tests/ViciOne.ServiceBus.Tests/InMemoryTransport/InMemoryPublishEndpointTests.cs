using System.Collections.Concurrent;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryPublishEndpointTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-OVERLOADS", "seven-declared-message-and-context-paths")]
    public async Task EveryPublishOverload_DeliversItsDeclaredMessageAndContextExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"publish-overloads-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var recorder = new PublishRecorder(expectedCount: 7);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Handler<ConcretePublished>(recorder.RecordConcreteAsync);
            endpoint.Handler<DynamicPublished>(recorder.RecordDynamicAsync);
        };
        Guid basePipeRequestId = Guid.Parse("8efbc787-5190-46da-b927-f5a8f992457c");
        Guid typedPipeRequestId = Guid.Parse("cf45d634-89ba-46b4-96ab-923b42dab3ad");
        Guid objectCallbackRequestId = Guid.Parse("82695ea0-de1b-48cc-9107-83806df8f801");
        Guid dynamicCallbackRequestId = Guid.Parse("57b5c7e5-3564-47da-aa70-c60436f5f7f1");
        bool started = false;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;

            await harness.Bus.PublishAsync(new ConcretePublished(1), cancellationToken);

            object boxed = new ConcretePublished(2);
            await harness.Bus.Advanced().PublishAsync(boxed, boxed.GetType(), cancellationToken);

            await harness.Bus.PublishAsync(
                new ConcretePublished(3),
                Pipe.New<PublishContext>(pipe =>
                    pipe.UseExecute(context => context.RequestId = basePipeRequestId)),
                cancellationToken);

            await harness.Bus.PublishAsync(
                new ConcretePublished(4),
                Pipe.New<PublishContext<ConcretePublished>>(pipe =>
                    pipe.UseExecute(context => context.RequestId = typedPipeRequestId)),
                cancellationToken);

            object boxedWithContext = new ConcretePublished(5);
            await harness.Bus.PublishAsync(
                boxedWithContext,
                context => { context.RequestId = objectCallbackRequestId; },
                cancellationToken);

            await harness.Bus.PublishAsync<DynamicPublished>(new { Sequence = 6 }, cancellationToken);
            await harness.Bus.PublishAsync<DynamicPublished>(
                new { Sequence = 7 },
                context => { context.RequestId = dynamicCallbackRequestId; },
                cancellationToken);

            await recorder.Completed.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            started = false;

            PublishObservation[] observations = recorder.Observations;
            Assert.Equal(7, observations.Length);
            Assert.Equal(Enumerable.Range(1, 7), observations.Select(x => x.Sequence).Order());
            Assert.Equal("concrete", Assert.Single(observations, x => x.Sequence == 1).Contract);
            Assert.Equal("concrete", Assert.Single(observations, x => x.Sequence == 2).Contract);
            Assert.Null(Assert.Single(observations, x => x.Sequence == 1).RequestId);
            Assert.Null(Assert.Single(observations, x => x.Sequence == 2).RequestId);
            Assert.Equal(basePipeRequestId, Assert.Single(observations, x => x.Sequence == 3).RequestId);
            Assert.Equal(typedPipeRequestId, Assert.Single(observations, x => x.Sequence == 4).RequestId);
            Assert.Equal(objectCallbackRequestId, Assert.Single(observations, x => x.Sequence == 5).RequestId);
            Assert.Equal("dynamic", Assert.Single(observations, x => x.Sequence == 6).Contract);
            Assert.Null(Assert.Single(observations, x => x.Sequence == 6).RequestId);
            Assert.Equal("dynamic", Assert.Single(observations, x => x.Sequence == 7).Contract);
            Assert.Equal(dynamicCallbackRequestId, Assert.Single(observations, x => x.Sequence == 7).RequestId);
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

    private sealed record ConcretePublished(int Sequence);

    public interface DynamicPublished
    {
        int Sequence { get; }
    }

    private sealed record PublishObservation(int Sequence, Guid? RequestId, string Contract);

    private sealed class PublishRecorder(int expectedCount)
    {
        private readonly TaskCompletionSource<bool> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<PublishObservation> _observations = new();
        private int _count;

        public Task Completed => _completed.Task;

        public PublishObservation[] Observations => _observations.ToArray();

        public Task RecordConcreteAsync(ConsumeContext<ConcretePublished> context) =>
            RecordAsync(new PublishObservation(context.Message.Sequence, context.RequestId, "concrete"));

        public Task RecordDynamicAsync(ConsumeContext<DynamicPublished> context) =>
            RecordAsync(new PublishObservation(context.Message.Sequence, context.RequestId, "dynamic"));

        private Task RecordAsync(PublishObservation observation)
        {
            _observations.Enqueue(observation);
            if (Interlocked.Increment(ref _count) == expectedCount)
                _completed.TrySetResult(true);

            return Task.CompletedTask;
        }
    }
}
