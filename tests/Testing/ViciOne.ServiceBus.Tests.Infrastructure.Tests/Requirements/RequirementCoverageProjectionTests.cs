using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Tests.Requirements;

public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.Tests.Infrastructure.Tests.Requirements.RequirementCoverageVerifierRequirements.json";

    [Fact]
    public void RequirementCoverageVerifierRequirements_MatchCompiledMetadata()
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
