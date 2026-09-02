namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Requirements;

using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RequirementCoverageProjectionTests
{
    private const string ResourceName =
        "ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Requirements.PostgreSqlTransportRequirements.json";

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-POSTGRES-REQUIREMENT-PROJECTION", "compiled-metadata-matches-projection")]
    public void PostgreSqlRequirementsMatchCompiledRequirementMetadata()
    {
        Assembly assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using Stream projection = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"Embedded requirement projection '{ResourceName}' is missing.");

        RequirementCoverageVerificationResult result = RequirementCoverageProjectionVerifier.Verify(
            assembly,
            projection,
            method => method.GetCustomAttributesData()
                .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }
}
