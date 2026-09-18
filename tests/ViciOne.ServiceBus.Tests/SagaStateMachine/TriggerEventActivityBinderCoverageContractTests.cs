using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class TriggerEventActivityBinderCoverageContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-trigger-conditional-helper-requires-filter")]
    public void ConditionalHelper_RejectsMissingFilterWithStableFailure()
    {
        var machine = new CoverageMachine();
        var binder = new TriggerEventActivityBinder<CoverageSaga>(machine, machine.Trigger);
        MethodInfo helper = typeof(TriggerEventActivityBinder<CoverageSaga>).GetMethod(
            "CreateConditionalActivityBinder",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Xunit.Sdk.XunitException("The conditional activity helper was not found.");

        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => helper.Invoke(binder, null));

        InvalidOperationException failure = Assert.IsType<InvalidOperationException>(invocation.InnerException);
        Assert.Equal("A conditional activity requires a filter.", failure.Message);
        Assert.Empty(binder.GetStateActivityBinders());

        var filteredBinder = new TriggerEventActivityBinder<CoverageSaga>(
            machine,
            machine.Trigger,
            (StateMachineCondition<CoverageSaga>)(_ => true),
            []);
        IActivityBinder<CoverageSaga> conditional = Assert.Single(filteredBinder.GetStateActivityBinders());
        Assert.IsType<ConditionalActivityBinder<CoverageSaga>>(conditional);
        Assert.Same(machine.Trigger, conditional.Event);
    }

    sealed class CoverageMachine : ViciOneServiceBusStateMachine<CoverageSaga>
    {
        public CoverageMachine()
        {
            InstanceState(instance => instance.CurrentState!);
        }

        public IEvent Trigger { get; private set; } = null!;
    }

    sealed class CoverageSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public IState? CurrentState { get; set; }
    }
}
