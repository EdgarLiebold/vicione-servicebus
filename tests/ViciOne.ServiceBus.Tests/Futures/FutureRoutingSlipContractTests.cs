using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureRoutingSlipContractTests
{
    private static readonly Uri ActivityAddress = new("loopback://localhost/future-activity");

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-ROUTING-SLIP", "tracking-precedes-dispatch-and-callback-receives-cancellation")]
    public async Task CallbackBuiltRoutingSlip_TracksBeforeDispatchAndForwardsTheExactCancellationTokenAsync()
    {
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        using var source = new CancellationTokenSource();
        CancellationToken observedToken = default;
        var executor = new BuildRoutingSlipExecutor<InputMessage>((_, builder, cancellationToken) =>
        {
            observedToken = cancellationToken;
            builder.AddActivity("activity", ActivityAddress);
            return Task.CompletedTask;
        })
        {
            TrackRoutingSlip = true,
        };
        var recorder = new OutgoingMessageRecorder(message =>
        {
            IRoutingSlip routingSlip = Assert.IsAssignableFrom<IRoutingSlip>(message);
            Assert.Equal([routingSlip.TrackingNumber], state.Pending);
        });
        var machine = new ContextMachine();

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.InputReceived,
            state,
            new InputMessage(Guid.NewGuid()),
            context => executor.ExecuteAsync(context, source.Token),
            recorder,
            source.Token);

        Assert.Equal(source.Token, observedToken);
        Assert.Single(state.Pending);
        Assert.Single(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-ROUTING-SLIP", "failed-dispatch-rolls-back-tracking")]
    public async Task CallbackBuiltRoutingSlip_RemovesTrackingWhenDispatchFailsAsync()
    {
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var executor = new BuildRoutingSlipExecutor<InputMessage>((_, builder, _) =>
        {
            builder.AddActivity("activity", ActivityAddress);
            return Task.CompletedTask;
        })
        {
            TrackRoutingSlip = true,
        };
        var recorder = new OutgoingMessageRecorder(_ => throw new ExpectedDispatchException());
        var machine = new ContextMachine();

        await Assert.ThrowsAsync<ExpectedDispatchException>(() => FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.InputReceived,
            state,
            new InputMessage(Guid.NewGuid()),
            context => executor.ExecuteAsync(context),
            recorder,
            TestContext.Current.CancellationToken));

        Assert.Empty(state.Pending);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-ROUTING-SLIP", "tracking-is-independent-of-configuration-order")]
    public async Task TrackThenBuildConfiguration_StillTracksTheRoutingSlipBeforeDispatchAsync()
    {
        var machine = new TrackThenBuildFuture();
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var command = new InputMessage(state.CorrelationId);
        var recorder = new OutgoingMessageRecorder(message =>
        {
            IRoutingSlip routingSlip = Assert.IsAssignableFrom<IRoutingSlip>(message);
            Assert.Equal([routingSlip.TrackingNumber], state.Pending);
        });

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.CommandReceived,
            state,
            command,
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context),
            recorder,
            TestContext.Current.CancellationToken,
            new Uri("loopback://localhost/future-result"),
            Guid.NewGuid());

        Assert.Single(state.Pending);
        Assert.Single(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-ROUTING-SLIP", "planner-selection-preserves-tracking-and-cancellation")]
    public async Task TrackThenPlannerConfiguration_TracksBeforeDispatchAndForwardsCancellationAsync()
    {
        var machine = new TrackThenPlannerFuture();
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var command = new InputMessage(state.CorrelationId);
        using var source = new CancellationTokenSource();
        var planner = new RecordingItineraryPlanner();
        await using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IItineraryPlanner<InputMessage>>(planner)
            .BuildServiceProvider();
        var recorder = new OutgoingMessageRecorder(message =>
        {
            IRoutingSlip routingSlip = Assert.IsAssignableFrom<IRoutingSlip>(message);
            Assert.Equal([routingSlip.TrackingNumber], state.Pending);
        });

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.CommandReceived,
            state,
            command,
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context),
            recorder,
            source.Token,
            new Uri("loopback://localhost/future-result"),
            Guid.NewGuid(),
            services);

        Assert.Equal(source.Token, planner.ObservedToken);
        Assert.Equal(command, planner.ObservedInput);
        Assert.Single(state.Pending);
        Assert.Single(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-ROUTING-SLIP", "configuration-validation-and-event-hooks-are-actionable")]
    public void Configuration_ValidatesTerminalOwnershipAndForwardsEventHooks()
    {
        var machine = new ContextMachine();
        var stateMachineConfigurator = new RecordingStateMachineConfigurator();
        var configurator = new FutureRoutingSlipConfigurator<InputMessage, ResultMessage, FaultMessage, InputMessage>(
            stateMachineConfigurator,
            machine.RoutingSlipCompleted,
            machine.RoutingSlipFaulted);

        ValidationResult[] missingResult = configurator.Validate().ToArray();

        Assert.Collection(
            missingResult,
            failure => Assert.Contains("OnRoutingSlipCompleted or TrackPendingRoutingSlip", failure.Message, StringComparison.Ordinal),
            failure => Assert.Contains("OnRoutingSlipFaulted or TrackPendingRoutingSlip", failure.Message, StringComparison.Ordinal));
        Assert.False(configurator.HasResult(out _));
        Assert.True(configurator.HasFault(out _));

        configurator.OnRoutingSlipCompleted(result =>
            result.SetResultFactory(_ => new ResultMessage()));
        configurator.OnRoutingSlipFaulted(fault =>
            fault.SetFaultFactory(context => new FaultMessage(context.Message.TrackingNumber)));
        configurator.WhenRoutingSlipCompleted(binder => binder);
        configurator.WhenRoutingSlipFaulted(binder => binder);

        Assert.Empty(configurator.Validate());
        Assert.True(configurator.HasResult(out _));
        Assert.True(configurator.HasFault(out _));
        Assert.Equal([machine.RoutingSlipCompleted, machine.RoutingSlipFaulted], stateMachineConfigurator.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-ROUTING-SLIP", "default-fault-initializer-preserves-command-and-routing-slip-failure")]
    public async Task DefaultFaultInitializer_MapsRoutingSlipFailureAndStoredCommandAsync()
    {
        var activityHost = new FixedHostInfo("routing-slip-activity");
        ExceptionInfo exception = new FaultExceptionInfo(new InvalidOperationException("activity failed"));
        IActivityException activityException = new RoutingSlipActivityException(
            "charge-card",
            activityHost,
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch,
            TimeSpan.FromSeconds(2),
            exception);

        RoutingSlipFutureFault withActivityFailure = await CaptureDefaultFaultAsync([activityException]);
        RoutingSlipFutureFault withoutActivityFailure = await CaptureDefaultFaultAsync([]);

        Assert.Equal("routing-slip-activity", withActivityFailure.Host.MachineName);
        Assert.Equal("activity failed", Assert.Single(withActivityFailure.Exceptions).Message);
        Assert.Equal(HostMetadataCache.Host.MachineName, withoutActivityFailure.Host.MachineName);
        Assert.Empty(withoutActivityFailure.Exceptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-ROUTING-SLIP", "configuration-callbacks-and-constructor-dependencies-are-required")]
    public void Configuration_RejectsMissingCallbacksAndConstructorDependencies()
    {
        var machine = new ContextMachine();
        var stateMachineConfigurator = new RecordingStateMachineConfigurator();
        var configurator = new FutureRoutingSlipConfigurator<InputMessage, ResultMessage, FaultMessage, InputMessage>(
            stateMachineConfigurator,
            machine.RoutingSlipCompleted,
            machine.RoutingSlipFaulted);

        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => configurator.OnRoutingSlipCompleted(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => configurator.OnRoutingSlipFaulted(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => configurator.WhenRoutingSlipCompleted(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => configurator.WhenRoutingSlipFaulted(null!)).ParamName);
        Assert.Equal("buildItinerary", Assert.Throws<ArgumentNullException>(() => configurator.BuildItinerary(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            new FutureRoutingSlipConfigurator<InputMessage, ResultMessage, FaultMessage, InputMessage>(
                null!, machine.RoutingSlipCompleted, machine.RoutingSlipFaulted)).ParamName);
        Assert.Equal("routingSlipCompleted", Assert.Throws<ArgumentNullException>(() =>
            new FutureRoutingSlipConfigurator<InputMessage, ResultMessage, FaultMessage, InputMessage>(
                stateMachineConfigurator, null!, machine.RoutingSlipFaulted)).ParamName);
        Assert.Equal("routingSlipFaulted", Assert.Throws<ArgumentNullException>(() =>
            new FutureRoutingSlipConfigurator<InputMessage, ResultMessage, FaultMessage, InputMessage>(
                stateMachineConfigurator, machine.RoutingSlipCompleted, null!)).ParamName);
    }

    private static async Task<RoutingSlipFutureFault> CaptureDefaultFaultAsync(IReadOnlyList<IActivityException> activityExceptions)
    {
        var machine = new ContextMachine();
        var stateMachineConfigurator = new RecordingStateMachineConfigurator();
        var configurator = new FutureRoutingSlipConfigurator<InputMessage, ResultMessage, RoutingSlipFutureFault, InputMessage>(
            stateMachineConfigurator,
            machine.RoutingSlipCompleted,
            machine.RoutingSlipFaulted);
        Assert.True(configurator.HasFault(out FutureFault<InputMessage, RoutingSlipFutureFault, IRoutingSlipFaulted>? producer));

        Guid correlationId = Guid.NewGuid();
        Guid trackingNumber = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        var command = new InputMessage(correlationId);
        DateTimeOffset timestamp = new(2026, 9, 12, 18, 0, 0, TimeSpan.Zero);
        IRoutingSlipFaulted faulted = new RoutingSlipFaultedMessage(
            trackingNumber,
            timestamp,
            TimeSpan.FromSeconds(3),
            activityExceptions,
            new Dictionary<string, object>());
        var state = new FutureState { CorrelationId = correlationId, Pending = [correlationId] };
        RoutingSlipFutureFault? captured = null;

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.RoutingSlipFaulted,
            state,
            faulted,
            async context =>
            {
                state.Command = context.CreateFutureMessage(command);
                Assert.True(await producer.TrySetFaultedAsync(context, context.CancellationToken));
                IBehaviorContext<FutureState> stateContext = context;
                Assert.True(stateContext.TryGetFault(correlationId, out captured));
            },
            cancellationToken: TestContext.Current.CancellationToken,
            messageId: messageId);

        RoutingSlipFutureFault result = Assert.IsAssignableFrom<RoutingSlipFutureFault>(captured);
        Assert.Equal(messageId, result.FaultId);
        Assert.Equal(trackingNumber, result.FaultedMessageId);
        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(MessageTypeCache<InputMessage>.MessageTypeNames, result.FaultMessageTypes);
        Assert.Equal(command, result.Message);
        Assert.Empty(state.Pending);
        Assert.Equal(DateTimeOffset.UnixEpoch, state.Faulted);
        return result;
    }

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public IEvent<InputMessage> InputReceived { get; private set; } = null!;

        public IEvent<IRoutingSlipCompleted> RoutingSlipCompleted { get; private set; } = null!;

        public IEvent<IRoutingSlipFaulted> RoutingSlipFaulted { get; private set; } = null!;
    }

    private sealed class TrackThenBuildFuture : Future<InputMessage, ResultMessage>
    {
        public TrackThenBuildFuture()
        {
            ConfigureCommand(configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            ExecuteRoutingSlip(configuration =>
            {
                configuration.TrackPendingRoutingSlip();
                configuration.BuildItinerary((_, builder, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    builder.AddActivity("activity", ActivityAddress);
                    return Task.CompletedTask;
                });
            });
        }
    }

    private sealed class TrackThenPlannerFuture : Future<InputMessage, ResultMessage>
    {
        public TrackThenPlannerFuture()
        {
            ConfigureCommand(configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            ExecuteRoutingSlip(configuration =>
            {
                configuration.TrackPendingRoutingSlip();
                configuration.BuildUsingItineraryPlanner();
            });
        }
    }

    private sealed class RecordingItineraryPlanner : IItineraryPlanner<InputMessage>
    {
        public InputMessage? ObservedInput { get; private set; }

        public CancellationToken ObservedToken { get; private set; }

        public Task PlanItineraryAsync(IBehaviorContext<FutureState, InputMessage> context, IItineraryBuilder builder,
            CancellationToken cancellationToken = default)
        {
            ObservedInput = context.Message;
            ObservedToken = cancellationToken;
            builder.AddActivity("activity", ActivityAddress);
            return Task.CompletedTask;
        }
    }

    public sealed record InputMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record ResultMessage;

    public sealed record FaultMessage(Guid TrackingNumber);

    public interface RoutingSlipFutureFault : Fault<InputMessage>;

    private sealed class FixedHostInfo(string machineName) : HostInfo
    {
        public string MachineName { get; } = machineName;

        public string? ProcessName => null;

        public int ProcessId => 0;

        public string? Assembly => null;

        public string? AssemblyVersion => null;

        public string? FrameworkVersion => null;

        public string? ViciOneServiceBusVersion => null;

        public string? OperatingSystemVersion => null;
    }

    private sealed class RecordingStateMachineConfigurator : IFutureStateMachineConfigurator
    {
        public List<object> Events { get; } = [];

        public IEvent<T> CreateResponseEvent<T>()
            where T : class => throw new NotSupportedException();

        public void SetResult<T>(IEvent<T> responseReceived, Func<IBehaviorContext<FutureState, T>, Task> callback)
            where T : class => throw new NotSupportedException();

        public void SetFaulted<T>(IEvent<T> requestCompleted, Func<IBehaviorContext<FutureState, T>, Task<bool>> callback)
            where T : class => throw new NotSupportedException();

        public void CompletePendingRequest<T>(IEvent<T> requestCompleted, PendingFutureIdProvider<T> pendingIdProvider)
            where T : class => throw new NotSupportedException();

        public void DuringAnyWhen<T>(IEvent<T> whenEvent,
            Func<IEventActivityBinder<FutureState, T>, IEventActivityBinder<FutureState, T>> configure)
            where T : class
        {
            Events.Add(whenEvent);
        }
    }

    private sealed class ExpectedDispatchException : Exception;
}
