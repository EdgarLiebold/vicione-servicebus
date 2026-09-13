using System.Collections.Concurrent;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipRetryIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REDELIVERY", "redelivery-header-does-not-leak-into-compensation")]
    public async Task RedeliveredActivity_DoesNotLeakItsRedeliveryCountIntoPreviousCompensationAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var compensationCounts = new ConcurrentQueue<int>();
        var redeliveryAttempts = new ConcurrentQueue<RetryObservation>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-redelivery-header-isolation");
        ActivityTestHarness<HeaderIsolationActivity, RetryArguments, RetryLog> compensating = harness.AddActivity<
            HeaderIsolationActivity,
            RetryArguments,
            RetryLog>(
            _ => new HeaderIsolationActivity(compensationCounts),
            _ => new HeaderIsolationActivity(compensationCounts));
        ExecuteActivityTestHarness<RedeliverThenFaultActivity, RedeliveryArguments> failing = harness.AddExecuteActivity<
            RedeliverThenFaultActivity,
            RedeliveryArguments>(_ => new RedeliverThenFaultActivity(redeliveryAttempts));
        failing.ExecuteReceiveEndpointConfiguring += endpoint => endpoint.UseDelayedRedelivery(
            redelivery => redelivery.Interval(1, TimeSpan.Zero));
        using var compensated = new CourierMessageRecorder<IRoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "header-isolation");
            builder.AddActivity(compensating.Name, compensating.ExecuteAddress, new RetryArguments("log"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new RedeliveryArguments(1));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(
                [new RetryObservation(0, 0, "header-isolation"), new RetryObservation(0, 1, "header-isolation")],
                redeliveryAttempts);
            Assert.Equal([0], compensationCounts);
            Assert.Equal(compensating.Name, Assert.Single(compensated.Messages).Message.ActivityName);
            Assert.Equal(trackingNumber, Assert.Single(faulted.Messages).Message.TrackingNumber);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-RETRY", "immediate-retry-eventually-succeeds")]
    public async Task ImmediateRetry_ReexecutesTheActivityWithTheSameVariablesThenCompletesOnceAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var attempts = new ConcurrentQueue<RetryObservation>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-immediate-success");
        ExecuteActivityTestHarness<RetryThenCompleteActivity, RetryArguments> activity = harness.AddExecuteActivity<
            RetryThenCompleteActivity,
            RetryArguments>(_ => new RetryThenCompleteActivity(attempts));
        activity.ExecuteReceiveEndpointConfiguring += endpoint =>
            endpoint.UseMessageRetry(retry => retry.Immediate(2));
        using var activityCompleted = new CourierMessageRecorder<IRoutingSlipActivityCompleted>(1);
        using var completed = new CourierMessageRecorder<IRoutingSlipCompleted>(1);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "retry-seed");
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new RetryArguments("execute"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityCompleted.WaitAsync(timeout, cancellationToken),
                completed.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(
                [new RetryObservation(0, 0, "retry-seed"), new RetryObservation(1, 0, "retry-seed")],
                attempts);
            ConsumeContext<IRoutingSlipActivityCompleted> activityEvent = Assert.Single(activityCompleted.Messages);
            Assert.Equal(activity.Name, activityEvent.Message.ActivityName);
            Assert.Equal(trackingNumber, activityEvent.Message.TrackingNumber);
            ConsumeContext<IRoutingSlipCompleted> slipEvent = Assert.Single(completed.Messages);
            Assert.Equal(trackingNumber, slipEvent.Message.TrackingNumber);
            Assert.Equal("immediate-retry-succeeded", slipEvent.GetVariable<string>("RetryResult"));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-RETRY", "immediate-retry-eventually-compensates")]
    public async Task ImmediateRetry_ReexecutesCompensationWithTheOriginalLogThenFaultsOnceAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var attempts = new ConcurrentQueue<CompensationObservation>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-immediate-compensate");
        ActivityTestHarness<RetryThenCompensateActivity, RetryArguments, RetryLog> compensating = harness.AddActivity<
            RetryThenCompensateActivity,
            RetryArguments,
            RetryLog>(
            _ => new RetryThenCompensateActivity(attempts),
            _ => new RetryThenCompensateActivity(attempts));
        compensating.CompensateReceiveEndpointConfiguring += endpoint =>
            endpoint.UseMessageRetry(retry => retry.Immediate(2));
        ExecuteActivityTestHarness<TerminalFaultActivity, RetryArguments> failing = harness.AddExecuteActivity<
            TerminalFaultActivity,
            RetryArguments>();
        using var compensated = new CourierMessageRecorder<IRoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "compensation-seed");
            builder.AddActivity(compensating.Name, compensating.ExecuteAddress, new RetryArguments("logged-value"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new RetryArguments("terminal"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(
                [
                    new CompensationObservation(0, 0, "compensation-seed", "logged-value"),
                    new CompensationObservation(1, 0, "compensation-seed", "logged-value"),
                ],
                attempts);
            ConsumeContext<IRoutingSlipActivityCompensated> compensation = Assert.Single(compensated.Messages);
            Assert.Equal(compensating.Name, compensation.Message.ActivityName);
            Assert.Equal(trackingNumber, compensation.Message.TrackingNumber);
            Assert.Equal("logged-value", compensation.GetResult<string>(nameof(RetryLog.Value)));
            Assert.Equal(trackingNumber, Assert.Single(faulted.Messages).Message.TrackingNumber);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-FAULT", "compensation-failure-publish-and-explicit-subscription")]
    public async Task ExhaustedCompensation_UsesEitherPublishOrTheExplicitSubscriptionExactlyOnceAsync(
        bool explicitSubscription)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var attempts = new ConcurrentQueue<CompensationObservation>();
        var subscribedActivityFailure = new TaskCompletionSource<ConsumeContext<IRoutingSlipActivityCompensationFailed>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var subscribedSlipFailure = new TaskCompletionSource<ConsumeContext<IRoutingSlipCompensationFailed>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        string subscriptionQueue = $"courier-compensation-subscription-{NewId.NextGuid():N}";
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-compensation-failed");
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(subscriptionQueue, endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Handler<IRoutingSlipActivityCompensationFailed>(context =>
            {
                subscribedActivityFailure.TrySetResult(context);
                return Task.CompletedTask;
            });
            endpoint.Handler<IRoutingSlipCompensationFailed>(context =>
            {
                subscribedSlipFailure.TrySetResult(context);
                return Task.CompletedTask;
            });
        });
        ActivityTestHarness<AlwaysFailingCompensationActivity, RetryArguments, RetryLog> compensating = harness.AddActivity<
            AlwaysFailingCompensationActivity,
            RetryArguments,
            RetryLog>(
            _ => new AlwaysFailingCompensationActivity(attempts),
            _ => new AlwaysFailingCompensationActivity(attempts));
        compensating.CompensateReceiveEndpointConfiguring += endpoint =>
            endpoint.UseMessageRetry(retry => retry.Immediate(2));
        ExecuteActivityTestHarness<TerminalFaultActivity, RetryArguments> failing = harness.AddExecuteActivity<
            TerminalFaultActivity,
            RetryArguments>();
        using var publishedActivityFailure = new CourierMessageRecorder<IRoutingSlipActivityCompensationFailed>(1);
        using var publishedSlipFailure = new CourierMessageRecorder<IRoutingSlipCompensationFailed>(1);
        publishedActivityFailure.Configure(harness);
        publishedSlipFailure.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            Uri subscriptionAddress = new(harness.BaseAddress, subscriptionQueue);
            var builder = new RoutingSlipBuilder(trackingNumber);
            if (explicitSubscription)
            {
                builder.AddSubscription(
                    subscriptionAddress,
                    RoutingSlipEvents.ActivityCompensationFailed | RoutingSlipEvents.CompensationFailed,
                    RoutingSlipEventContents.All);
            }
            builder.SetVariable("Seed", "failed-compensation-seed");
            builder.AddActivity(compensating.Name, compensating.ExecuteAddress, new RetryArguments("failed-log"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new RetryArguments("terminal"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            if (explicitSubscription)
            {
                await Task.WhenAll(
                    subscribedActivityFailure.Task.WaitAsync(timeout, cancellationToken),
                    subscribedSlipFailure.Task.WaitAsync(timeout, cancellationToken));
            }
            else
            {
                await Task.WhenAll(
                    publishedActivityFailure.WaitAsync(timeout, cancellationToken),
                    publishedSlipFailure.WaitAsync(timeout, cancellationToken));
            }
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(
                [
                    new CompensationObservation(0, 0, "failed-compensation-seed", "failed-log"),
                    new CompensationObservation(1, 0, "failed-compensation-seed", "failed-log"),
                    new CompensationObservation(2, 0, "failed-compensation-seed", "failed-log"),
                ],
                attempts);
            if (explicitSubscription)
            {
                Assert.Empty(publishedActivityFailure.Messages);
                Assert.Empty(publishedSlipFailure.Messages);
                AssertActivityCompensationFailure(
                    await subscribedActivityFailure.Task,
                    trackingNumber,
                    compensating.Name);
                AssertSlipCompensationFailure(await subscribedSlipFailure.Task, trackingNumber);
            }
            else
            {
                Assert.False(subscribedActivityFailure.Task.IsCompleted);
                Assert.False(subscribedSlipFailure.Task.IsCompleted);
                AssertActivityCompensationFailure(
                    Assert.Single(publishedActivityFailure.Messages),
                    trackingNumber,
                    compensating.Name);
                AssertSlipCompensationFailure(Assert.Single(publishedSlipFailure.Messages), trackingNumber);
            }
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-COURIER-REDELIVERY", "exact-redelivery-count-and-variable-survival")]
    public async Task DelayedRedelivery_PreservesVariablesAcrossEveryAttemptAndTerminalFaultAsync(int redeliveryCount)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var attempts = new ConcurrentQueue<RetryObservation>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness($"courier-redelivery-{redeliveryCount}");
        ExecuteActivityTestHarness<RedeliverThenFaultActivity, RedeliveryArguments> activity = harness.AddExecuteActivity<
            RedeliverThenFaultActivity,
            RedeliveryArguments>(_ => new RedeliverThenFaultActivity(attempts));
        activity.ExecuteReceiveEndpointConfiguring += endpoint => endpoint.UseDelayedRedelivery(
            redelivery => redelivery.Interval(redeliveryCount, TimeSpan.Zero));
        using var activityFaulted = new CourierMessageRecorder<IRoutingSlipActivityFaulted>(1);
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        activityFaulted.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "redelivery-seed");
            builder.AddActivity(
                activity.Name,
                activity.ExecuteAddress,
                new RedeliveryArguments(redeliveryCount));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityFaulted.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(
                Enumerable.Range(0, redeliveryCount + 1)
                    .Select(count => new RetryObservation(0, count, "redelivery-seed")),
                attempts);
            ConsumeContext<IRoutingSlipActivityFaulted> activityFailure = Assert.Single(activityFaulted.Messages);
            Assert.Equal("redelivery-terminal", activityFailure.GetVariable<string>("ErrorMessage"));
            Assert.Equal("redelivery-seed", activityFailure.GetVariable<string>("Seed"));
            ConsumeContext<IRoutingSlipFaulted> slipFailure = Assert.Single(faulted.Messages);
            Assert.Equal(trackingNumber, slipFailure.Message.TrackingNumber);
            Assert.Equal("redelivery-terminal", slipFailure.GetVariable<string>("ErrorMessage"));
            Assert.Equal("redelivery-seed", slipFailure.GetVariable<string>("Seed"));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-RETRY", "retry-exhaustion-preserves-fault-variables")]
    public async Task ImmediateRetry_PreservesVariablesUntilTheTerminalActivityAndSlipFaultsAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var attempts = new ConcurrentQueue<RetryObservation>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-retry-terminal-fault");
        ExecuteActivityTestHarness<RetryThenFaultActivity, RetryArguments> activity = harness.AddExecuteActivity<
            RetryThenFaultActivity,
            RetryArguments>(_ => new RetryThenFaultActivity(attempts));
        activity.ExecuteReceiveEndpointConfiguring += endpoint =>
            endpoint.UseMessageRetry(retry => retry.Immediate(2));
        using var activityFaulted = new CourierMessageRecorder<IRoutingSlipActivityFaulted>(1);
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        activityFaulted.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "retry-fault-seed");
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new RetryArguments("fault"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityFaulted.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(
                [
                    new RetryObservation(0, 0, "retry-fault-seed"),
                    new RetryObservation(1, 0, "retry-fault-seed"),
                    new RetryObservation(2, 0, "retry-fault-seed"),
                ],
                attempts);
            ConsumeContext<IRoutingSlipActivityFaulted> activityFailure = Assert.Single(activityFaulted.Messages);
            Assert.Equal("retry-terminal", activityFailure.GetVariable<string>("ErrorMessage"));
            Assert.Equal("retry-fault-seed", activityFailure.GetVariable<string>("Seed"));
            ConsumeContext<IRoutingSlipFaulted> slipFailure = Assert.Single(faulted.Messages);
            Assert.Equal(trackingNumber, slipFailure.Message.TrackingNumber);
            Assert.Equal("retry-terminal", slipFailure.GetVariable<string>("ErrorMessage"));
            Assert.Equal("retry-fault-seed", slipFailure.GetVariable<string>("Seed"));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static void AssertActivityCompensationFailure(
        ConsumeContext<IRoutingSlipActivityCompensationFailed> context,
        Guid trackingNumber,
        string activityName)
    {
        Assert.Equal(trackingNumber, context.Message.TrackingNumber);
        Assert.Equal(activityName, context.Message.ActivityName);
        Assert.Equal("failed-log", context.GetResult<string>(nameof(RetryLog.Value)));
        Assert.Equal("failed-compensation-seed", context.GetVariable<string>("Seed"));
        Assert.Equal(TypeCache<CourierExpectedException>.ShortName, context.Message.ExceptionInfo.ExceptionType);
        Assert.Equal("compensation-exhausted", context.Message.ExceptionInfo.Message);
    }

    private static void AssertSlipCompensationFailure(
        ConsumeContext<IRoutingSlipCompensationFailed> context,
        Guid trackingNumber)
    {
        Assert.Equal(trackingNumber, context.Message.TrackingNumber);
        Assert.Equal("failed-compensation-seed", context.GetVariable<string>("Seed"));
        Assert.Equal(TypeCache<CourierExpectedException>.ShortName, context.Message.ExceptionInfo.ExceptionType);
        Assert.Equal("compensation-exhausted", context.Message.ExceptionInfo.Message);
    }

    public sealed record RetryArguments(string Value);

    public sealed record RedeliveryArguments(int TerminalRedeliveryCount);

    public sealed record RetryLog(string Value);

    public sealed record RetryObservation(int RetryAttempt, int RedeliveryCount, string Variable);

    public sealed record CompensationObservation(
        int RetryAttempt,
        int RedeliveryCount,
        string Variable,
        string LogValue);

    public sealed class RetryThenCompleteActivity(ConcurrentQueue<RetryObservation> attempts) :
        IExecuteActivity<RetryArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RetryArguments> context)
        {
            int retryAttempt = context.GetRetryAttempt();
            attempts.Enqueue(new RetryObservation(
                retryAttempt,
                context.GetRedeliveryCount(),
                context.GetVariable<string>("Seed")!));

            if (retryAttempt == 0)
                throw new CourierExpectedException("retry-once");

            return Task.FromResult(context.CompletedWithVariables(new { RetryResult = "immediate-retry-succeeded" }));
        }
    }

    public sealed class HeaderIsolationActivity(ConcurrentQueue<int> compensationCounts) :
        IActivity<RetryArguments, RetryLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RetryArguments> context) =>
            Task.FromResult(context.Completed(new RetryLog(context.Arguments.Value)));

        public Task<CompensationResult> CompensateAsync(CompensateContext<RetryLog> context)
        {
            compensationCounts.Enqueue(context.GetRedeliveryCount());
            return Task.FromResult(context.Compensated());
        }
    }

    public sealed class RetryThenCompensateActivity(ConcurrentQueue<CompensationObservation> attempts) :
        IActivity<RetryArguments, RetryLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RetryArguments> context) =>
            Task.FromResult(context.Completed(new RetryLog(context.Arguments.Value)));

        public Task<CompensationResult> CompensateAsync(CompensateContext<RetryLog> context)
        {
            int retryAttempt = context.GetRetryAttempt();
            attempts.Enqueue(new CompensationObservation(
                retryAttempt,
                context.GetRedeliveryCount(),
                context.GetVariable<string>("Seed")!,
                context.Log.Value));

            if (retryAttempt == 0)
                throw new CourierExpectedException("compensate-once");

            return Task.FromResult(context.Compensated());
        }
    }

    public sealed class AlwaysFailingCompensationActivity(ConcurrentQueue<CompensationObservation> attempts) :
        IActivity<RetryArguments, RetryLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RetryArguments> context) =>
            Task.FromResult(context.Completed(new RetryLog(context.Arguments.Value)));

        public Task<CompensationResult> CompensateAsync(CompensateContext<RetryLog> context)
        {
            attempts.Enqueue(new CompensationObservation(
                context.GetRetryAttempt(),
                context.GetRedeliveryCount(),
                context.GetVariable<string>("Seed")!,
                context.Log.Value));
            throw new CourierExpectedException("compensation-exhausted");
        }
    }

    public sealed class TerminalFaultActivity : IExecuteActivity<RetryArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RetryArguments> context) =>
            Task.FromResult(context.Faulted(new CourierExpectedException(context.Arguments.Value)));
    }

    public sealed class RedeliverThenFaultActivity(ConcurrentQueue<RetryObservation> attempts) :
        IExecuteActivity<RedeliveryArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RedeliveryArguments> context)
        {
            int redeliveryCount = context.GetRedeliveryCount();
            attempts.Enqueue(new RetryObservation(
                context.GetRetryAttempt(),
                redeliveryCount,
                context.GetVariable<string>("Seed")!));

            if (redeliveryCount < context.Arguments.TerminalRedeliveryCount)
                throw new CourierExpectedException($"redelivery-{redeliveryCount}");

            return Task.FromResult(context.FaultedWithVariables(
                new CourierExpectedException("redelivery-terminal"),
                new { ErrorMessage = "redelivery-terminal" }));
        }
    }

    public sealed class RetryThenFaultActivity(ConcurrentQueue<RetryObservation> attempts) :
        IExecuteActivity<RetryArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RetryArguments> context)
        {
            int retryAttempt = context.GetRetryAttempt();
            attempts.Enqueue(new RetryObservation(
                retryAttempt,
                context.GetRedeliveryCount(),
                context.GetVariable<string>("Seed")!));

            if (retryAttempt < 2)
                throw new CourierExpectedException($"retry-{retryAttempt}");

            return Task.FromResult(context.FaultedWithVariables(
                new CourierExpectedException("retry-terminal"),
                new { ErrorMessage = "retry-terminal" }));
        }
    }
}
