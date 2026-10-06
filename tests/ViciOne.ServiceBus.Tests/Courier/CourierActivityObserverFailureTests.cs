using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityObserverFailureTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "activity-fault-observer-preserves-primary-and-mixed-cancellation-causes")]
    public async Task PublicActivityFaultObservers_PreservePrimaryFailureAndOrderedSecondaryCausesAsync(
        bool compensate, int secondaryMode)
    {
        string unique = NewId.NextGuid().ToString("N");
        var state = new ObservationState(compensate, secondaryMode, unique);
        string executeName = "observer-execute-" + unique;
        string compensateName = "observer-compensate-" + unique;
        string triggerName = "observer-trigger-" + unique;
        ConnectHandle? observerHandle = null;
        var services = new ServiceCollection();
        services.AddSingleton(state);
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.AddActivity<ObservedActivity, Arguments, ActivityLog>()
                .ExecuteEndpoint(endpoint => endpoint.Name = executeName)
                .CompensateEndpoint(endpoint => endpoint.Name = compensateName);
            registration.AddExecuteActivity<BeginCompensationActivity, Arguments>()
                .Endpoint(endpoint => endpoint.Name = triggerName);
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/courier-observer-{unique}"));
                observerHandle = bus.ConnectActivityObserver(new FaultingActivityObserver(state));
                bus.ReceiveEndpoint("observer-outcome-" + unique, endpoint =>
                {
                    endpoint.Handler<IRoutingSlipActivityFaulted>(delivery =>
                    {
                        if (delivery.Message.TrackingNumber == state.TrackingNumber && delivery.Message.ActivityName == "Observed")
                            state.Outcome.TrySetResult(new ObservedOutcome(delivery.Message, [delivery.Message.ExceptionInfo]));
                        return Task.CompletedTask;
                    });
                    endpoint.Handler<IRoutingSlipActivityCompensationFailed>(delivery =>
                    {
                        if (delivery.Message.TrackingNumber == state.TrackingNumber && delivery.Message.ActivityName == "Observed")
                            state.Outcome.TrySetResult(new ObservedOutcome(delivery.Message, [delivery.Message.ExceptionInfo]));
                        return Task.CompletedTask;
                    });
                    endpoint.Handler<Fault<IRoutingSlip>>(delivery =>
                    {
                        if (delivery.Message.Message.TrackingNumber == state.TrackingNumber)
                            state.Outcome.TrySetResult(new ObservedOutcome(delivery.Message, delivery.Message.Exceptions));
                        return Task.CompletedTask;
                    });
                });
                bus.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IBusControl bus = Assert.IsAssignableFrom<IBusControl>(provider.GetRequiredService<IBus>());
        using var operations = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        operations.CancelAfter(Timeout);
        try
        {
            await bus.StartAsync(operations.Token);
            var builder = new RoutingSlipBuilder(state.TrackingNumber);
            builder.AddActivity("Observed", new Uri(bus.Address, executeName), new Arguments { Value = "original-input" });
            if (compensate)
                builder.AddActivity("Trigger", new Uri(bus.Address, triggerName), new Arguments { Value = "begin-compensation" });
            await bus.ExecuteAsync(builder.Build(), operations.Token);
            ObservedOutcome outcome = await state.Outcome.Task.WaitAsync(Timeout, operations.Token);

            // Inspect the actual outcome type first: an observer's cancellation must not turn a business failure into delivery cancellation.
            ExceptionInfo projected;
            if (compensate)
            {
                IRoutingSlipActivityCompensationFailed failed = Assert.IsAssignableFrom<IRoutingSlipActivityCompensationFailed>(outcome.Message);
                Assert.Equal(state.TrackingNumber, failed.TrackingNumber);
                Assert.Equal("Observed", failed.ActivityName);
                Assert.Equal("original-input", failed.Data[nameof(ActivityLog.Value)]);
                projected = failed.ExceptionInfo;
            }
            else
            {
                IRoutingSlipActivityFaulted failed = Assert.IsAssignableFrom<IRoutingSlipActivityFaulted>(outcome.Message);
                Assert.Equal(state.TrackingNumber, failed.TrackingNumber);
                Assert.Equal("Observed", failed.ActivityName);
                Assert.Equal("original-input", failed.Arguments[nameof(Arguments.Value)]);
                projected = failed.ExceptionInfo;
            }
            Assert.Single(outcome.Exceptions);
            Assert.Same(state.Primary, state.ObserverPrimary);
            Assert.Same(state.Primary, state.RecordedPrimaryAtCallback);
            Assert.Same(compensate ? state.PreCompensateContext : state.PreExecuteContext, state.FaultContext);
            Assert.Same(compensate ? state.CompensateInstance : state.ExecuteInstance, state.ActivityAtFault);
            Assert.Equal(1, state.ExecuteCalls);
            Assert.Equal(compensate ? 1 : 0, state.CompensateCalls);
            Assert.Equal(compensate
                ? new[] { "pre-execute", "activity-execute", "post-execute", "pre-compensate", "activity-compensate", "compensate-fault" }
                : new[] { "pre-execute", "activity-execute", "execute-fault" }, state.Trace.ToArray());
            if (compensate)
                Assert.Same(state.PreExecuteContext, state.PostExecuteContext);

            ExceptionInfo primaryProjection = Assert.Single(InnerChain(projected), item => item.Message == state.Primary.Message);
            Assert.Equal(TypeCache<IOException>.ShortName, primaryProjection.ExceptionType);
            Func<Exception?> readResult = Assert.IsAssignableFrom<Func<Exception?>>(state.ReadRecordedFailure);
            if (secondaryMode == 0)
            {
                Assert.Same(state.Primary, readResult());
                Assert.Equal(TypeCache<IOException>.ShortName, projected.ExceptionType);
                Assert.Equal(state.Primary.Message, projected.Message);
            }
            else
            {
                AggregateException actual = Assert.IsType<AggregateException>(readResult());
                Assert.Collection(actual.InnerExceptions,
                    failure => Assert.Same(state.Primary, failure),
                    failure => Assert.Same(state.Secondary, failure));
                Assert.Equal(TypeCache<AggregateException>.ShortName, projected.ExceptionType);
                Assert.Contains(state.Secondary!.Message, projected.Message, StringComparison.Ordinal);
            }
        }
        finally
        {
            operations.Cancel();
            try
            {
                await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            }
            finally
            {
                observerHandle?.Disconnect();
            }
        }
    }

    private static IEnumerable<ExceptionInfo> InnerChain(ExceptionInfo exception)
    {
        for (ExceptionInfo? current = exception; current is not null; current = current.InnerException)
            yield return current;
    }

    public sealed record ObservedOutcome(object Message, ExceptionInfo[] Exceptions);

    public sealed class ObservationState(bool compensate, int secondaryMode, string unique)
    {
        public readonly bool Compensate = compensate;
        public readonly Guid TrackingNumber = NewId.NextGuid();
        public readonly IOException Primary = new("activity-primary-" + unique);
        public readonly Exception? Secondary = secondaryMode switch
        {
            0 => null,
            1 => new IOException("observer-secondary-" + unique),
            _ => new OperationCanceledException("observer-secondary-canceled-" + unique)
        };
        public readonly ConcurrentQueue<string> Trace = new();
        public readonly TaskCompletionSource<ObservedOutcome> Outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ExecuteCalls;
        public int CompensateCalls;
        public object? ExecuteInstance;
        public object? CompensateInstance;
        public object? PreExecuteContext;
        public object? PostExecuteContext;
        public object? PreCompensateContext;
        public object? FaultContext;
        public object? ActivityAtFault;
        public Exception? ObserverPrimary;
        public Exception? RecordedPrimaryAtCallback;
        public Func<Exception?>? ReadRecordedFailure;
    }

    public sealed class Arguments
    {
        public string Value { get; set; } = "";
    }

    public sealed class ActivityLog
    {
        public string Value { get; set; } = "";
    }

    public sealed class ObservedActivity(ObservationState state) : IActivity<Arguments, ActivityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context)
        {
            Interlocked.Increment(ref state.ExecuteCalls);
            state.ExecuteInstance = this;
            state.Trace.Enqueue("activity-execute");
            return state.Compensate
                ? Task.FromResult(context.Completed(new ActivityLog { Value = context.Arguments.Value }))
                : Task.FromException<ExecutionResult>(state.Primary);
        }

        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context)
        {
            Interlocked.Increment(ref state.CompensateCalls);
            state.CompensateInstance = this;
            state.Trace.Enqueue("activity-compensate");
            return Task.FromException<CompensationResult>(state.Primary);
        }
    }

    public sealed class BeginCompensationActivity : IExecuteActivity<Arguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) =>
            Task.FromResult(context.Faulted(new InvalidOperationException("begin-compensation")));
    }

    private sealed class FaultingActivityObserver(ObservationState state) : IActivityObserver
    {
        public Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
            where TActivity : class where TArguments : class
        {
            if (typeof(TActivity) == typeof(ObservedActivity))
            {
                state.PreExecuteContext = context;
                state.Trace.Enqueue("pre-execute");
            }
            return Task.CompletedTask;
        }

        public Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
            where TActivity : class where TArguments : class
        {
            if (typeof(TActivity) == typeof(ObservedActivity))
            {
                state.PostExecuteContext = context;
                state.Trace.Enqueue("post-execute");
            }
            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
            where TActivity : class where TArguments : class
        {
            if (typeof(TActivity) != typeof(ObservedActivity))
                return Task.CompletedTask;
            state.FaultContext = context;
            state.ActivityAtFault = context.Activity;
            state.ObserverPrimary = exception;
            state.RecordedPrimaryAtCallback = context.Result is { } result && result.IsFaulted(out Exception? failure) ? failure : null;
            state.ReadRecordedFailure = () => context.Result is { } current && current.IsFaulted(out Exception? currentFailure) ? currentFailure : null;
            state.Trace.Enqueue("execute-fault");
            return state.Secondary is null ? Task.CompletedTask : Task.FromException(state.Secondary);
        }

        public Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
            where TActivity : class where TLog : class
        {
            if (typeof(TActivity) == typeof(ObservedActivity))
            {
                state.PreCompensateContext = context;
                state.Trace.Enqueue("pre-compensate");
            }
            return Task.CompletedTask;
        }

        public Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
            where TActivity : class where TLog : class => Task.CompletedTask;

        public Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
            where TActivity : class where TLog : class
        {
            if (typeof(TActivity) != typeof(ObservedActivity))
                return Task.CompletedTask;
            state.FaultContext = context;
            state.ActivityAtFault = context.Activity;
            state.ObserverPrimary = exception;
            state.RecordedPrimaryAtCallback = context.Result is { } result && result.IsFailed(out Exception? failure) ? failure : null;
            state.ReadRecordedFailure = () => context.Result is { } current && current.IsFailed(out Exception? currentFailure) ? currentFailure : null;
            state.Trace.Enqueue("compensate-fault");
            return state.Secondary is null ? Task.CompletedTask : Task.FromException(state.Secondary);
        }
    }
}
