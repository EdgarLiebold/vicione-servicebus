using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class ActivityRedeliveryMessageIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-REDELIVERY", "execute-redelivery-configured-message-identity")]
    public Task DelayedExecute_RedeliveryUsesConfiguredMessageIdentityAsync(bool replaceMessageId) =>
        RunAsync(compensate: false, replaceMessageId);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-REDELIVERY", "compensate-redelivery-configured-message-identity")]
    public Task DelayedCompensate_RedeliveryUsesConfiguredMessageIdentityAsync(bool replaceMessageId) =>
        RunAsync(compensate: true, replaceMessageId);

    private static async Task RunAsync(bool compensate, bool replaceMessageId)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        budget.CancelAfter(timeout);
        var state = new IdentityState();
        var retryObserver = new IdentityRetryObserver();
        var handles = new List<ConnectHandle>();
        var startedTasks = new List<StartedTask>();
        InMemoryTestHarness harness = CourierTestSupport.CreateHarness("activity-redelivery-identity");
        using var activityCompleted = new CourierMessageRecorder<IRoutingSlipActivityCompleted>(1);
        using var completed = new CourierMessageRecorder<IRoutingSlipCompleted>(1);
        using var compensated = new CourierMessageRecorder<IRoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);

        Task TrackAsync(Task task, bool observational = false)
        {
            startedTasks.Add(new StartedTask(task, observational));
            return task;
        }

        void ConfigureRedelivery(IReceiveEndpointConfigurator endpoint)
        {
            endpoint.UseDelayedRedelivery(redelivery =>
            {
                Interlocked.Increment(ref state.ConfigurationCalls);
                redelivery.ReplaceMessageId = replaceMessageId;
                redelivery.Interval(1, TimeSpan.Zero);
                handles.Add(redelivery.ConnectRetryObserver(retryObserver));
            });
        }

        try
        {
            activityCompleted.Configure(harness);
            completed.Configure(harness);
            compensated.Configure(harness);
            faulted.Configure(harness);
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "identity-seed");
            string activityName;
            Action addActivities;
            string? terminalActivityName = null;

            if (compensate)
            {
                ActivityTestHarness<IdentityActivity, IdentityArguments, IdentityLog> activity = harness.AddActivity<
                    IdentityActivity, IdentityArguments, IdentityLog>(
                    _ => new IdentityActivity(state, redeliverExecute: false),
                    _ => new IdentityActivity(state, redeliverExecute: false));
                activity.CompensateReceiveEndpointConfiguring += ConfigureRedelivery;
                ExecuteActivityTestHarness<TerminalActivity, IdentityArguments> terminal = harness.AddExecuteActivity<
                    TerminalActivity, IdentityArguments>(_ => new TerminalActivity(state));
                activityName = activity.Name;
                terminalActivityName = terminal.Name;
                addActivities = () =>
                {
                    builder.AddActivity(activity.Name, activity.ExecuteAddress, new IdentityArguments("identity-input"));
                    builder.AddActivity(terminal.Name, terminal.ExecuteAddress, new IdentityArguments("terminal-input"));
                };
            }
            else
            {
                ExecuteActivityTestHarness<IdentityActivity, IdentityArguments> activity = harness.AddExecuteActivity<
                    IdentityActivity, IdentityArguments>(_ => new IdentityActivity(state, redeliverExecute: true));
                activity.ExecuteReceiveEndpointConfiguring += ConfigureRedelivery;
                activityName = activity.Name;
                addActivities = () =>
                    builder.AddActivity(activity.Name, activity.ExecuteAddress, new IdentityArguments("identity-input"));
            }

            await TrackAsync(harness.StartAsync(budget.Token)).WaitAsync(timeout, budget.Token);
            addActivities();
            var observations = new List<Task>
            {
                TrackAsync(activityCompleted.WaitAsync(timeout, budget.Token), observational: true),
            };
            if (compensate)
            {
                observations.Add(TrackAsync(compensated.WaitAsync(timeout, budget.Token), observational: true));
                observations.Add(TrackAsync(faulted.WaitAsync(timeout, budget.Token), observational: true));
            }
            else
                observations.Add(TrackAsync(completed.WaitAsync(timeout, budget.Token), observational: true));

            await TrackAsync(harness.Bus.ExecuteAsync(builder.Build(), budget.Token)).WaitAsync(timeout, budget.Token);
            await TrackAsync(Task.WhenAll(observations), observational: true).WaitAsync(timeout, budget.Token);
            // Recorder notification occurs inside its handler. Stop joins the real receive pipelines.
            await TrackAsync(harness.StopAsync(CancellationToken.None)).WaitAsync(timeout, CancellationToken.None);

            Assert.Equal(1, state.ConfigurationCalls);
            Assert.Single(handles);
            Delivery[] deliveries = state.Deliveries.ToArray();
            Assert.Equal(2, deliveries.Length);
            Assert.Equal([0, 1], deliveries.Select(delivery => delivery.RedeliveryCount));
            Assert.All(deliveries, delivery =>
            {
                Assert.Equal(0, delivery.RetryAttempt);
                Assert.Equal(trackingNumber, delivery.TrackingNumber);
                Assert.Equal(activityName, delivery.ActivityName);
                Assert.Equal("identity-seed", delivery.Seed);
                Assert.Equal("identity-input", delivery.Value);
                Assert.NotNull(delivery.MessageId);
                Assert.NotEqual(Guid.Empty, delivery.MessageId);
                Assert.NotEqual(Guid.Empty, delivery.ExecutionId);
            });
            Assert.Equal(2, retryObserver.Created);
            Assert.Same(state.RedeliveryFailure, Assert.Single(retryObserver.Failures));
            Assert.Equal(0, retryObserver.PreRetries);
            Assert.Equal(0, retryObserver.TerminalFaults);
            Assert.Equal(0, retryObserver.RetryCompletions);
            ConsumeContext<IRoutingSlipActivityCompleted> activityEvent = Assert.Single(activityCompleted.Messages);
            Assert.Equal(trackingNumber, activityEvent.Message.TrackingNumber);
            Assert.Equal(activityName, activityEvent.Message.ActivityName);
            Assert.Equal("identity-input", activityEvent.GetArgument<string>(nameof(IdentityArguments.Value)));

            if (compensate)
            {
                Assert.Equal(1, state.SuccessfulInitialExecutions);
                Assert.Equal(1, state.TerminalExecutions);
                ConsumeContext<IRoutingSlipActivityCompensated> compensation = Assert.Single(compensated.Messages);
                Assert.Equal(trackingNumber, compensation.Message.TrackingNumber);
                Assert.Equal(activityName, compensation.Message.ActivityName);
                Assert.Equal("identity-input", compensation.GetResult<string>(nameof(IdentityLog.Value)));
                Assert.Equal("identity-seed", compensation.GetVariable<string>("Seed"));
                ConsumeContext<IRoutingSlipFaulted> slipEvent = Assert.Single(faulted.Messages);
                Assert.Equal(trackingNumber, slipEvent.Message.TrackingNumber);
                Assert.Equal("identity-seed", slipEvent.GetVariable<string>("Seed"));
                IActivityException cause = Assert.Single(slipEvent.Message.ActivityExceptions);
                Assert.Equal(terminalActivityName, cause.Name);
                Assert.Equal(TypeCache<CourierExpectedException>.ShortName, cause.ExceptionInfo.ExceptionType);
                Assert.Equal(state.TerminalFailure.Message, cause.ExceptionInfo.Message);
                Assert.Empty(completed.Messages);
            }
            else
            {
                Assert.Equal(0, state.SuccessfulInitialExecutions);
                Assert.Equal(0, state.TerminalExecutions);
                ConsumeContext<IRoutingSlipCompleted> slipEvent = Assert.Single(completed.Messages);
                Assert.Equal(trackingNumber, slipEvent.Message.TrackingNumber);
                Assert.Equal("identity-seed", slipEvent.GetVariable<string>("Seed"));
                Assert.Equal("identity-complete", slipEvent.GetVariable<string>("IdentityResult"));
                Assert.Empty(compensated.Messages);
                Assert.Empty(faulted.Messages);
            }

            // First flag-dependent oracle follows genuine redelivery, business success and pipeline drain.
            if (replaceMessageId)
                Assert.NotEqual(deliveries[0].MessageId, deliveries[1].MessageId);
            else
                Assert.Equal(deliveries[0].MessageId, deliveries[1].MessageId);
            Assert.Null(deliveries[0].OriginalMessageId);
            if (replaceMessageId)
                Assert.Equal(deliveries[0].MessageId, deliveries[1].OriginalMessageId);
            else
                Assert.Null(deliveries[1].OriginalMessageId);
        }
        finally
        {
            try
            {
                budget.Cancel();
            }
            finally
            {
                try
                {
                    await JoinStartedTasksAsync(startedTasks, budget.Token);
                }
                finally
                {
                    try
                    {
                        Task stop = harness.StopAsync(CancellationToken.None);
                        await stop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                    finally
                    {
                        try
                        {
                            Task dispose = harness.DisposeAsync().AsTask();
                            await dispose.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                        }
                        finally
                        {
                            DisposeHandles(handles);
                        }
                    }
                }
            }
        }
    }

    private static async Task JoinStartedTasksAsync(IEnumerable<StartedTask> tasks, CancellationToken cleanupBudget)
    {
        var failures = new List<Exception>();
        foreach (StartedTask started in tasks)
        {
            try
            {
                await started.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            catch (OperationCanceledException exception) when (started.Observational && started.Task.IsCanceled
                && cleanupBudget.IsCancellationRequested && exception.CancellationToken == cleanupBudget)
            {
                // Only our canceled recorder wait wrapper is expected; source operation faults remain visible.
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
        ThrowFailures(failures);
    }

    private static void DisposeHandles(IEnumerable<ConnectHandle> handles)
    {
        var failures = new List<Exception>();
        foreach (ConnectHandle handle in handles)
        {
            try { handle.Dispose(); }
            catch (Exception exception) { failures.Add(exception); }
        }
        ThrowFailures(failures);
    }

    private static void ThrowFailures(List<Exception> failures)
    {
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }

    private sealed record StartedTask(Task Task, bool Observational);
    private sealed record Delivery(Guid? MessageId, Guid? OriginalMessageId, Guid TrackingNumber,
        string ActivityName, Guid ExecutionId, int RedeliveryCount, int RetryAttempt, string? Seed, string Value);
    public sealed record IdentityArguments(string Value);
    public sealed record IdentityLog(string Value);

    private sealed class IdentityState
    {
        public readonly ConcurrentQueue<Delivery> Deliveries = new();
        public readonly CourierExpectedException RedeliveryFailure = new("activity-identity-redelivery-once");
        public readonly CourierExpectedException TerminalFailure = new("activity-identity-terminal");
        public int ConfigurationCalls;
        public int SuccessfulInitialExecutions;
        public int TerminalExecutions;

        public int Record(ActivityContext context, string value)
        {
            int count = context.GetRedeliveryCount();
            Guid? original = context.TryGetHeader(MessageHeaders.OriginalMessageId, out Guid? header) ? header : null;
            Deliveries.Enqueue(new Delivery(context.MessageId, original, context.TrackingNumber, context.ActivityName,
                context.ExecutionId, count, context.GetRetryAttempt(), context.GetVariable<string>("Seed"), value));
            return count;
        }
    }

    private sealed class IdentityActivity(IdentityState state, bool redeliverExecute) : IActivity<IdentityArguments, IdentityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<IdentityArguments> context)
        {
            if (!redeliverExecute)
            {
                Interlocked.Increment(ref state.SuccessfulInitialExecutions);
                return Task.FromResult(context.Completed(new IdentityLog(context.Arguments.Value)));
            }
            if (state.Record(context, context.Arguments.Value) == 0)
                throw state.RedeliveryFailure;
            return Task.FromResult(context.CompletedWithVariables(new { IdentityResult = "identity-complete" }));
        }

        public Task<CompensationResult> CompensateAsync(CompensateContext<IdentityLog> context)
        {
            if (state.Record(context, context.Log.Value) == 0)
                throw state.RedeliveryFailure;
            return Task.FromResult(context.Compensated());
        }
    }

    private sealed class TerminalActivity(IdentityState state) : IExecuteActivity<IdentityArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<IdentityArguments> context)
        {
            Interlocked.Increment(ref state.TerminalExecutions);
            return Task.FromResult(context.Faulted(state.TerminalFailure));
        }
    }

    private sealed class IdentityRetryObserver : IRetryObserver
    {
        public readonly ConcurrentQueue<Exception> Failures = new();
        public int Created;
        public int PreRetries;
        public int TerminalFaults;
        public int RetryCompletions;

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context) where T : class, PipeContext
        { Interlocked.Increment(ref Created); return Task.CompletedTask; }
        public Task PostFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext
        { Failures.Enqueue(context.Exception); return Task.CompletedTask; }
        public Task PreRetryAsync<T>(RetryContext<T> context) where T : class, PipeContext
        { Interlocked.Increment(ref PreRetries); return Task.CompletedTask; }
        public Task RetryFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext
        { Interlocked.Increment(ref TerminalFaults); return Task.CompletedTask; }
        public Task RetryCompleteAsync<T>(RetryContext<T> context) where T : class, PipeContext
        { Interlocked.Increment(ref RetryCompletions); return Task.CompletedTask; }
    }
}
