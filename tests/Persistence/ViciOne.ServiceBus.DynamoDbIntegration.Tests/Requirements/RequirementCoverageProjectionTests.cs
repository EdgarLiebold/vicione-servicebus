using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Tests.Requirements;

public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.DynamoDbIntegration.Tests.Requirements.DynamoDbRequirements.json";

    [Fact]
    public void DynamoDbRequirements_MatchCompiledRequirementMetadata()
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
