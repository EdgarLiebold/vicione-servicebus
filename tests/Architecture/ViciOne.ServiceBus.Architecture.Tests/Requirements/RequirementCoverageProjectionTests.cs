using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Requirements;

/// <summary>
/// Compares the immutable architecture requirement projection with passive metadata on compiled
/// test methods.
/// </summary>
/// <remarks>
/// The comparison itself is framework-neutral and shared with every other executable test project, so
/// this projection and the projection of every other test project are read by one implementation rather than
/// by implementations that could drift. What stays here is what only an xUnit assembly can supply: xUnit's own
/// definition of a test method, and the single assertion that turns evidence into a verdict.
/// </remarks>
public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.Architecture.Tests.Requirements.ArchitectureFoundationRequirements.json";

    [Fact]
    public void ArchitectureRequirements_MatchCompiledRequirementMetadata()
    {
        var assembly = typeof(RequirementCoverageProjectionTests).Assembly;

        using var projection = assembly.GetManifestResourceStream(ProjectionResourceName)
            ?? throw new InvalidDataException(
                $"Embedded requirement projection '{ProjectionResourceName}' is missing.");

        var result = RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsExactlyOneXunitTest);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }

    /// <summary>
    /// xUnit's own answer to "is this one test method", kept in the assembly that references xUnit.
    /// </summary>
    /// <remarks>
    /// <c>TheoryAttribute</c> derives from <c>FactAttribute</c>, so a data-driven method counts once
    /// here, exactly as it did before the comparison moved. Counting rather than testing for presence
    /// is what rejects a method carrying two test attributes.
    /// </remarks>
    private static bool IsExactlyOneXunitTest(MethodInfo method) =>
        method.GetCustomAttributesData()
            .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1;
}
