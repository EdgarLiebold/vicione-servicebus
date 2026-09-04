using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests.Requirements;

public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.AmazonSqs.Tests.Requirements.AmazonSqsRequirements.json";

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-REQUIREMENT-PROJECTION", "compiled-metadata-matches-projection")]
    public void AmazonSqsRequirements_MatchCompiledRequirementMetadata()
    {
        Assembly assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using Stream projection = assembly.GetManifestResourceStream(ProjectionResourceName)
            ?? throw new InvalidDataException($"Embedded requirement projection '{ProjectionResourceName}' is missing.");

        RequirementCoverageVerificationResult result = RequirementCoverageProjectionVerifier.Verify(
            assembly,
            projection,
            IsExactlyOneXunitTest);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }

    private static bool IsExactlyOneXunitTest(MethodInfo method) =>
        method.GetCustomAttributesData()
            .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1;
}
