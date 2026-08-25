using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class TelemetryMonitorTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "provider-backed-idle-completion")]
    public async Task PublishWait_CompletesAtTheProviderBackedIdleDeadlineAfterTheReceiveFinishes()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan idleTimeout = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var receiveCompleted = new ReceiveCompletionObserver(timeProvider);
        using var harness = new InMemoryTestHarness($"telemetry-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        harness.Consumer<MonitoredConsumer>();

        await harness.Start(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(receiveCompleted);
        try
        {
            Task wait = harness.Bus.Wait(
                endpoint => endpoint.Publish(new MonitoredMessage(NewId.NextGuid()), TestContext.Current.CancellationToken),
                TimeSpan.FromMinutes(10),
                idleTimeout,
                timeProvider);

            await timeProvider.WaitForTimerCount(1).WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            await receiveCompleted.IdleTimerArmedAfterReceive.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.False(wait.IsCompleted);

            timeProvider.Advance(idleTimeout);

            await wait.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            IReceivedMessage<MonitoredMessage> consumed = await harness.Consumed
                .SelectAsync<MonitoredMessage>(TestContext.Current.CancellationToken)
                .First();
            Assert.Null(consumed.Exception);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "callback-failure-is-immediate")]
    public async Task PublishWait_PropagatesCallbackFailureWithoutWaitingForTheMonitoringTimeout()
    {
        TimeSpan operationTimeout = OperationTimeout();
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness($"telemetry-fault-{NewId.NextGuid():N}");

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            ExpectedCallbackException exception = await Assert.ThrowsAsync<ExpectedCallbackException>(() =>
                harness.Bus.Wait(
                    _ => Task.FromException(new ExpectedCallbackException("callback failed")),
                    TimeSpan.FromDays(1),
                    TimeSpan.FromDays(1),
                    timeProvider).WaitAsync(operationTimeout, TestContext.Current.CancellationToken));

            Assert.Equal("callback failed", exception.Message);
            Assert.Equal(StartTime, timeProvider.GetUtcNow());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "send-waits-for-consumption")]
    public async Task SendWait_CompletesOnlyAfterTheSentMessageHasBeenConsumedAndTheTraceIsIdle()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan idleTimeout = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var receiveCompleted = new ReceiveCompletionObserver(timeProvider);
        using var harness = new InMemoryTestHarness($"telemetry-send-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        ConsumerTestHarness<MonitoredConsumer> consumer = harness.Consumer<MonitoredConsumer>();

        await harness.Start(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(receiveCompleted);
        try
        {
            Task wait = harness.InputQueueSendEndpoint.Wait(
                endpoint => endpoint.Send(new MonitoredMessage(NewId.NextGuid()), TestContext.Current.CancellationToken),
                TimeSpan.FromMinutes(10),
                idleTimeout,
                timeProvider);

            await timeProvider.WaitForTimerCount(1).WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            await receiveCompleted.IdleTimerArmedAfterReceive.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.False(wait.IsCompleted);

            timeProvider.Advance(idleTimeout);

            await wait.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            IReceivedMessage<MonitoredMessage> consumed = await consumer.Consumed
                .SelectAsync<MonitoredMessage>(TestContext.Current.CancellationToken)
                .First();
            Assert.Null(consumed.Exception);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "request-waits-for-response-and-produced-messages")]
    public async Task RequestWait_ReturnsTheExactResponseOnlyAfterTheCompleteRequestTraceIsIdle()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan idleTimeout = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var callbackCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"telemetry-request-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        harness.Consumer<MonitoredRequestConsumer>();

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            IRequestClient<MonitoredRequest> client = harness.CreateRequestClient<MonitoredRequest>();
            Task<Response<MonitoredResponse>> wait = client.Wait(
                async requestClient =>
                {
                    Response<MonitoredResponse> response = await requestClient.GetResponse<MonitoredResponse>(
                        new MonitoredRequest(correlationId),
                        TestContext.Current.CancellationToken);
                    callbackCompleted.TrySetResult(true);
                    return response;
                },
                TimeSpan.FromMinutes(10),
                idleTimeout,
                timeProvider);

            await timeProvider.WaitForTimerCount(1).WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            await callbackCompleted.Task.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.False(wait.IsCompleted);
            Assert.True(await harness.Published.Any<MonitoredRequestHandled>(TestContext.Current.CancellationToken));

            timeProvider.Advance(idleTimeout);

            Response<MonitoredResponse> response = await wait.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.True(await harness.Consumed.Any<MonitoredRequest>(TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record MonitoredMessage(Guid CorrelationId);

    private sealed record MonitoredRequest(Guid CorrelationId);

    private sealed record MonitoredResponse(Guid CorrelationId);

    private sealed record MonitoredRequestHandled(Guid CorrelationId);

    private sealed class MonitoredConsumer : IConsumer<MonitoredMessage>
    {
        public Task Consume(ConsumeContext<MonitoredMessage> context) => Task.CompletedTask;
    }

    private sealed class MonitoredRequestConsumer : IConsumer<MonitoredRequest>
    {
        public async Task Consume(ConsumeContext<MonitoredRequest> context)
        {
            await context.Publish(
                new MonitoredRequestHandled(context.Message.CorrelationId),
                context.CancellationToken);
            await context.RespondAsync(new MonitoredResponse(context.Message.CorrelationId));
        }
    }

    private sealed class ReceiveCompletionObserver(ObservableTimeProvider timeProvider) : IReceiveObserver
    {
        private readonly TaskCompletionSource<Task> _idleTimerArmedAfterReceive =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task IdleTimerArmedAfterReceive => _idleTimerArmedAfterReceive.Task.Unwrap();

        public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceive(ReceiveContext context)
        {
            Task nextTimerChange = timeProvider.WaitForChangeCount(timeProvider.ChangeCount + 1);
            _idleTimerArmedAfterReceive.TrySetResult(nextTimerChange);
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            _idleTimerArmedAfterReceive.TrySetException(exception);
            return Task.CompletedTask;
        }
    }

    private sealed class ExpectedCallbackException(string message) : Exception(message);
}
