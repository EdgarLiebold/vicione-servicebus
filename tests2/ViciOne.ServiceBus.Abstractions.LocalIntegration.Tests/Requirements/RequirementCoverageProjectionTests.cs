namespace ViciOne.ServiceBus.Abstractions.LocalIntegration.Tests.Requirements;

using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.Abstractions.LocalIntegration.Tests.Requirements.AbstractionsLocalIntegrationRequirements.json";

    [Fact]
    public void AbstractionsLocalIntegrationRequirements_MatchCompiledRequirementMetadata()
    {
        var assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using var projection = assembly.GetManifestResourceStream(ProjectionResourceName)
            ?? throw new InvalidDataException($"Embedded requirement projection '{ProjectionResourceName}' is missing.");

        var result = RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsExactlyOneXunitTest);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }

    private static bool IsExactlyOneXunitTest(MethodInfo method) =>
        method.GetCustomAttributesData()
            .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1;
}
