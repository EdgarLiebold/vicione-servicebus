using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class DataEventActivityBinderCoverageContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-data-conditional-helper-requires-filter")]
    public void ConditionalHelper_PreservesConfiguredFilterAndRejectsMissingFilterWithStableFailure()
    {
        var machine = new CoverageMachine();
        MethodInfo helper = typeof(DataEventActivityBinder<CoverageSaga, CoverageData>).GetMethod(
            "CreateConditionalActivityBinder",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Xunit.Sdk.XunitException("The conditional activity helper was not found.");
        var filtered = new DataEventActivityBinder<CoverageSaga, CoverageData>(machine, machine.Data, _ => true);

        object configured = helper.Invoke(filtered, null)
            ?? throw new Xunit.Sdk.XunitException("The conditional activity helper returned null.");

        var conditional = Assert.IsType<ConditionalActivityBinder<CoverageSaga, CoverageData>>(configured);
        Assert.Same(machine.Data, conditional.Event);

        var binder = new DataEventActivityBinder<CoverageSaga, CoverageData>(machine, machine.Data);

        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => helper.Invoke(binder, null));

        InvalidOperationException failure = Assert.IsType<InvalidOperationException>(invocation.InnerException);
        Assert.Equal("A conditional activity requires a filter.", failure.Message);
        Assert.Empty(binder.GetStateActivityBinders());
        IEventActivityBinder<CoverageSaga, CoverageData> contract = binder;
        Assert.Same(machine, contract.StateMachine);
        Assert.Same(machine.Data, contract.Event);
    }

    sealed class CoverageMachine : ViciOneServiceBusStateMachine<CoverageSaga>
    {
        public CoverageMachine()
        {
            InstanceState(instance => instance.CurrentState!);
        }

        public IEvent<CoverageData> Data { get; private set; } = null!;
    }

    sealed class CoverageSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public IState? CurrentState { get; set; }
    }

    sealed record CoverageData;
}
