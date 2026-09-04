using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.Requirements;

public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.ActiveMq.Tests.Requirements.ActiveMqRequirements.json";

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-REQUIREMENT-PROJECTION", "compiled-metadata-matches-projection")]
    public void ActiveMqRequirements_MatchCompiledRequirementMetadata()
    {
        Assembly assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using Stream projection = assembly.GetManifestResourceStream(ProjectionResourceName)
            ?? throw new InvalidDataException($"Embedded requirement projection '{ProjectionResourceName}' is missing.");

        RequirementCoverageVerificationResult result = RequirementCoverageProjectionVerifier.Verify(
            assembly,
            projection,
            method => method.GetCustomAttributesData()
                .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }
}
