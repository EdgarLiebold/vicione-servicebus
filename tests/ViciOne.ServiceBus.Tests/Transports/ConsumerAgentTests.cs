using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ConsumerAgentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "stop-debug-does-not-skip-manual-consume-completion")]
    public async Task ConsumerStoppingDiagnostic_DoesNotSkipManualLoopCompletionAsync(bool loggerThrows)
    {
        ILogContext? previous = LogContext.Current;
        var logger = new ConsumerStopAdmissionLogger(loggerThrows);
        LogContext.ConfigureCurrentLogContext(logger);
        var agent = CreateAgent();
        agent.CreateManualConsumeTask();
        Task consume = agent.RegisteredConsumeTask;
        Task? stop = null;
        try
        {
            Assert.False(consume.IsCompleted);
            var context = new TestStopContext(CancellationToken.None);
            stop = agent.StopAsync(context, CancellationToken.None);
            Exception? failure = await Record.ExceptionAsync(() =>
                stop.WaitAsync(OperationTimeout, CancellationToken.None));
            Assert.Equal(1, logger.SelectedCalls);
            Assert.Equal(new Uri("loopback://consumer-agent/input"), logger.Address);
            Assert.Equal(context.Reason, logger.Reason);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (failure is not null)
                Assert.Same(logger.Failure, failure);

            // FIRST finite ownership oracle: the real manually managed source must be completed.
            Assert.True(consume.IsCompletedSuccessfully);
            Assert.True(agent.Completed.IsCompletedSuccessfully);
            Assert.Null(failure);
            Assert.True(stop.IsCompletedSuccessfully);
            Assert.True(agent.Stopped.IsCancellationRequested);
            Assert.True(agent.GracefulShutdown);
        }
        finally
        {
            LogContext.ConfigureCurrentLogContext();
            agent.CompleteManualConsumeTask();
            try
            {
                if (stop is not null)
                    await ObserveOwnedTaskAsync(stop);
            }
            finally
            {
                try
                {
                    await ObserveOwnedTaskAsync(consume);
                }
                finally
                {
                    try
                    {
                        await agent.StopAsync(new TestStopContext(CancellationToken.None), CancellationToken.None)
                            .WaitAsync(OperationTimeout, CancellationToken.None);
                        await agent.Completed.WaitAsync(OperationTimeout, CancellationToken.None);
                    }
                    finally
                    {
                        LogContext.Current = previous;
                    }
                }
            }
        }
    }

    private sealed class ConsumerStopAdmissionLogger(bool throws) : Microsoft.Extensions.Logging.ILogger
    {
        public IOException Failure { get; } = new("Consumer stop admission debug failure");
        public int SelectedCalls { get; private set; }
        public int ThrowCount { get; private set; }
        public Uri? Address { get; private set; }
        public string? Reason { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) =>
            logLevel == Microsoft.Extensions.Logging.LogLevel.Debug;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            IEnumerable<KeyValuePair<string, object?>> fields = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            if (fields.FirstOrDefault(field => field.Key == "{OriginalFormat}").Value as string != "Consumer Stopping: {InputAddress} ({Reason})")
                return;
            SelectedCalls++;
            Address = fields.FirstOrDefault(field => field.Key == "InputAddress").Value as Uri;
            Reason = fields.FirstOrDefault(field => field.Key == "Reason").Value as string;
            if (throws)
            {
                ThrowCount++;
                throw Failure;
            }
        }
    }

    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "unexpected-consume-loop-exit-stops-agent")]
    public async Task UnexpectedConsumeLoopExit_StopsTheAgentAndMarksTheShutdownAsUngracefulAsync()
    {
        var consumeLoop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = CreateAgent();
        agent.RegisterConsumeTask(consumeLoop.Task);

        consumeLoop.TrySetResult();

        await agent.Completed.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await agent.StopAsync(TestContext.Current.CancellationToken)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.False(agent.GracefulShutdown);
        Assert.True(agent.Stopping.IsCancellationRequested);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "first-consume-loop-task-is-authoritative")]
    public async Task ExplicitStop_WaitsForTheFirstRegisteredTaskAndRemainsGracefulAsync()
    {
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ignored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = CreateAgent();

        Assert.Equal(
            "consumeTask",
            Assert.Throws<ArgumentNullException>(() => agent.RegisterConsumeTask(null!)).ParamName);
        agent.RegisterConsumeTask(first.Task);
        agent.RegisterConsumeTask(ignored.Task);

        Task stop = agent.StopAsync(TestContext.Current.CancellationToken);
        ignored.TrySetResult();
        Assert.False(stop.IsCompleted);

        first.TrySetResult();
        await stop.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.True(agent.GracefulShutdown);
    }

    [Theory]
    [InlineData(ManualConsumeOutcome.Completed)]
    [InlineData(ManualConsumeOutcome.Canceled)]
    [InlineData(ManualConsumeOutcome.Faulted)]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "manual-consume-loop-terminal-outcomes-are-owned")]
    public async Task ManualConsumeLoopOutcome_IsObservedAndOwnedAsync(ManualConsumeOutcome outcome)
    {
        var agent = CreateAgent();
        agent.CreateManualConsumeTask();
        agent.CreateManualConsumeTask();

        switch (outcome)
        {
            case ManualConsumeOutcome.Completed:
                agent.CompleteManualConsumeTask();
                break;
            case ManualConsumeOutcome.Canceled:
                agent.CancelManualConsumeTask(TestContext.Current.CancellationToken);
                break;
            case ManualConsumeOutcome.Faulted:
                agent.FaultManualConsumeTask(new ExpectedConsumeException());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }

        await agent.Completed.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await agent.StopAsync(TestContext.Current.CancellationToken)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.False(agent.GracefulShutdown);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "parallel-delivery-identity-coordination")]
    public async Task ParallelDispatch_CoordinatesSameKeysAndPreservesIndependentKeysAsync(bool distinctKeys)
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        const int deliveryCount = 32;
        ReceiveEndpointDispatcherReceiveContext[] contexts = Enumerable.Range(0, deliveryCount).Select(_ => CreateDelivery()).ToArray();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<DispatchAdmission>[] launches = contexts.Select((context, index) => Task.Run(async () =>
        {
            await release.Task.WaitAsync(callerToken);
            string key = distinctKeys ? $"key-{index}" : "same";
            // A wrapper prevents Task.Run/WhenAll from unwrapping and awaiting the still-pending owner dispatch.
            return new DispatchAdmission(agent.DispatchDeliveryAsync(key, context, NoLockReceiveContext.Instance));
        }, callerToken)).ToArray();
        Task[] admitted = [];
        try
        {
            release.TrySetResult();
            DispatchAdmission[] returned = await Task.WhenAll(launches).WaitAsync(OperationTimeout, callerToken);
            admitted = returned.Select(admission => admission.DispatchTask).ToArray();
            int expected = distinctKeys ? deliveryCount : 1;
            Assert.Equal(expected, dispatcher.Snapshot().Length);
            Assert.Equal(expected, admitted.Count(task => !task.IsCompleted));
            Assert.Equal(deliveryCount - expected, admitted.Count(task => task.IsCompletedSuccessfully));
            Assert.Equal(expected, agent.DeliveryCount);
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                Assert.Contains(contexts, context => ReferenceEquals(context, record.Context));

            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, callerToken);
            await Task.WhenAll(admitted).WaitAsync(OperationTimeout, callerToken);
            Assert.All(admitted, task => Assert.True(task.IsCompletedSuccessfully));
        }
        finally
        {
            release.TrySetResult();
            // Observe every launcher even if a guard canceled while returning admissions.
            Exception? launchFailure = await Record.ExceptionAsync(() =>
                Task.WhenAll(launches).WaitAsync(OperationTimeout, CancellationToken.None));
            try
            {
                await DrainAsync(agent, dispatcher, launches.Where(task => task.IsCompletedSuccessfully).Select(task => task.Result.DispatchTask));
            }
            finally
            {
                foreach (ReceiveEndpointDispatcherReceiveContext context in contexts)
                    context.Dispose();
            }
            Assert.Null(launchFailure);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "new-generation-retains-duplicate-fallbacks")]
    public async Task RenewedIdentity_RetainsDuplicatesWhileTheNewDeliveryIsPendingAsync()
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext secondContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext thirdContext = CreateDelivery();
        var admitted = new List<Task>();
        try
        {
            Task first = agent.DispatchDeliveryAsync("same", firstContext, NoLockReceiveContext.Instance);
            admitted.Add(first);
            await dispatcher.SettleAndReleaseAsync(Assert.Single(dispatcher.Snapshot()), callerToken);
            await first.WaitAsync(OperationTimeout, callerToken);

            Task second = agent.DispatchDeliveryAsync("same", secondContext, NoLockReceiveContext.Instance);
            admitted.Add(second);
            Task third = agent.DispatchDeliveryAsync("same", thirdContext, NoLockReceiveContext.Instance);
            admitted.Add(third);
            Assert.Equal(2, dispatcher.Snapshot().Length);
            Assert.False(second.IsCompleted);
            Assert.True(third.IsCompletedSuccessfully);
            Assert.Same(secondContext, dispatcher.Snapshot()[1].Context);
            await dispatcher.SettleAndReleaseAsync(dispatcher.Snapshot()[1], callerToken);
            await second.WaitAsync(OperationTimeout, callerToken);
            Assert.Equal(2, agent.DeliveryCount);
        }
        finally
        {
            await DrainAsync(agent, dispatcher, admitted);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "cancel-reaches-active-and-every-retained-delivery")]
    public async Task ManualCancellation_CancelsActiveAndEveryRetainedDeliveryAsync()
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        agent.CreateManualConsumeTask();
        using var cancellation = new CancellationTokenSource();
        ReceiveEndpointDispatcherReceiveContext[] contexts = Enumerable.Range(0, 8).Select(_ => CreateDelivery()).ToArray();
        var admitted = new List<Task>();
        try
        {
            foreach (ReceiveEndpointDispatcherReceiveContext context in contexts)
                admitted.Add(agent.DispatchDeliveryAsync("same", context, NoLockReceiveContext.Instance));
            Assert.Single(dispatcher.Snapshot());
            Assert.False(admitted[0].IsCompleted);
            Assert.All(admitted.Skip(1), task => Assert.True(task.IsCompletedSuccessfully));
            Assert.All(contexts, context => Assert.False(context.CancellationToken.IsCancellationRequested));
            await cancellation.CancelAsync();
            agent.CancelManualConsumeTask(cancellation.Token);
            Assert.All(contexts, context => Assert.True(context.CancellationToken.IsCancellationRequested));
            await dispatcher.SettleAndReleaseAsync(Assert.Single(dispatcher.Snapshot()), callerToken);
            await Task.WhenAll(admitted).WaitAsync(OperationTimeout, callerToken);
            // Only the registered unexpected-loop observer initiates stop before Completed becomes terminal.
            await agent.Completed.WaitAsync(OperationTimeout, callerToken);
            await agent.StopAsync(callerToken).WaitAsync(OperationTimeout, callerToken);
            Assert.True(agent.Completed.IsCompletedSuccessfully);
            Assert.False(agent.GracefulShutdown);
        }
        finally
        {
            try
            {
                await DrainAsync(agent, dispatcher, admitted);
            }
            finally
            {
                foreach (ReceiveEndpointDispatcherReceiveContext context in contexts)
                    context.Dispose();
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "manual-terminal-state-survives-retained-callback-failure")]
    public async Task ManualFailure_CompletesTheConsumeSourceDespiteCancellationCallbackFailureAsync(bool canceled)
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        agent.CreateManualConsumeTask();
        // Read-only reflection observes the ACTUAL manually registered consume promise, never mutates it.
        Task consumeTask = agent.RegisteredConsumeTask;
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext secondContext = CreateDelivery();
        var callbackFailure = new ExpectedConsumeException();
        var consumeFailure = new ExpectedConsumeException();
        int secondCallbackCount = 0;
        using CancellationTokenRegistration firstRegistration = firstContext.CancellationToken.Register(() => throw callbackFailure);
        using CancellationTokenRegistration secondRegistration = secondContext.CancellationToken.Register(() => Interlocked.Increment(ref secondCallbackCount));
        var admitted = new List<Task>();
        try
        {
            admitted.Add(agent.DispatchDeliveryAsync("first", firstContext, NoLockReceiveContext.Instance));
            admitted.Add(agent.DispatchDeliveryAsync("second", secondContext, NoLockReceiveContext.Instance));
            Assert.Equal(2, dispatcher.Snapshot().Length);
            AggregateException cleanupFailure = Assert.Throws<AggregateException>(() =>
            {
                if (canceled)
                    agent.CancelManualConsumeTask(source.Token);
                else
                    agent.FaultManualConsumeTask(consumeFailure);
            });
            Assert.Contains(cleanupFailure.Flatten().InnerExceptions, exception => ReferenceEquals(callbackFailure, exception));
            // Finite ORIGINAL witness: the real consume promise remains pending when cleanup throws.
            Assert.True(consumeTask.IsCompleted);
            if (canceled)
            {
                Assert.True(consumeTask.IsCanceled);
                OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    consumeTask.WaitAsync(OperationTimeout, callerToken));
                Assert.Equal(source.Token, observed.CancellationToken);
            }
            else
            {
                Assert.True(consumeTask.IsFaulted);
                ExpectedConsumeException observed = await Assert.ThrowsAsync<ExpectedConsumeException>(() =>
                    consumeTask.WaitAsync(OperationTimeout, callerToken));
                Assert.Same(consumeFailure, observed);
            }
            Assert.True(firstContext.CancellationToken.IsCancellationRequested);
            Assert.True(secondContext.CancellationToken.IsCancellationRequested);
            Assert.Equal(1, Volatile.Read(ref secondCallbackCount));
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, callerToken);
            await Task.WhenAll(admitted).WaitAsync(OperationTimeout, callerToken);
            await agent.Completed.WaitAsync(OperationTimeout, callerToken);
            Assert.False(agent.GracefulShutdown);
        }
        finally
        {
            // Original RED must also release the manual promise that the defect left pending.
            agent.CompleteManualConsumeTask();
            await ObserveOwnedTaskAsync(consumeTask);
            try
            {
                await DrainAsync(agent, dispatcher, admitted);
            }
            finally
            {
                // All sources get a cleanup attempt even if one callback unexpectedly fails again.
                try { firstContext.Cancel(); }
                finally { secondContext.Cancel(); }
            }
        }
    }

    [Theory]
    [InlineData(0)] // synchronous provider exception
    [InlineData(1)] // null provider task
    [InlineData(2)] // returned faulted task before settlement
    [InlineData(3)] // returned canceled task before settlement
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "failed-dispatch-detaches-identity-and-admits-retry")]
    public async Task DispatchStartupFailure_DetachesTrackedIdentityAndAdmitsRetryAsync(int failureMode)
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        using var failureSource = new CancellationTokenSource();
        await failureSource.CancelAsync();
        dispatcher.FirstFailureMode = failureMode;
        dispatcher.FailureCancellationToken = failureSource.Token;
        using ReceiveEndpointDispatcherReceiveContext failedContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext retryContext = CreateDelivery();
        Task? firstTask = null;
        var healthyAdmissions = new List<Task>();
        try
        {
            Exception? synchronousFailure = Record.Exception(() =>
            {
                firstTask = agent.DispatchDeliveryAsync("same", failedContext, NoLockReceiveContext.Instance);
            });
            Exception? observed = synchronousFailure;
            if (observed is null)
            {
                Assert.NotNull(firstTask);
                observed = await Record.ExceptionAsync(() => firstTask.WaitAsync(OperationTimeout, callerToken));
            }
            switch (failureMode)
            {
                case 0:
                case 2:
                    Assert.Same(dispatcher.ExpectedFailure, observed);
                    break;
                case 1:
                    Assert.Equal("The receive pipe dispatcher returned no dispatch task.", Assert.IsType<InvalidOperationException>(observed).Message);
                    break;
                case 3:
                    Assert.Equal(failureSource.Token, Assert.IsAssignableFrom<OperationCanceledException>(observed).CancellationToken);
                    Assert.NotNull(firstTask);
                    Assert.True(firstTask.IsCanceled);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failureMode));
            }
            Task retry = agent.DispatchDeliveryAsync("same", retryContext, NoLockReceiveContext.Instance);
            healthyAdmissions.Add(retry);
            // Finite ORIGINAL witness: actual dispatcher count is 1, the same-key retry is suppressed.
            Assert.Equal(2, dispatcher.Snapshot().Length);
            Assert.False(retry.IsCompleted);
            Assert.Same(retryContext, dispatcher.Snapshot()[1].Context);
            Assert.True(failedContext.CancellationToken.IsCancellationRequested);
            await dispatcher.SettleAndReleaseAsync(dispatcher.Snapshot()[1], callerToken);
            await retry.WaitAsync(OperationTimeout, callerToken);
            Assert.True(retry.IsCompletedSuccessfully);
        }
        finally
        {
            // Release every real gate before joining, even if an earlier assertion failed.
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, CancellationToken.None);
            // Observe expected failed owner/raw tasks separately: they are NOT successful-drain tasks.
            if (firstTask is not null)
                await ObserveOwnedTaskAsync(firstTask);
            foreach (RecordedDispatch record in dispatcher.Snapshot())
            {
                if (record.ReturnedTask is not null && record.ReturnedTask.IsCompleted)
                    await ObserveOwnedTaskAsync(record.ReturnedTask);
            }
            await DrainAsync(agent, dispatcher, healthyAdmissions);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "late-failure-cannot-cancel-successor-generation")]
    public async Task SettledGenerationLateFailure_DoesNotCancelTheNewPendingGenerationAsync()
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext secondContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext thirdContext = CreateDelivery();
        var failure = new ExpectedConsumeException();
        Task? first = null;
        var healthyAdmissions = new List<Task>();
        try
        {
            first = agent.DispatchDeliveryAsync("same", firstContext, NoLockReceiveContext.Instance);
            RecordedDispatch oldGeneration = Assert.Single(dispatcher.Snapshot());
            // Settle the REAL lock while its actual dispatcher task stays pending.
            await oldGeneration.ReceiveLock.CompleteAsync(callerToken);
            Assert.False(first.IsCompleted);
            Task second = agent.DispatchDeliveryAsync("same", secondContext, NoLockReceiveContext.Instance);
            healthyAdmissions.Add(second);
            Assert.Equal(2, dispatcher.Snapshot().Length);
            Assert.False(second.IsCompleted);
            dispatcher.FaultAndRelease(oldGeneration, failure);
            Assert.Same(failure, await Assert.ThrowsAsync<ExpectedConsumeException>(() => first.WaitAsync(OperationTimeout, callerToken)));
            Assert.False(secondContext.CancellationToken.IsCancellationRequested);
            Task third = agent.DispatchDeliveryAsync("same", thirdContext, NoLockReceiveContext.Instance);
            healthyAdmissions.Add(third);
            Assert.Equal(2, dispatcher.Snapshot().Length);
            Assert.True(third.IsCompletedSuccessfully);
            Assert.False(second.IsCompleted);
            Assert.False(thirdContext.CancellationToken.IsCancellationRequested);
            await dispatcher.SettleAndReleaseAsync(dispatcher.Snapshot()[1], callerToken);
            await Task.WhenAll(healthyAdmissions).WaitAsync(OperationTimeout, callerToken);
        }
        finally
        {
            // Original can also pass this generation control; it is NOT a separate RED claim.
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, CancellationToken.None);
            if (first is not null)
                await ObserveOwnedTaskAsync(first);
            await DrainAsync(agent, dispatcher, healthyAdmissions);
        }
    }

    // CORRECTED-ONLY. Original timer-thread callback propagation can terminate the native process.
    // Exclude this explicit method/trait from every original-source native run.
    [Fact]
    [Trait("ReviewPhase", "CorrectedOnly")]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "consumer-stop-timeout-owns-callback-failure-and-consume-task")]
    public async Task ConsumerStopTimeout_OwnsCallbackFailureAndJoinsTheConsumeTaskAsync()
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher, consumerStopTimeout: TimeSpan.FromMilliseconds(50));
        var consumeLoop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        agent.RegisterConsumeTask(consumeLoop.Task);
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext secondContext = CreateDelivery();
        var callbackFailure = new ExpectedConsumeException();
        var firstCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration firstRegistration = firstContext.CancellationToken.Register(() =>
        {
            firstCanceled.TrySetResult();
            throw callbackFailure;
        });
        using CancellationTokenRegistration secondRegistration = secondContext.CancellationToken.Register(() => secondCanceled.TrySetResult());
        var admitted = new List<Task>();
        Task? stop = null;
        try
        {
            admitted.Add(agent.DispatchDeliveryAsync("first", firstContext, NoLockReceiveContext.Instance));
            admitted.Add(agent.DispatchDeliveryAsync("second", secondContext, NoLockReceiveContext.Instance));
            stop = agent.StopAsync(callerToken);
            // Signals establish actual timeout cancellation; no Sleep/elapsed-time assertion.
            await Task.WhenAll(firstCanceled.Task, secondCanceled.Task).WaitAsync(OperationTimeout, callerToken);
            Assert.True(firstContext.CancellationToken.IsCancellationRequested);
            Assert.True(secondContext.CancellationToken.IsCancellationRequested);
            Assert.False(stop.IsCompleted);
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, callerToken);
            await Task.WhenAll(admitted).WaitAsync(OperationTimeout, callerToken);
            // Even after all dispatcher tasks end, the actual registered consume-loop task is owned.
            Assert.False(stop.IsCompleted);
            consumeLoop.TrySetResult();
            AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => stop.WaitAsync(OperationTimeout, callerToken));
            Assert.Single(observed.Flatten().InnerExceptions);
            Assert.Same(callbackFailure, observed.Flatten().InnerExceptions[0]);
        }
        finally
        {
            consumeLoop.TrySetResult();
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, CancellationToken.None);
            await Task.WhenAll(admitted).WaitAsync(OperationTimeout, CancellationToken.None);
            stop ??= agent.StopAsync(CancellationToken.None);
            await ObserveOwnedTaskAsync(stop);
            await consumeLoop.Task.WaitAsync(OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task ObserveOwnedTaskAsync(Task task)
    {
        Exception? observed = await Record.ExceptionAsync(() => task.WaitAsync(OperationTimeout, CancellationToken.None));
        // Expected fault/cancellation is observed; a watchdog never qualifies as joined task ownership.
        Assert.IsNotType<TimeoutException>(observed);
        Assert.True(task.IsCompleted);
    }

    // Primary finite ORIGINAL witness: expected AggregateException, actual plain provider exception.
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "dispatch-primary-and-cleanup-failures-preserve-both-causes")]
    public async Task DispatchFailure_PreservesThePrimaryAndCallbackCausesAndAdmitsRetryAsync()
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        dispatcher.FirstFailureMode = 2;
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext retryContext = CreateDelivery();
        var callbackFailure = new ExpectedConsumeException();
        using CancellationTokenRegistration callback = firstContext.CancellationToken.Register(() => throw callbackFailure);
        Task? first = null;
        var healthyAdmissions = new List<Task>();
        try
        {
            first = agent.DispatchDeliveryAsync("same", firstContext, NoLockReceiveContext.Instance);
            AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => first.WaitAsync(OperationTimeout, callerToken));
            Exception[] causes = observed.Flatten().InnerExceptions.ToArray();
            Assert.Equal(2, causes.Length);
            Assert.Contains(causes, exception => ReferenceEquals(dispatcher.ExpectedFailure, exception));
            Assert.Contains(causes, exception => ReferenceEquals(callbackFailure, exception));
            Task retry = agent.DispatchDeliveryAsync("same", retryContext, NoLockReceiveContext.Instance);
            healthyAdmissions.Add(retry);
            Assert.Equal(2, dispatcher.Snapshot().Length);
            Assert.False(retry.IsCompleted);
            Assert.Same(retryContext, dispatcher.Snapshot()[1].Context);
            await dispatcher.SettleAndReleaseAsync(dispatcher.Snapshot()[1], callerToken);
            await retry.WaitAsync(OperationTimeout, callerToken);
        }
        finally
        {
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, CancellationToken.None);
            if (first is not null)
                await ObserveOwnedTaskAsync(first);
            await DrainAsync(agent, dispatcher, healthyAdmissions);
        }
    }

    [Fact]
    [Trait("ReviewPhase", "CorrectedOnly")]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "stop-budget-cancellation-reports-owned-callback-failure")]
    public async Task StopBudgetCancellation_ReportsTheOwnedCallbackFailureAsync()
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher);
        var consumeLoop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        agent.RegisterConsumeTask(consumeLoop.Task);
        using var stopBudget = new CancellationTokenSource();
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext secondContext = CreateDelivery();
        var callbackFailure = new ExpectedConsumeException();
        var firstCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration firstRegistration = firstContext.CancellationToken.Register(() =>
        {
            firstCanceled.TrySetResult();
            throw callbackFailure;
        });
        using CancellationTokenRegistration secondRegistration = secondContext.CancellationToken.Register(() => secondCanceled.TrySetResult());
        var admitted = new List<Task>();
        Task? stop = null;
        try
        {
            admitted.Add(agent.DispatchDeliveryAsync("first", firstContext, NoLockReceiveContext.Instance));
            admitted.Add(agent.DispatchDeliveryAsync("second", secondContext, NoLockReceiveContext.Instance));
            // Actual stop budget differs from the caller wait. Caller cancellation cannot stand in for owned failure.
            stop = agent.StopAsync(new TestStopContext(stopBudget.Token), CancellationToken.None);
            Assert.True(agent.Stopping.IsCancellationRequested);
            await stopBudget.CancelAsync();
            await Task.WhenAll(firstCanceled.Task, secondCanceled.Task).WaitAsync(OperationTimeout, callerToken);
            Assert.True(firstContext.CancellationToken.IsCancellationRequested);
            Assert.True(secondContext.CancellationToken.IsCancellationRequested);
            // A canceled stop budget permits cooperative exit of delivery/consume waits.
            // The outer caller token is None: observe the ACTUAL owned cleanup failure, not a caller OCE.
            AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => stop.WaitAsync(OperationTimeout, callerToken));
            Assert.Single(observed.Flatten().InnerExceptions);
            Assert.Same(callbackFailure, observed.Flatten().InnerExceptions[0]);
            Assert.True(agent.Completed.IsFaulted);
            AggregateException lifecycleFailure = await Assert.ThrowsAsync<AggregateException>(() => agent.Completed.WaitAsync(OperationTimeout, callerToken));
            Assert.Single(lifecycleFailure.Flatten().InnerExceptions);
            Assert.Same(callbackFailure, lifecycleFailure.Flatten().InnerExceptions[0]);
            Assert.False(consumeLoop.Task.IsCompleted);
            // Fixture-owned pending providers/consume promise are released and joined separately in finally.
        }
        finally
        {
            consumeLoop.TrySetResult();
            foreach (RecordedDispatch record in dispatcher.Snapshot())
                await dispatcher.SettleAndReleaseAsync(record, CancellationToken.None);
            await Task.WhenAll(admitted).WaitAsync(OperationTimeout, CancellationToken.None);
            stop ??= agent.StopAsync(new TestStopContext(stopBudget.Token), CancellationToken.None);
            await ObserveOwnedTaskAsync(stop);
            await ObserveOwnedTaskAsync(agent.Completed);
            await consumeLoop.Task.WaitAsync(OperationTimeout, CancellationToken.None);
        }
    }

    // CURRENT pre-diagnostic-fix source only: qualified F065 cancellation containment is already present.
    // Never run this timer case against the historical pre-F065 callback implementation.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "stop-diagnostics-do-not-replace-owned-cancellation-failures")]
    public async Task StopDiagnostics_DoNotReplaceOwnedCancellationFailuresAsync(bool timerCancellation, bool loggerThrows)
    {
        CancellationToken callerToken = TestContext.Current.CancellationToken;
        ILogContext? previous = LogContext.Current;
        var logger = new ConsumerStopDiagnosticLogger(loggerThrows);
        LogContext.ConfigureCurrentLogContext(logger);
        var agent = CreateRecordingAgent(out RecordingDispatcherProxy dispatcher,
            consumerStopTimeout: timerCancellation ? TimeSpan.FromMilliseconds(50) : null);
        var consumeLoop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        agent.RegisterConsumeTask(consumeLoop.Task);
        using var stopBudget = new CancellationTokenSource();
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateDelivery();
        using ReceiveEndpointDispatcherReceiveContext secondContext = CreateDelivery();
        var callbackFailure = new ExpectedConsumeException();
        var loopFailure = new ExpectedConsumeException();
        var firstCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration firstRegistration = firstContext.CancellationToken.Register(() =>
        {
            firstCanceled.TrySetResult();
            throw callbackFailure;
        });
        using CancellationTokenRegistration secondRegistration = secondContext.CancellationToken.Register(() => secondCanceled.TrySetResult());
        var admitted = new List<Task>();
        Task? stop = null;
        try
        {
            admitted.Add(agent.DispatchDeliveryAsync("first", firstContext, NoLockReceiveContext.Instance));
            admitted.Add(agent.DispatchDeliveryAsync("second", secondContext, NoLockReceiveContext.Instance));
            stop = agent.StopAsync(new TestStopContext(stopBudget.Token), CancellationToken.None);
            Assert.True(agent.Stopping.IsCancellationRequested);
            if (!timerCancellation)
                await stopBudget.CancelAsync();
            await Task.WhenAll(firstCanceled.Task, secondCanceled.Task).WaitAsync(OperationTimeout, callerToken);
            Assert.True(firstContext.CancellationToken.IsCancellationRequested);
            Assert.True(secondContext.CancellationToken.IsCancellationRequested);
            if (timerCancellation)
            {
                Assert.False(stop.IsCompleted);
                foreach (RecordedDispatch record in dispatcher.Snapshot())
                    await dispatcher.SettleAndReleaseAsync(record, callerToken);
                await Task.WhenAll(admitted).WaitAsync(OperationTimeout, callerToken);
                Assert.False(stop.IsCompleted);
                // Actual consume-loop failure is diagnostic-only under the existing policy;
                // retained cancellation failures must still be the owned lifecycle outcome.
                consumeLoop.TrySetException(loopFailure);
            }

            AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() =>
                stop.WaitAsync(OperationTimeout, callerToken));
            Assert.Single(observed.Flatten().InnerExceptions);
            Assert.Same(callbackFailure, observed.Flatten().InnerExceptions[0]);
            AggregateException lifecycleFailure = await Assert.ThrowsAsync<AggregateException>(() =>
                agent.Completed.WaitAsync(OperationTimeout, callerToken));
            Assert.Single(lifecycleFailure.Flatten().InnerExceptions);
            Assert.Same(callbackFailure, lifecycleFailure.Flatten().InnerExceptions[0]);
            Exception? logged = Assert.Single(logger.Failures);
            if (timerCancellation)
                Assert.Same(loopFailure, logged);
            else
            {
                Assert.Null(logged);
                // Canceled stop budgets preserve cooperative exit; fixture owns this pending loop.
                Assert.False(consumeLoop.Task.IsCompleted);
            }
        }
        finally
        {
            LogContext.ConfigureCurrentLogContext();
            try
            {
                consumeLoop.TrySetResult();
                foreach (RecordedDispatch record in dispatcher.Snapshot())
                    await dispatcher.SettleAndReleaseAsync(record, CancellationToken.None);
                await Task.WhenAll(admitted).WaitAsync(OperationTimeout, CancellationToken.None);
                stop ??= agent.StopAsync(new TestStopContext(stopBudget.Token), CancellationToken.None);
                await ObserveOwnedTaskAsync(stop);
                await ObserveOwnedTaskAsync(agent.Completed);
                await ObserveOwnedTaskAsync(consumeLoop.Task);
            }
            finally
            {
                LogContext.Current = previous;
            }
        }
    }

    private sealed class TestStopContext(CancellationToken cancellationToken)
        : ViciOne.ServiceBus.Middleware.BasePipeContext(cancellationToken), StopContext
    {
        public string Reason => "Caller stop budget canceled";
    }

    private static ReceiveEndpointDispatcherReceiveContext CreateDelivery()
    {
        var limits = new MessageLimits
        {
            MaxBodyBytes = 1024,
            MaxEnvelopeBytes = 1024,
            MaxJsonDepth = 16,
        };
        var endpoint = new ReceiveMessageLimitsTestContext(limits, new Uri("loopback://consumer-agent/input"));
        return new ReceiveEndpointDispatcherReceiveContext(endpoint, [], new Dictionary<string, object>());
    }

    private static TestConsumerAgent CreateRecordingAgent(
        out RecordingDispatcherProxy recording,
        IEqualityComparer<string>? comparer = null,
        TimeSpan? consumerStopTimeout = null)
    {
        var dispatcher = DispatchProxy.Create<IReceivePipeDispatcher, RecordingDispatcherProxy>();
        recording = (RecordingDispatcherProxy)(object)dispatcher;
        var context = DispatchProxy.Create<ReceiveEndpointContext, ReceiveEndpointContextProxy>();
        ((ReceiveEndpointContextProxy)(object)context).Initialize(dispatcher, consumerStopTimeout);
        return new TestConsumerAgent(context, comparer);
    }

    private static async Task DrainAsync(
        TestConsumerAgent agent,
        RecordingDispatcherProxy dispatcher,
        IEnumerable<Task> admitted)
    {
        // Cleanup releases REAL tasks even after a finite assertion failed.
        foreach (RecordedDispatch record in dispatcher.Snapshot())
            await dispatcher.SettleAndReleaseAsync(record, CancellationToken.None);
        await Task.WhenAll(admitted).WaitAsync(OperationTimeout, CancellationToken.None);
        await agent.StopAsync(CancellationToken.None).WaitAsync(OperationTimeout, CancellationToken.None);
    }

    private sealed record DispatchAdmission(Task DispatchTask);

    private sealed class RecordedDispatch(ReceiveContext context, ReceiveLockContext receiveLock)
    {
        public ReceiveContext Context { get; } = context;
        public ReceiveLockContext ReceiveLock { get; } = receiveLock;
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? ReturnedTask { get; set; }
    }

    private class RecordingDispatcherProxy : DispatchProxy
    {
        private readonly object _gate = new();
        private readonly List<RecordedDispatch> _records = [];
        private ZeroActivityHandler? _zeroActivity;
        private int _peak;
        public int? FirstFailureMode { get; set; }
        public CancellationToken FailureCancellationToken { get; set; }
        public Exception ExpectedFailure { get; } = new ExpectedConsumeException();

        public RecordedDispatch[] Snapshot()
        {
            lock (_gate)
                return _records.ToArray();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            lock (_gate)
            {
                switch (targetMethod.Name)
                {
                    case nameof(IReceivePipeDispatcher.DispatchAsync):
                    {
                        var record = new RecordedDispatch((ReceiveContext)args![0]!, (ReceiveLockContext)args[1]!);
                        _records.Add(record);
                        if (_records.Count == 1 && FirstFailureMode is int mode)
                        {
                            // Terminalize the fixture gate without faking transport settlement.
                            // Sync/null have NO actual returned dispatcher task and count as no active task.
                            record.Completion.TrySetResult();
                            switch (mode)
                            {
                                case 0: throw ExpectedFailure;
                                case 1: return null;
                                case 2: record.ReturnedTask = Task.FromException(ExpectedFailure); break;
                                case 3: record.ReturnedTask = Task.FromCanceled(FailureCancellationToken); break;
                                default: throw new ArgumentOutOfRangeException(nameof(FirstFailureMode));
                            }
                        }
                        else
                            record.ReturnedTask = record.Completion.Task;
                        _peak = Math.Max(_peak, ActiveCount());
                        // Deliberately NONASYNC: actual returned task is stored and joined.
                        return record.ReturnedTask;
                    }
                    case $"get_{nameof(IDispatchMetrics.ActiveDispatchCount)}":
                        return ActiveCount();
                    case $"get_{nameof(IDispatchMetrics.DispatchCount)}":
                        return (long)_records.Count;
                    case $"get_{nameof(IDispatchMetrics.MaxConcurrentDispatchCount)}":
                        return _peak;
                    case $"add_{nameof(IDispatchMetrics.ZeroActivity)}":
                        _zeroActivity += (ZeroActivityHandler?)args![0];
                        return null;
                    case $"remove_{nameof(IDispatchMetrics.ZeroActivity)}":
                        _zeroActivity -= (ZeroActivityHandler?)args![0];
                        return null;
                    default:
                        throw new NotSupportedException(targetMethod.Name);
                }
            }
        }

        private int ActiveCount() => _records.Count(record => record.ReturnedTask is { IsCompleted: false });

        public void FaultAndRelease(RecordedDispatch record, Exception failure) => record.Completion.TrySetException(failure);

        public async Task SettleAndReleaseAsync(RecordedDispatch record, CancellationToken cancellationToken)
        {
            // This is the REAL ConsumerAgent-supplied PendingReceiveLockContext when tracked.
            await record.ReceiveLock.CompleteAsync(cancellationToken);
            bool released = record.Completion.TrySetResult();
            ZeroActivityHandler? handlers;
            lock (_gate)
                handlers = released && ActiveCount() == 0 ? _zeroActivity : null;
            if (handlers is not null)
                await Task.WhenAll(handlers.GetInvocationList().Cast<ZeroActivityHandler>().Select(handler => handler()));
        }
    }


    private static TestConsumerAgent CreateAgent()
    {
        var dispatcher = DispatchProxy.Create<IReceivePipeDispatcher, IdleDispatcherProxy>();
        var context = DispatchProxy.Create<ReceiveEndpointContext, ReceiveEndpointContextProxy>();
        ((ReceiveEndpointContextProxy)(object)context).Initialize(dispatcher);
        return new TestConsumerAgent(context);
    }

    public enum ManualConsumeOutcome
    {
        Completed,
        Canceled,
        Faulted,
    }

    private sealed class TestConsumerAgent(ReceiveEndpointContext context, IEqualityComparer<string>? comparer = null) : ConsumerAgent<string>(context, comparer)
    {
        public bool GracefulShutdown => IsGracefulShutdown;

        public Task RegisteredConsumeTask => (Task)(typeof(ConsumerAgent<string>)
            .GetField("_consumeTask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(this) ?? throw new InvalidOperationException("No consume task was registered."));

        public Task DispatchDeliveryAsync(string key, BaseReceiveContext context, ReceiveLockContext receiveLock) => DispatchAsync(key, context, receiveLock);

        public void CreateManualConsumeTask() => TrySetManualConsumeTask();

        public void RegisterConsumeTask(Task task) => TrySetConsumeTask(task);

        public void CompleteManualConsumeTask() => TrySetConsumeCompleted();

        public void CancelManualConsumeTask(CancellationToken cancellationToken) => TrySetConsumeCanceled(cancellationToken);

        public void FaultManualConsumeTask(Exception exception) => TrySetConsumeException(exception);
    }

    private class ReceiveEndpointContextProxy : DispatchProxy
    {
        private IReceivePipeDispatcher? _dispatcher;
        private TimeSpan? _consumerStopTimeout;
        private readonly ILogContext _logContext = new BusLogContext(NullLoggerFactory.Instance);

        public void Initialize(IReceivePipeDispatcher dispatcher, TimeSpan? consumerStopTimeout = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _consumerStopTimeout = consumerStopTimeout;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                nameof(ReceiveEndpointContext.CreateReceivePipeDispatcher) => _dispatcher
                    ?? throw new InvalidOperationException("The test context has not been initialized."),
                $"get_{nameof(ReceiveEndpointContext.LogContext)}" => _logContext,
                $"get_{nameof(ReceiveEndpointContext.StopTimeout)}" => OperationTimeout,
                $"get_{nameof(ReceiveEndpointContext.ConsumerStopTimeout)}" => _consumerStopTimeout,
                $"get_{nameof(ReceiveEndpointContext.InputAddress)}" => new Uri("loopback://consumer-agent/input"),
                nameof(PipeContext.TryGetPayload) when targetMethod.IsGenericMethod
                    && targetMethod.GetGenericArguments()[0] == typeof(TimeProvider) => NoTimeProvider(args),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        private static bool NoTimeProvider(object?[]? args)
        {
            args![0] = null;
            return false;
        }
    }

    private class IdleDispatcherProxy : DispatchProxy
    {
        private ZeroActivityHandler? _zeroActivity;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                $"add_{nameof(IDispatchMetrics.ZeroActivity)}" => AddZeroActivity(args),
                $"remove_{nameof(IDispatchMetrics.ZeroActivity)}" => RemoveZeroActivity(args),
                $"get_{nameof(IDispatchMetrics.ActiveDispatchCount)}" => 0,
                $"get_{nameof(IDispatchMetrics.DispatchCount)}" => 0L,
                $"get_{nameof(IDispatchMetrics.MaxConcurrentDispatchCount)}" => 0,
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        private object? AddZeroActivity(object?[]? args)
        {
            _zeroActivity += (ZeroActivityHandler?)args?[0];
            return null;
        }

        private object? RemoveZeroActivity(object?[]? args)
        {
            _zeroActivity -= (ZeroActivityHandler?)args?[0];
            return null;
        }
    }

    private sealed class ConsumerStopDiagnosticLogger(bool throws) : Microsoft.Extensions.Logging.ILogger
    {
        public System.Collections.Concurrent.ConcurrentQueue<Exception?> Failures { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) =>
            logLevel == Microsoft.Extensions.Logging.LogLevel.Warning;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            if (!message.StartsWith("Consumer stop canceled:", StringComparison.Ordinal)
                && !message.StartsWith("Consumer stop faulted:", StringComparison.Ordinal))
                return;
            Failures.Enqueue(exception);
            if (throws)
                throw new ConsumerStopDiagnosticException();
        }
    }

    private sealed class ConsumerStopDiagnosticException : Exception;

    private sealed class ExpectedConsumeException : Exception;
}
