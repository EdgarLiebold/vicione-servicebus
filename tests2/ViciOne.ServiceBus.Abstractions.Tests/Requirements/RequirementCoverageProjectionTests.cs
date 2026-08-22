using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Requirements;

/// <summary>
/// Compares the immutable MessageBody requirement projection with the passive metadata compiled
/// into this assembly.
/// </summary>
/// <remarks>
/// This is the one Fact of the cohort that carries no <see cref="RequirementCoverageAttribute" />,
/// and deliberately so: it is the method that reads every attribute, so attributing it would make it
/// part of the set it is comparing and it would confirm its own entry.
/// <para>
/// The comparison is the shared framework-neutral verifier. What is supplied here is what only an
/// xUnit assembly can supply - xUnit's own definition of a test method, and the single assertion
/// that turns the returned evidence into a verdict.
/// </para>
/// </remarks>
public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.Abstractions.Tests.Requirements.MessageBodyRequirements.json";

    [Fact]
    public void MessageBodyRequirements_MatchCompiledRequirementMetadata()
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
    /// Counting rather than testing for presence is what rejects a method carrying two test
    /// attributes. <c>TheoryAttribute</c> derives from <c>FactAttribute</c>; this cohort declares no
    /// Theory, and a Theory that appeared would still count once here rather than slip past.
    /// </remarks>
    private static bool IsExactlyOneXunitTest(MethodInfo method) =>
        method.GetCustomAttributesData()
            .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1;
}
