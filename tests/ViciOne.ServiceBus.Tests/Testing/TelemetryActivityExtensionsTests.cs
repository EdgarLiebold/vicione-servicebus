using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class TelemetryActivityExtensionsTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "provider-backed-idle-completion")]
    public async Task PublishOperation_CompletesAtTheProviderBackedIdleDeadlineAfterTheReceiveFinishesAsync()
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
        harness.AddConsumer<MonitoredConsumer>();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(receiveCompleted);
        try
        {
            Task wait = harness.Bus.ExecuteAndWaitForIdleAsync(
                endpoint => endpoint.PublishAsync(new MonitoredMessage(NewId.NextGuid()), TestContext.Current.CancellationToken),
                TimeSpan.FromMinutes(10), idleTimeout, timeProvider, cancellationToken: TestContext.Current.CancellationToken);

            await timeProvider.WaitForTimerCountAsync(1).WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            await receiveCompleted.IdleTimerArmedAfterReceive.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(idleTimeout, timeProvider.LastDueTime);
            Assert.False(wait.IsCompleted);

            timeProvider.Advance(idleTimeout - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));

            await wait.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            IConsumedMessage<MonitoredMessage> consumed = await harness.Consumed
                .SelectAsync<MonitoredMessage>(TestContext.Current.CancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(consumed.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "callback-failure-is-immediate")]
    public async Task PublishOperation_PropagatesActionFailureWithoutWaitingForTheMonitoringTimeoutAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness($"telemetry-fault-{NewId.NextGuid():N}");

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            ExpectedCallbackException exception = await Assert.ThrowsAsync<ExpectedCallbackException>(() =>
                harness.Bus.ExecuteAndWaitForIdleAsync(_ => Task.FromException(new ExpectedCallbackException("callback failed")),
                    TimeSpan.FromDays(1), TimeSpan.FromDays(1), timeProvider, cancellationToken: TestContext.Current.CancellationToken)
                    .WaitAsync(operationTimeout, TestContext.Current.CancellationToken));

            Assert.Equal("callback failed", exception.Message);
            Assert.Equal(StartTime, timeProvider.GetUtcNow());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "activity-wait-cancellation")]
    public async Task PublishOperation_CancelsWhileWaitingForTheTraceToBecomeIdleAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var harness = new InMemoryTestHarness($"telemetry-cancel-{NewId.NextGuid():N}");

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            Task wait = harness.Bus.ExecuteAndWaitForIdleAsync(_ => Task.CompletedTask, TimeSpan.FromDays(1), TimeSpan.FromDays(1),
                timeProvider, cancellation.Token);

            await timeProvider.WaitForTimerCountAsync(1).WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.False(wait.IsCompleted);

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait.WaitAsync(operationTimeout, CancellationToken.None));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "argument-validation")]
    public async Task PublishOperation_RejectsAnAbsentActionAtTheApiBoundaryAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness($"telemetry-arguments-{NewId.NextGuid():N}");

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                harness.Bus.ExecuteAndWaitForIdleAsync(null!, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1), timeProvider,
                    TestContext.Current.CancellationToken));

            Assert.Equal("action", exception.ParamName);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(0, 1, "timeout")]
    [InlineData(1, 0, "idleTimeout")]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "timeout-validation")]
    public async Task PublishOperation_RejectsNonPositiveTimeoutsAtTheApiBoundaryAsync(int timeoutMilliseconds,
        int idleTimeoutMilliseconds, string parameterName)
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness($"telemetry-timeout-{NewId.NextGuid():N}");

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                harness.Bus.ExecuteAndWaitForIdleAsync(_ => Task.CompletedTask, TimeSpan.FromMilliseconds(timeoutMilliseconds),
                    TimeSpan.FromMilliseconds(idleTimeoutMilliseconds), timeProvider, TestContext.Current.CancellationToken));

            Assert.Equal(parameterName, exception.ParamName);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "send-waits-for-consumption")]
    public async Task SendOperation_CompletesOnlyAfterTheSentMessageHasBeenConsumedAndTheTraceIsIdleAsync()
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
        ConsumerTestHarness<MonitoredConsumer> consumer = harness.AddConsumer<MonitoredConsumer>();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(receiveCompleted);
        try
        {
            Task wait = harness.InputQueueSendEndpoint.ExecuteAndWaitForIdleAsync(
                endpoint => endpoint.SendAsync(new MonitoredMessage(NewId.NextGuid()), TestContext.Current.CancellationToken),
                TimeSpan.FromMinutes(10), idleTimeout, timeProvider, cancellationToken: TestContext.Current.CancellationToken);

            await timeProvider.WaitForTimerCountAsync(1).WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            await receiveCompleted.IdleTimerArmedAfterReceive.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(idleTimeout, timeProvider.LastDueTime);
            Assert.False(wait.IsCompleted);

            timeProvider.Advance(idleTimeout - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));

            await wait.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            IConsumedMessage<MonitoredMessage> consumed = await consumer.Consumed
                .SelectAsync<MonitoredMessage>(TestContext.Current.CancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(consumed.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "request-waits-for-response-and-produced-messages")]
    public async Task RequestOperation_ReturnsTheExactResponseOnlyAfterTheCompleteRequestTraceIsIdleAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan idleTimeout = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var callbackCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiveCompleted = new ReceiveCompletionObserver(timeProvider);
        using var harness = new InMemoryTestHarness($"telemetry-request-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        harness.AddConsumer<MonitoredRequestConsumer>();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(receiveCompleted);
        try
        {
            Guid correlationId = NewId.NextGuid();
            IRequestClient<MonitoredRequest> client = harness.CreateRequestClient<MonitoredRequest>();
            Task<Response<MonitoredResponse>> wait = client.ExecuteAndWaitForIdleAsync(async requestClient =>
                {
                    Response<MonitoredResponse> response = await requestClient.GetResponseAsync<MonitoredResponse>(
                        new MonitoredRequest(correlationId),
                        TestContext.Current.CancellationToken);
                    callbackCompleted.TrySetResult(true);
                    return response;
                }, TimeSpan.FromMinutes(10), idleTimeout, timeProvider, cancellationToken: TestContext.Current.CancellationToken);

            await timeProvider.WaitForTimerCountAsync(1).WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            await callbackCompleted.Task.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            await receiveCompleted.IdleTimerArmedAfterReceive.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(idleTimeout, timeProvider.LastDueTime);
            Assert.False(wait.IsCompleted);
            Assert.True(await harness.Published.AnyAsync<MonitoredRequestHandled>(TestContext.Current.CancellationToken));

            timeProvider.Advance(idleTimeout - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));

            Response<MonitoredResponse> response = await wait.WaitAsync(operationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.True(await harness.Consumed.AnyAsync<MonitoredRequest>(TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "multiple-response-overloads")]
    public async Task RequestOperation_PreservesTheSelectedBranchForTwoAndThreeResponseOverloadsAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan idleTimeout = TimeSpan.FromMilliseconds(10);
        using var harness = new InMemoryTestHarness($"telemetry-multiple-response-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        harness.AddConsumer<MultipleResponseConsumer>();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            IRequestClient<MultipleResponseRequest> client = harness.CreateRequestClient<MultipleResponseRequest>();
            Response<FirstMultipleResponse, SecondMultipleResponse> twoResponses = await client.ExecuteAndWaitForIdleAsync(
                requestClient => requestClient.Advanced().GetResponseAsync<FirstMultipleResponse, SecondMultipleResponse>(
                    new MultipleResponseRequest(2),
                    cancellationToken: TestContext.Current.CancellationToken),
                operationTimeout,
                idleTimeout,
                TestContext.Current.CancellationToken);
            Response<FirstMultipleResponse, SecondMultipleResponse, ThirdMultipleResponse> threeResponses =
                await client.ExecuteAndWaitForIdleAsync(
                    requestClient => requestClient.Advanced()
                        .GetResponseAsync<FirstMultipleResponse, SecondMultipleResponse, ThirdMultipleResponse>(
                            new MultipleResponseRequest(3),
                            cancellationToken: TestContext.Current.CancellationToken),
                    operationTimeout,
                    idleTimeout,
                    TestContext.Current.CancellationToken);

            Assert.True(twoResponses.Is(out Response<SecondMultipleResponse>? second));
            Assert.Equal(2, second.Message.Value);
            Assert.True(threeResponses.Is(out Response<ThirdMultipleResponse>? third));
            Assert.Equal(3, third.Message.Value);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "system-time-overloads")]
    public async Task MessagingOperations_SystemTimeOverloadsCompletePublishSendAndSingleResponseFlowsAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan idleTimeout = TimeSpan.FromMilliseconds(10);
        Guid correlationId = NewId.NextGuid();
        using var harness = new InMemoryTestHarness($"telemetry-system-time-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        harness.AddConsumer<MonitoredConsumer>();
        harness.AddConsumer<MonitoredRequestConsumer>();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await harness.Bus.ExecuteAndWaitForIdleAsync(
                endpoint => endpoint.PublishAsync(new MonitoredMessage(correlationId), TestContext.Current.CancellationToken),
                operationTimeout,
                idleTimeout,
                TestContext.Current.CancellationToken);
            await harness.InputQueueSendEndpoint.ExecuteAndWaitForIdleAsync(
                endpoint => endpoint.SendAsync(new MonitoredMessage(correlationId), TestContext.Current.CancellationToken),
                operationTimeout,
                idleTimeout,
                TestContext.Current.CancellationToken);

            IRequestClient<MonitoredRequest> client = harness.CreateRequestClient<MonitoredRequest>();
            Response<MonitoredResponse> response = await client.ExecuteAndWaitForIdleAsync(
                requestClient => requestClient.GetResponseAsync<MonitoredResponse>(
                    new MonitoredRequest(correlationId),
                    TestContext.Current.CancellationToken),
                operationTimeout,
                idleTimeout,
                TestContext.Current.CancellationToken);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.True(await harness.Published.AnyAsync<MonitoredMessage>(TestContext.Current.CancellationToken));
            Assert.True(await harness.Sent.AnyAsync<MonitoredMessage>(TestContext.Current.CancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<MonitoredRequest>(TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record MonitoredMessage(Guid CorrelationId);

    private sealed record MonitoredRequest(Guid CorrelationId);

    private sealed record MonitoredResponse(Guid CorrelationId);

    private sealed record MonitoredRequestHandled(Guid CorrelationId);

    private sealed record MultipleResponseRequest(int ResponseIndex);

    private sealed record FirstMultipleResponse(int Value);

    private sealed record SecondMultipleResponse(int Value);

    private sealed record ThirdMultipleResponse(int Value);

    private sealed class MonitoredConsumer : IConsumer<MonitoredMessage>
    {
        public Task ConsumeAsync(ConsumeContext<MonitoredMessage> context) => Task.CompletedTask;
    }

    private sealed class MonitoredRequestConsumer : IConsumer<MonitoredRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<MonitoredRequest> context)
        {
            await context.Advanced().PublishAsync(
                new MonitoredRequestHandled(context.Message.CorrelationId),
                context.CancellationToken);
            await context.RespondAsync(new MonitoredResponse(context.Message.CorrelationId));
        }
    }

    private sealed class MultipleResponseConsumer : IConsumer<MultipleResponseRequest>
    {
        public Task ConsumeAsync(ConsumeContext<MultipleResponseRequest> context) => context.Message.ResponseIndex switch
        {
            2 => context.RespondAsync(new SecondMultipleResponse(2)),
            3 => context.RespondAsync(new ThirdMultipleResponse(3)),
            _ => context.RespondAsync(new FirstMultipleResponse(1)),
        };
    }

    private sealed class ReceiveCompletionObserver(ObservableTimeProvider timeProvider) : IReceiveObserver
    {
        private readonly TaskCompletionSource<Task> _idleTimerArmedAfterReceive =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task IdleTimerArmedAfterReceive => _idleTimerArmedAfterReceive.Task.Unwrap();

        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceiveAsync(ReceiveContext context)
        {
            Task nextTimerChange = timeProvider.WaitForChangeCountAsync(timeProvider.ChangeCount + 1);
            _idleTimerArmedAfterReceive.TrySetResult(nextTimerChange);
            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            _idleTimerArmedAfterReceive.TrySetException(exception);
            return Task.CompletedTask;
        }
    }

    private sealed class ExpectedCallbackException(string message) : Exception(message);
}
