using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class RequestStateMachineOwnershipTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-STATE-MACHINE", "missing-instance-policy-is-instance-owned")]
    public void TwoMachines_ConfigureTheirOwnMissingInstancePoliciesWithoutCrossTalk()
    {
        var firstCalls = 0;
        var secondCalls = 0;

        _ = new RequestStateMachine(configurator =>
        {
            Interlocked.Increment(ref firstCalls);
            configurator.Immediate(1);
        });
        _ = new RequestStateMachine(configurator =>
        {
            Interlocked.Increment(ref secondCalls);
            configurator.Immediate(2);
        });
        _ = new RequestStateMachine();

        Assert.Equal(2, Volatile.Read(ref firstCalls));
        Assert.Equal(2, Volatile.Read(ref secondCalls));
    }
}
