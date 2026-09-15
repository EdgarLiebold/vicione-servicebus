using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Topology;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "late-capability-metadata-preserves-application-freeze")]
    public void FrozenTopology_AcceptsCapabilityMetadataWithoutReopeningApplicationConfiguration()
    {
        _ = Bus.Factory.CreateUsingInMemory(_ => { });

        bool found = GlobalTopologyTestDriver.RegisterCapabilityCorrelationId<LateCapabilityContract>(
            message => message.CorrelationId);

        Assert.True(found);
        Assert.Throws<InvalidOperationException>(() =>
            MessageCorrelation.UseCorrelationId<AnotherLateContract>(message => message.CorrelationId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "late-nullable-capability-correlation-preserves-value-semantics")]
    public void FrozenTopology_AcceptsNullableCapabilityCorrelationMetadata()
    {
        _ = Bus.Factory.CreateUsingInMemory(_ => { });
        Guid expected = Guid.Parse("6472ceae-78f9-4a49-9239-21a090fc46b7");

        bool found = GlobalTopologyTestDriver.TryResolveCapabilityNullableCorrelationId<LateNullableCapabilityContract>(
            message => message.CorrelationId,
            new LateNullableCapabilityContract(expected),
            out Guid actual);

        Assert.True(found);
        Assert.Equal(expected, actual);
        Assert.Throws<InvalidOperationException>(() =>
            ApplicationMessageTopology.ExcludeFromConsumeTopology<YetAnotherLateContract>());
    }

    private sealed record LateContract(Guid CorrelationId);

    private sealed record LateCapabilityContract(Guid CorrelationId);

    private sealed record AnotherLateContract(Guid CorrelationId);

    private sealed record LateNullableCapabilityContract(Guid? CorrelationId);

    private sealed record YetAnotherLateContract;
}
