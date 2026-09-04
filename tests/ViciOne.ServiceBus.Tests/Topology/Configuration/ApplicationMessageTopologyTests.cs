using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class ApplicationMessageTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-TOPOLOGY", "bootstrap-registry-freezes-on-first-bus-topology")]
    public void FirstBusTopology_RejectsEveryLateApplicationConventionMutation()
    {
        _ = Bus.Factory.CreateUsingInMemory(_ => { });

        InvalidOperationException correlation = Assert.Throws<InvalidOperationException>(() =>
            MessageCorrelation.UseCorrelationId<LateContract>(message => message.CorrelationId));
        InvalidOperationException exclusion = Assert.Throws<InvalidOperationException>(() =>
            ApplicationMessageTopology.ExcludeFromConsumeTopology<LateContract>());
        InvalidOperationException separation = Assert.Throws<InvalidOperationException>(
            ApplicationMessageTopology.SeparatePublishFromSendConventions);

        const string expected = "Application message conventions are immutable after the first bus topology is created.";
        Assert.Equal(expected, correlation.Message);
        Assert.Equal(expected, exclusion.Message);
        Assert.Equal(expected, separation.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-TOPOLOGY", "legacy-global-runtime-surfaces-are-not-public")]
    public void RuntimeGlobalTopologyAndEndpointConventionCaches_AreNotPublicApi()
    {
        Type assemblyMarker = typeof(MessageCorrelation);
        Type? globalTopology = assemblyMarker.Assembly.GetType("ViciOne.ServiceBus.Advanced.Topology.GlobalTopology");

        Assert.NotNull(globalTopology);
        Assert.False(globalTopology.IsPublic);
        Assert.Null(assemblyMarker.Assembly.GetType("ViciOne.ServiceBus.GlobalTopology"));
        Assert.Null(assemblyMarker.Assembly.GetType("ViciOne.ServiceBus.IGlobalTopology"));
        Assert.Null(assemblyMarker.Assembly.GetType("ViciOne.ServiceBus.EndpointConventionCache"));
        Assert.Null(assemblyMarker.Assembly.GetType("ViciOne.ServiceBus.IEndpointConventionCache"));
    }

    private sealed record LateContract(Guid CorrelationId);
}
