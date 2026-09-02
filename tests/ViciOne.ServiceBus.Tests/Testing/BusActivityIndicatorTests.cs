using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class BusActivityIndicatorTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-ACTIVITY", "publish-idle-deadline")]
    public async Task PublishActivity_BecomesIdleOnTheConfiguredClock()
    {
        TimeSpan timeout = OperationTimeout();
        var timeProvider = new FakeTimeProvider(StartTime);
        var signal = new CountingSignalResource();
        using var indicator = new BusActivityPublishIndicator(signal, TimeSpan.FromMinutes(1), timeProvider);
        using var harness = CreateHarness(timeout, timeProvider);

        await harness.Start(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectPublishObserver(indicator);
        try
        {
            await harness.Bus.Publish(new ActivityMessage(), TestContext.Current.CancellationToken);
            Assert.False(indicator.IsMet);

            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await signal.Signaled.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.True(indicator.IsMet);
            Assert.Equal(1, signal.Count);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-ACTIVITY", "send-idle-deadline")]
    public async Task SendActivity_BecomesIdleOnTheConfiguredClock()
    {
        TimeSpan timeout = OperationTimeout();
        var timeProvider = new FakeTimeProvider(StartTime);
        var signal = new CountingSignalResource();
        using var indicator = new BusActivitySendIndicator(signal, TimeSpan.FromMinutes(1), timeProvider);
        using var harness = CreateHarness(timeout, timeProvider);
        harness.Handler<ActivityMessage>();

        await harness.Start(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectSendObserver(indicator);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new ActivityMessage(), TestContext.Current.CancellationToken);
            Assert.False(indicator.IsMet);

            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await signal.Signaled.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.True(indicator.IsMet);
            Assert.Equal(1, signal.Count);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-ACTIVITY", "receive-idle-deadline")]
    public async Task ReceiveActivity_BecomesIdleOnTheConfiguredClock()
    {
        TimeSpan timeout = OperationTimeout();
        var timeProvider = new FakeTimeProvider(StartTime);
        var signal = new CountingSignalResource();
        var receiveCompleted = new ReceiveCompletionObserver();
        using var indicator = new BusActivityReceiveIndicator(signal, TimeSpan.FromMinutes(1), timeProvider);
        using var harness = CreateHarness(timeout, timeProvider);
        HandlerTestHarness<ActivityMessage> handler = harness.Handler<ActivityMessage>();

        await harness.Start(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(indicator);
        using ConnectHandle completionObserver = harness.Bus.ConnectReceiveObserver(receiveCompleted);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new ActivityMessage(), TestContext.Current.CancellationToken);
            Assert.True(await handler.Consumed.Any(TestContext.Current.CancellationToken));
            await receiveCompleted.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.False(indicator.IsMet);

            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await signal.Signaled.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.True(indicator.IsMet);
            Assert.Equal(1, signal.Count);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-ACTIVITY", "consume-in-flight-lifecycle")]
    public async Task ConsumeActivity_RemainsBusyUntilTheConsumerCompletes()
    {
        TimeSpan timeout = OperationTimeout();
        var signal = new CountingSignalResource();
        var consumer = new GatedConsumer();
        var indicator = new BusActivityConsumeIndicator(signal);
        using var harness = new InMemoryTestHarness($"consume-activity-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.Consumer(() => consumer);

        await harness.Start(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectConsumeObserver(indicator);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new GatedMessage(), TestContext.Current.CancellationToken);
            await consumer.Started.WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.False(indicator.IsMet);
            Assert.Equal(0, signal.Count);

            consumer.Release();
            await signal.Signaled.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.True(indicator.IsMet);
            Assert.Equal(1, signal.Count);
            Assert.True(await harness.Consumed.Any<GatedMessage>(TestContext.Current.CancellationToken));
        }
        finally
        {
            consumer.Release();
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-ACTIVITY", "consume-fault-returns-idle")]
    public async Task ConsumeActivity_ReturnsIdleWhenTheConsumerFaults()
    {
        TimeSpan timeout = OperationTimeout();
        var signal = new CountingSignalResource();
        var indicator = new BusActivityConsumeIndicator(signal);
        using var harness = new InMemoryTestHarness($"consume-fault-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.Consumer<FailingConsumer>();

        await harness.Start(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectConsumeObserver(indicator);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new FailingMessage(), TestContext.Current.CancellationToken);
            await signal.Signaled.WaitAsync(timeout, TestContext.Current.CancellationToken);
            IReceivedMessage<FailingMessage> consumed = await harness.Consumed
                .SelectAsync<FailingMessage>(TestContext.Current.CancellationToken)
                .First();

            Assert.True(indicator.IsMet);
            Assert.Equal(1, signal.Count);
            ExpectedConsumerException exception = Assert.IsType<ExpectedConsumerException>(consumed.Exception);
            Assert.Equal("consumer failed", exception.Message);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout, TimeProvider timeProvider) =>
        new(timeProvider, $"activity-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed record ActivityMessage;

    private sealed record GatedMessage;

    private sealed record FailingMessage;

    private sealed class CountingSignalResource : ISignalResource
    {
        private int _count;
        private readonly TaskCompletionSource<bool> _signaled =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Count => Volatile.Read(ref _count);

        public Task Signaled => _signaled.Task;

        public void Signal()
        {
            Interlocked.Increment(ref _count);
            _signaled.TrySetResult(true);
        }
    }

    private sealed class GatedConsumer : IConsumer<GatedMessage>
    {
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public async Task Consume(ConsumeContext<GatedMessage> context)
        {
            _started.TrySetResult(true);
            await _release.Task.WaitAsync(context.CancellationToken);
        }

        public void Release() => _release.TrySetResult(true);
    }

    private sealed class FailingConsumer : IConsumer<FailingMessage>
    {
        public Task Consume(ConsumeContext<FailingMessage> context) =>
            Task.FromException(new ExpectedConsumerException("consumer failed"));
    }

    private sealed class ExpectedConsumerException(string message) : Exception(message);

    private sealed class ReceiveCompletionObserver : IReceiveObserver
    {
        private readonly TaskCompletionSource<bool> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completed => _completed.Task;

        public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceive(ReceiveContext context)
        {
            _completed.TrySetResult(true);
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            _completed.TrySetException(exception);
            return Task.CompletedTask;
        }
    }
}
