using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRecoveryTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "dependency-factory-and-continuation")]
    public async Task FactoryActivity_UsesItsDependencyAndContinuesExactlyOnceAsync(
        StateMachineConstructionStyle style)
    {
        RecoveryScenario scenario = CreateDependencyScenario(style);
        var instance = new RecoveryInstance();

        await StateMachineTestExecution.RaiseAsync(
            scenario.Machine,
            instance,
            scenario.Create,
            new CalculationData(56, 23));

        Assert.Equal("79", instance.Value);
        Assert.Equal(["factory", "activity", "continuation"], instance.Markers);
        Assert.Equal(1, instance.FactoryCount);
        Assert.Equal(1, instance.ActivityCount);
        Assert.Equal(1, instance.ContinuationCount);
        Assert.Same(scenario.Running, instance.CurrentState);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "compensation-restores-and-preserves-failure")]
    public async Task CompensatingActivity_RestoresOriginalValueAndPreservesTheFailureAsync(
        StateMachineConstructionStyle style)
    {
        ExpectedActivityFailureException failure = new("downstream failed");
        RecoveryScenario scenario = CreateCompensationScenario(style, failure);
        var instance = new RecoveryInstance { Value = "original" };

        EventExecutionException exception = await Assert.ThrowsAsync<EventExecutionException>(() =>
            StateMachineTestExecution.RaiseAsync(
                scenario.Machine,
                instance,
                scenario.Create,
                new CalculationData(56, 23)));

        Assert.Same(failure, exception.GetBaseException());
        EventExecutionException compensatedFailure = Assert.IsType<EventExecutionException>(instance.CompensatedFailure);
        Assert.Same(failure, compensatedFailure.InnerException);
        Assert.Equal("original", instance.Value);
        Assert.Equal(["factory", "activity", "downstream", "compensate"], instance.Markers);
        Assert.Equal(1, instance.FactoryCount);
        Assert.Equal(1, instance.ActivityCount);
        Assert.Equal(1, instance.CompensationCount);
        Assert.Equal(0, instance.ContinuationCount);
        Assert.Same(scenario.Machine.Initial, await StateMachineTestExecution.GetStateAsync(scenario.Machine, instance));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "retry-trigger-data-catch-matrix")]
    public async Task Retry_ExhaustsFourAttemptsAndEitherPropagatesOrRunsOneCatchAsync(
        bool dataEvent,
        bool withCatch)
    {
        var machine = new DeclarativeRetryMachine(dataEvent, withCatch);
        var instance = new RetryInstance();

        if (withCatch)
        {
            if (dataEvent)
                await StateMachineTestExecution.RaiseAsync(machine, instance, machine.Data, new RetryData("payload"));
            else
                await StateMachineTestExecution.RaiseAsync(machine, instance, machine.Trigger);
        }
        else
        {
            ExpectedRetryFailureException exception = dataEvent
                ? await Assert.ThrowsAsync<ExpectedRetryFailureException>(() =>
                    StateMachineTestExecution.RaiseAsync(machine, instance, machine.Data, new RetryData("payload")))
                : await Assert.ThrowsAsync<ExpectedRetryFailureException>(() =>
                    StateMachineTestExecution.RaiseAsync(machine, instance, machine.Trigger));

            Assert.Same(instance.Failure, exception);
        }

        Assert.Equal(4, instance.AttemptCount);
        Assert.Equal(withCatch ? 1 : 0, instance.CatchCount);
        Assert.Equal(dataEvent ? 4 : 0, instance.PayloadCount);
        Assert.Equal(0, instance.AfterFailureCount);
        if (withCatch)
            Assert.Same(instance.Failure, instance.CaughtFailure);
        else
            Assert.Null(instance.CaughtFailure);
        Assert.Contains(nameof(Attempt), instance.Failure.StackTrace);
        Assert.Same(machine.Initial, await StateMachineTestExecution.GetStateAsync(machine, instance));
    }

    private static RecoveryScenario CreateDependencyScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeDependencyMachine();
            return new RecoveryScenario(declarativeMachine, declarativeMachine.Running, declarativeMachine.Create);
        }

        State running = null!;
        Event<CalculationData> create = null!;
        ViciOneServiceBusStateMachine<RecoveryInstance> machine = ViciOneServiceBusStateMachine<RecoveryInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Create", out create)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(create, behavior => behavior
                .Execute(context => CreateCalculateActivity(context.Saga, compensate: false))
                .Then(context => MarkContinuation(context.Saga))
                .TransitionTo(running)));

        return new RecoveryScenario(machine, running, create);
    }

    private static RecoveryScenario CreateCompensationScenario(
        StateMachineConstructionStyle style,
        ExpectedActivityFailureException failure)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeCompensationMachine(failure);
            return new RecoveryScenario(declarativeMachine, declarativeMachine.Running, declarativeMachine.Create);
        }

        State running = null!;
        Event<CalculationData> create = null!;
        ViciOneServiceBusStateMachine<RecoveryInstance> machine = ViciOneServiceBusStateMachine<RecoveryInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Create", out create)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(create, behavior => behavior
                .Execute(context => CreateCalculateActivity(context.Saga, compensate: true))
                .Then(context => ThrowDownstream(context.Saga, failure))
                .Then(context => MarkContinuation(context.Saga))
                .TransitionTo(running)));

        return new RecoveryScenario(machine, running, create);
    }

    private static CalculateValueActivity CreateCalculateActivity(RecoveryInstance instance, bool compensate)
    {
        instance.FactoryCount++;
        instance.Markers.Add("factory");
        return new CalculateValueActivity(new LocalCalculator(), compensate);
    }

    private static void MarkContinuation(RecoveryInstance instance)
    {
        instance.ContinuationCount++;
        instance.Markers.Add("continuation");
    }

    private static void ThrowDownstream(RecoveryInstance instance, ExpectedActivityFailureException failure)
    {
        instance.Markers.Add("downstream");
        throw failure;
    }

    private sealed class DeclarativeDependencyMachine : ViciOneServiceBusStateMachine<RecoveryInstance>
    {
        public DeclarativeDependencyMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Create)
                .Execute(context => CreateCalculateActivity(context.Saga, compensate: false))
                .Then(context => MarkContinuation(context.Saga))
                .TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;
        public Event<CalculationData> Create { get; private set; } = null!;
    }

    private sealed class DeclarativeCompensationMachine : ViciOneServiceBusStateMachine<RecoveryInstance>
    {
        public DeclarativeCompensationMachine(ExpectedActivityFailureException failure)
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Create)
                .Execute(context => CreateCalculateActivity(context.Saga, compensate: true))
                .Then(context => ThrowDownstream(context.Saga, failure))
                .Then(context => MarkContinuation(context.Saga))
                .TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;
        public Event<CalculationData> Create { get; private set; } = null!;
    }

    private sealed class DeclarativeRetryMachine : ViciOneServiceBusStateMachine<RetryInstance>
    {
        public DeclarativeRetryMachine(bool dataEvent, bool withCatch)
        {
            InstanceState(instance => instance.CurrentState!);

            if (dataEvent)
            {
                EventActivityBinder<RetryInstance, RetryData> binder = When(Data)
                    .Retry(
                        configurator => configurator.Intervals(0, 0, 0),
                        retry => retry
                            .Then(context => Attempt(context.Saga, context.Message))
                            .Then(context => context.Saga.AfterFailureCount++));

                if (withCatch)
                    binder = binder.Catch<ExpectedRetryFailureException>(caught => caught.Then(context => Capture(context.Saga, context.Exception)));

                During(Initial, binder);
            }
            else
            {
                EventActivityBinder<RetryInstance> binder = When(Trigger)
                    .Retry(
                        configurator => configurator.Intervals(0, 0, 0),
                        retry => retry
                            .Then(context => Attempt(context.Saga))
                            .Then(context => context.Saga.AfterFailureCount++));

                if (withCatch)
                    binder = binder.Catch<ExpectedRetryFailureException>(caught => caught.Then(context => Capture(context.Saga, context.Exception)));

                During(Initial, binder);
            }
        }

        public Event Trigger { get; private set; } = null!;
        public Event<RetryData> Data { get; private set; } = null!;
    }

    private static void Attempt(RetryInstance instance)
    {
        instance.AttemptCount++;
        throw instance.Failure;
    }

    private static void Attempt(RetryInstance instance, RetryData data)
    {
        Assert.Equal("payload", data.Value);
        instance.PayloadCount++;
        Attempt(instance);
    }

    private static void Capture(RetryInstance instance, ExpectedRetryFailureException failure)
    {
        instance.CatchCount++;
        instance.CaughtFailure = failure;
    }

    private sealed class CalculateValueActivity(
        CalculatorService calculator,
        bool compensate) : IStateMachineActivity<RecoveryInstance, CalculationData>
    {
        public async Task ExecuteAsync(
            BehaviorContext<RecoveryInstance, CalculationData> context,
            IBehavior<RecoveryInstance, CalculationData> next)
        {
            string? original = context.Saga.Value;
            context.Saga.ActivityCount++;
            context.Saga.Markers.Add("activity");
            context.Saga.Value = calculator.Add(context.Message.X, context.Message.Y);

            try
            {
                await next.ExecuteAsync(context);
            }
            catch (Exception exception) when (compensate)
            {
                context.Saga.Value = original;
                context.Saga.CompensationCount++;
                context.Saga.CompensatedFailure = exception;
                context.Saga.Markers.Add("compensate");
                throw;
            }
        }

        public Task FaultedAsync<TException>(
            BehaviorExceptionContext<RecoveryInstance, CalculationData, TException> context,
            IBehavior<RecoveryInstance, CalculationData> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context) => context.CreateScope("calculateValue");
    }

    private interface CalculatorService
    {
        string Add(int x, int y);
    }

    private sealed class LocalCalculator : CalculatorService
    {
        public string Add(int x, int y) => (x + y).ToString();
    }

    private sealed class RecoveryInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public State? CurrentState { get; set; }
        public string? Value { get; set; }
        public int FactoryCount { get; set; }
        public int ActivityCount { get; set; }
        public int ContinuationCount { get; set; }
        public int CompensationCount { get; set; }
        public Exception? CompensatedFailure { get; set; }
        public List<string> Markers { get; } = [];
    }

    private sealed class RetryInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public State? CurrentState { get; set; }
        public ExpectedRetryFailureException Failure { get; } = new("retry failed");
        public ExpectedRetryFailureException? CaughtFailure { get; set; }
        public int AttemptCount { get; set; }
        public int CatchCount { get; set; }
        public int PayloadCount { get; set; }
        public int AfterFailureCount { get; set; }
    }

    public sealed record CalculationData(int X, int Y);

    public sealed record RetryData(string Value);

    private sealed record RecoveryScenario(
        ViciOneServiceBusStateMachine<RecoveryInstance> Machine,
        State Running,
        Event<CalculationData> Create);

    private sealed class ExpectedActivityFailureException(string message) : Exception(message);

    private sealed class ExpectedRetryFailureException(string message) : Exception(message);
}
