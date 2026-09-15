using ViciOne.ServiceBus.Tests.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Topology.Configuration;
using Xunit;

[assembly: AssemblyFixture(typeof(ViciOne.ServiceBus.Tests.TestAssemblyBootstrap))]

namespace ViciOne.ServiceBus.Tests;

/// <summary>
/// Models the application bootstrap boundary for contract-wide message conventions. The fixture is
/// constructed once before any test creates a runtime topology, after which the catalog is immutable.
/// </summary>
public sealed class TestAssemblyBootstrap
{
    public TestAssemblyBootstrap()
    {
        ApplicationMessageTopology.ExcludeFromConsumeTopology<NonConsumableTopologyMessage>();
        MessageCorrelation.UseCorrelationId<StateMachineLifecycleIntegrationTests.MappedStart>(
            message => message.ServiceId);
        MessageCorrelation.UseCorrelationId<StateMachineLifecycleIntegrationTests.MappedStop>(
            message => message.ServiceId);
        MessageCorrelation.UseCorrelationId<StateMachineTransportIntegrationTests.DynamicStart>(
            message => message.ServiceId);
        MessageCorrelation.UseCorrelationId<StateMachineTransportIntegrationTests.DynamicStop>(
            message => message.ServiceId);
        MessageCorrelation.UseCorrelationId<CorrelationIdConventionTests.OptionalGlobalSelectorMessage>(
            (Func<CorrelationIdConventionTests.OptionalGlobalSelectorMessage, Guid?>)(message => message.SelectedCorrelationId));
    }

    public sealed record NonConsumableTopologyMessage;
}
