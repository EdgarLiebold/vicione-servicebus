using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Requirements;

public sealed class RequirementCoverageProjectionTests
{
    private const string ResourceName =
        "ViciOne.ServiceBus.SqlTransport.Tests.Requirements.SqlTransportRequirements.json";

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-REQUIREMENT-PROJECTION", "compiled-metadata-matches-projection")]
    public void SqlTransportRequirementsMatchCompiledRequirementMetadata()
    {
        Assembly assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using Stream projection = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"Embedded requirement projection '{ResourceName}' is missing.");

        RequirementCoverageVerificationResult result = RequirementCoverageProjectionVerifier.Verify(
            assembly, projection, method => method.GetCustomAttributesData()
                .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }
}
