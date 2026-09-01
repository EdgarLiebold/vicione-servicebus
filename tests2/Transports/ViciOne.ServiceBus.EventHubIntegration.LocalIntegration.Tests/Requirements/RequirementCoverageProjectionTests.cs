namespace ViciOne.ServiceBus.EventHubIntegration.LocalIntegration.Tests.Requirements;

using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.EventHubIntegration.LocalIntegration.Tests.Requirements.EventHubLocalIntegrationRequirements.json";

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-LOCAL-REQUIREMENT-PROJECTION", "compiled-metadata-matches-projection")]
    public void EventHubLocalRequirements_MatchCompiledRequirementMetadata()
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
