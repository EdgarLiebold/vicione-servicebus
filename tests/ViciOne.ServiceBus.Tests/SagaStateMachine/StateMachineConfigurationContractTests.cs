using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "consume-context-expression-conversion")]
    public void CorrelationExpressionConverter_BindsTheMessageAndPreservesTheSagaPredicate()
    {
        Guid correlationId = NewId.NextGuid();
        var message = new CorrelationMessage(correlationId);
        ConsumeContext<CorrelationMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message,
            TestContext.Current.CancellationToken);
        var converter = new EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>(consumeContext);

        Expression<Func<CorrelationState, bool>> expression = converter.Convert(
            (state, context) => state.BusinessId == context.Message.BusinessId);
        Func<CorrelationState, bool> predicate = expression.Compile();

        Assert.True(predicate(new CorrelationState { BusinessId = correlationId }));
        Assert.False(predicate(new CorrelationState { BusinessId = NewId.NextGuid() }));
        Assert.Contains(nameof(CorrelationState.BusinessId), expression.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("context", expression.Parameters.Select(parameter => parameter.Name));
        Assert.Single(expression.Parameters);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "inherited-state-and-event-surface-executes")]
    public async Task DerivedMachine_InitializesAndExecutesInheritedAndDeclaredStateMembersAsync()
    {
        var machine = new DerivedMachine();
        var instance = new InheritedState();

        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.BaseStarted, new BaseStart("base"));

        Assert.Equal("base", instance.Value);
        Assert.Same(machine.BaseRunning, instance.CurrentState);
        Assert.Contains(machine.BaseRunning, machine.States);
        Assert.Contains(machine.BaseStarted, machine.Events);

        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.DerivedStopped, new DerivedStop("derived"));

        Assert.Equal("base:derived", instance.Value);
        Assert.Same(machine.Final, instance.CurrentState);
        Assert.Contains(machine.DerivedStopped, machine.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "missing-correlation-fails-at-connect")]
    public void MissingEventCorrelation_IsRejectedBeforeTheBusCanStart()
    {
        var machine = new UnknownCorrelationMachine();
        var repository = new InMemorySagaRepository<UnknownCorrelationState>();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => Bus.Factory.CreateUsingInMemory(
            configurator => configurator.ReceiveEndpoint(
                $"unknown-correlation-{NewId.NextGuid():N}",
                endpoint => endpoint.StateMachineSaga(machine, repository))));

        ConfigurationException configurationFailure = Assert.IsType<ConfigurationException>(exception.InnerException);
        Assert.Contains(nameof(UnknownCorrelationMachine.Unknown), configurationFailure.Message, StringComparison.Ordinal);
        Assert.Contains("was not specified", configurationFailure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "fault-event-conventions-configure-without-overrides")]
    public async Task FaultEvents_ForCorrelatedAndUncorrelatedMessagesConfigureWithoutOverridesAsync()
    {
        var machine = new FaultConventionMachine();
        var repository = new InMemorySagaRepository<FaultConventionState>();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator => configurator.ReceiveEndpoint(
            $"fault-correlation-{NewId.NextGuid():N}",
            endpoint => endpoint.StateMachineSaga(machine, repository)));

        try
        {
            await bus.StartAsync(TestContext.Current.CancellationToken);
            Assert.Contains(machine.CorrelatedFaulted, machine.Events);
            Assert.Contains(machine.UncorrelatedFaulted, machine.Events);
            Assert.IsType<MessageEvent<Fault<CorrelatedFaultMessage>>>(machine.CorrelatedFaulted);
            Assert.IsType<MessageEvent<Fault<UncorrelatedFaultMessage>>>(machine.UncorrelatedFaulted);
        }
        finally
        {
            await bus.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "observer-reports-machine-saga-and-message-contracts")]
    public async Task StateMachineConfigurationObserver_ReportsTheExactMachineSagaAndMessageContractsAsync()
    {
        var observer = new StateMachineConfigurationRecorder();
        var machine = new ObservedMachine();
        var repository = new InMemorySagaRepository<ObservedState>();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator =>
        {
            configurator.ConnectSagaConfigurationObserver(observer);
            configurator.ReceiveEndpoint(
                $"state-machine-observer-{NewId.NextGuid():N}",
                endpoint => endpoint.StateMachineSaga(machine, repository));
        });

        try
        {
            Assert.Equal([typeof(ObservedState)], observer.SagaTypes);
            Assert.Equal([(typeof(ObservedState), typeof(ObservedMachine))], observer.StateMachines);
            Assert.Equal(
                [(typeof(ObservedState), typeof(ObservedStart)), (typeof(ObservedState), typeof(ObservedStop))],
                observer.Messages.OrderBy(item => item.MessageType.Name, StringComparer.Ordinal));
        }
        finally
        {
            await bus.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "include-initial-completes-in-initial-state")]
    public async Task IncludeInitialComposite_FiresOnceWhileTheInstanceRemainsInInitialAsync()
    {
        var machine = new InitialCompositeMachine();
        var instance = new InitialCompositeState();

        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.First);

        Assert.Equal(1, instance.Status);
        Assert.Equal(0, instance.CompositeCount);
        Assert.Same(machine.Initial, instance.CurrentState);

        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.Second);

        Assert.Equal(3, instance.Status);
        Assert.Equal(1, instance.CompositeCount);
        Assert.Equal(["first", "both", "second"], instance.Markers);
        Assert.Same(machine.Initial, instance.CurrentState);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "mismatched-delivery-debug-failure-keeps-rejection-observational")]
    public async Task ScheduledDelivery_DebugFailureCannotChangeTokenRejectionAsync(int deliveryKind, bool loggerThrows)
    {
        var currentToken = Guid.Parse("380f2ab5-8fe3-461f-bf17-5b427c4e3622");
        var foreignToken = Guid.Parse("92cf51a4-c89d-48e4-b8fb-d1762e7e8d70");
        Guid? initialToken = deliveryKind == 1 ? null : currentToken;
        Guid? incomingToken = deliveryKind == 3 ? null : deliveryKind == 2 ? currentToken : foreignToken;
        var machine = new DiagnosticScheduleMachine();
        var instance = new DiagnosticScheduleState
        {
            CorrelationId = Guid.Parse("2ab4ee26-6e91-4cb6-a06a-ff7ff9818e2e"),
            CurrentState = machine.Waiting,
            NoticeTokenId = initialToken
        };
        var headers = new DictionarySendHeaders();
        if (incomingToken.HasValue)
            headers.Set(MessageHeaders.SchedulingTokenId, incomingToken.Value);
        ConsumeContext<DiagnosticScheduleNotice> source = InMemoryOutboxTestContextFactory.Create(
            new DiagnosticScheduleNotice(instance.CorrelationId), TestContext.Current.CancellationToken);
        var headerContext = new DiagnosticScheduleHeaderContext(source, headers);
        var sagaInstance = new SagaInstance<DiagnosticScheduleState>(instance);
        await sagaInstance.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<DiagnosticScheduleState, DiagnosticScheduleNotice>(headerContext, sagaInstance);
        var behaviorContext = new ViciOneServiceBusStateMachine<DiagnosticScheduleState>.BehaviorContextProxy<DiagnosticScheduleNotice>(
            machine, sagaContext, sagaContext, machine.Notice.AnyReceived);
        var loggerFailure = new IOException("unique-scheduled-delivery-debug-failure");
        var logger = new DiagnosticScheduleLogger(loggerThrows ? loggerFailure : null);
        var previousLogContext = LogContext.Current;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            Task operation = ((IStateMachine<DiagnosticScheduleState>)machine).RaiseEventAsync(
                behaviorContext, TestContext.Current.CancellationToken);
            Exception? observed = await Record.ExceptionAsync(() => operation);

            Assert.Equal(incomingToken, headerContext.Headers.Get(MessageHeaders.SchedulingTokenId, default(Guid?)));
            Assert.Same(machine.Waiting, instance.CurrentState);
            bool rejected = deliveryKind is 0 or 1;
            Assert.Equal(rejected ? 0 : 1, instance.ReceivedCount);
            Assert.Equal(rejected ? initialToken : null, instance.NoticeTokenId);
            if (rejected)
            {
                string diagnostic = Assert.Single(logger.Messages);
                Assert.Contains("Scheduled message not current", diagnostic, StringComparison.Ordinal);
                Assert.Contains(foreignToken.ToString(), diagnostic, StringComparison.Ordinal);
            }
            else
                Assert.Empty(logger.Messages);
            Assert.Null(observed);
            Assert.True(operation.IsCompletedSuccessfully);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    public sealed record DiagnosticScheduleNotice(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class DiagnosticScheduleState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState? CurrentState { get; set; }
        public Guid? NoticeTokenId { get; set; }
        public int ReceivedCount { get; set; }
    }

    public sealed class DiagnosticScheduleMachine : ViciOneServiceBusStateMachine<DiagnosticScheduleState>
    {
        public DiagnosticScheduleMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Schedule(() => Notice, instance => instance.NoticeTokenId);
            During(Waiting, When(Notice.Received).Then(context => context.Saga.ReceivedCount++));
        }

        public IState Waiting { get; } = null!;
        public ISchedule<DiagnosticScheduleState, DiagnosticScheduleNotice> Notice { get; } = null!;
    }

    private sealed class DiagnosticScheduleHeaderContext(
        ConsumeContext<DiagnosticScheduleNotice> context, Headers headers) : ConsumeContextProxy<DiagnosticScheduleNotice>(context)
    {
        public override Headers Headers => headers;
    }

    private sealed class DiagnosticScheduleLogger(Exception? failure) : ILogger
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            if (logLevel != LogLevel.Debug || !message.Contains("Scheduled message not current", StringComparison.Ordinal))
                return;
            Messages.Add(message);
            if (failure is not null)
                throw failure;
        }
    }

    public sealed record CorrelationMessage(Guid BusinessId);

    public sealed class CorrelationState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public Guid BusinessId { get; set; }
    }

    public sealed record BaseStart(string Value);

    public sealed record DerivedStop(string Value);

    public sealed class InheritedState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public IState? CurrentState { get; set; }

        public string Value { get; set; } = string.Empty;
    }

    public abstract class BaseMachine<TInstance> : ViciOneServiceBusStateMachine<TInstance>
        where TInstance : class, ISagaStateMachineInstance
    {
        public IState BaseRunning { get; protected set; } = null!;

        public IEvent<BaseStart> BaseStarted { get; protected set; } = null!;
    }

    public sealed class DerivedMachine : BaseMachine<InheritedState>
    {
        public DerivedMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(BaseStarted)
                    .Then(context => context.Saga.Value = context.Message.Value)
                    .TransitionTo(BaseRunning));
            During(
                BaseRunning,
                When(DerivedStopped)
                    .Then(context => context.Saga.Value += $":{context.Message.Value}")
                    .Finalize());
        }

        public IEvent<DerivedStop> DerivedStopped { get; } = null!;
    }

    public sealed record KnownCorrelationMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record UnknownCorrelationMessage(string Value);

    public sealed class UnknownCorrelationState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class UnknownCorrelationMachine : ViciOneServiceBusStateMachine<UnknownCorrelationState>
    {
        public UnknownCorrelationMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Known).TransitionTo(Running));
            During(Running, When(Unknown).Then(_ => { }));
        }

        public IState Running { get; } = null!;

        public IEvent<KnownCorrelationMessage> Known { get; } = null!;

        public IEvent<UnknownCorrelationMessage> Unknown { get; } = null!;
    }

    public sealed record CorrelatedFaultMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record UncorrelatedFaultMessage(Guid CorrelationId);

    public sealed class FaultConventionState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class FaultConventionMachine : ViciOneServiceBusStateMachine<FaultConventionState>
    {
        public FaultConventionMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Running));
            During(
                Running,
                When(CorrelatedFaulted).Then(_ => { }),
                When(UncorrelatedFaulted).Then(_ => { }));
        }

        public IState Running { get; } = null!;

        public IEvent<CorrelatedFaultMessage> Start { get; } = null!;

        public IEvent<Fault<CorrelatedFaultMessage>> CorrelatedFaulted { get; } = null!;

        public IEvent<Fault<UncorrelatedFaultMessage>> UncorrelatedFaulted { get; } = null!;
    }

    public sealed record ObservedStart(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record ObservedStop(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ObservedState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ObservedMachine : ViciOneServiceBusStateMachine<ObservedState>
    {
        public ObservedMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Running));
            During(Running, When(Stop).Finalize());
        }

        public IState Running { get; } = null!;

        public IEvent<ObservedStart> Start { get; } = null!;

        public IEvent<ObservedStop> Stop { get; } = null!;
    }

    public sealed class StateMachineConfigurationRecorder : ISagaConfigurationObserver
    {
        public List<Type> SagaTypes { get; } = [];

        public List<(Type SagaType, Type MachineType)> StateMachines { get; } = [];

        public List<(Type SagaType, Type MessageType)> Messages { get; } = [];

        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
            where TSaga : class => SagaTypes.Add(typeof(TSaga));

        public void StateMachineSagaConfigured<TInstance>(
            ISagaConfigurator<TInstance> configurator,
            object stateMachine)
            where TInstance : class =>
            StateMachines.Add((typeof(TInstance), stateMachine.GetType()));

        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class
            where TMessage : class => Messages.Add((typeof(TSaga), typeof(TMessage)));
    }

    public sealed class InitialCompositeState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public IState? CurrentState { get; set; }

        public int Status { get; set; }

        public int CompositeCount { get; set; }

        public List<string> Markers { get; } = [];
    }

    public sealed class InitialCompositeMachine : ViciOneServiceBusStateMachine<InitialCompositeState>
    {
        public InitialCompositeMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            CompositeEvent(
                () => Both,
                instance => instance.Status,
                CompositeEventOptions.IncludeInitial | CompositeEventOptions.RaiseOnce,
                First,
                Second);
            Initially(
                When(First).Then(context => context.Saga.Markers.Add("first")),
                When(Second).Then(context => context.Saga.Markers.Add("second")),
                When(Both).Then(context =>
                {
                    context.Saga.CompositeCount++;
                    context.Saga.Markers.Add("both");
                }));
        }

        public IEvent First { get; } = null!;

        public IEvent Second { get; } = null!;

        public IEvent Both { get; } = null!;
    }
}
