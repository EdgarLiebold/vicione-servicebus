using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Tests.Requirements;

/// <summary>Marks generated fixture methods selected by the verifier-test predicate.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ProjectionTestMethodAttribute : Attribute
{
}

public sealed class RequirementCoverageProjectionVerifierTests
{
    private const string FixtureTypeName = "Example.Tests.ProjectionFixture";

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "matching-projection")]
    public void MatchingProjection_ReturnsSuccess()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));

        var result = Verify(assembly, Entry(assembly, "ProvesRequirement", "REQ-1", "variant-1"));

        Assert.True(result.IsSuccess, result.Diagnostics);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "null-assembly")]
    public void NullAssembly_IsRejected()
    {
        using var projection = JsonStream([]);

        Assert.Throws<ArgumentNullException>(() =>
            RequirementCoverageProjectionVerifier.Verify(null!, projection, IsTestMethod));
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "null-projection")]
    public void NullProjection_IsRejected()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));

        Assert.Throws<ArgumentNullException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, null!, IsTestMethod));
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "null-test-predicate")]
    public void NullTestPredicate_IsRejected()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        using var projection = JsonStream(Entry(assembly, "ProvesRequirement", "REQ-1", "variant-1"));

        Assert.Throws<ArgumentNullException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "non-array-root")]
    public void NonArrayRoot_IsRejected()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        using var projection = TextStream("{}");

        var error = Assert.Throws<InvalidDataException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod));

        Assert.Contains("must be a JSON array", error.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "malformed-json")]
    public void MalformedJson_IsRejectedAsInvalidData()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        using var projection = TextStream("[");

        var error = Assert.Throws<InvalidDataException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod));

        Assert.Equal("The requirement projection is not valid JSON.", error.Message);
        Assert.IsAssignableFrom<JsonException>(error.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "non-object-entry")]
    public void NonObjectEntry_IsRejected()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        using var projection = TextStream("[\"not-an-entry\"]");

        var error = Assert.Throws<InvalidDataException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod));

        Assert.Contains("must be a JSON object", error.Message);
    }

    [Theory]
    [InlineData("[{\"requirementId\":\"REQ-1\"}]", "missing")]
    [InlineData("[{\"requirementId\":1,\"variantKey\":\"v\",\"testAssembly\":\"a\",\"testType\":\"t\",\"testMethod\":\"m\"}]", "must be a string")]
    [InlineData("[{\"requirementId\":\"REQ 1\",\"variantKey\":\"v\",\"testAssembly\":\"a\",\"testType\":\"t\",\"testMethod\":\"m\"}]", "non-empty and canonical")]
    [InlineData("[{\"requirementId\":\"REQ-1\",\"variantKey\":\"v\",\"testAssembly\":\"a\",\"testType\":\"t\",\"testMethod\":\"m\",\"extra\":\"x\"}]", "not part of the five-field contract")]
    [InlineData("[{\"requirementId\":\"REQ-1\",\"requirementId\":\"REQ-2\",\"variantKey\":\"v\",\"testAssembly\":\"a\",\"testType\":\"t\",\"testMethod\":\"m\"}]", "occurs more than once")]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "malformed-entry")]
    public void MalformedEntry_IsRejected(string json, string expectedMessage)
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        using var projection = TextStream(json);

        var error = Assert.Throws<InvalidDataException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod));

        Assert.Contains(expectedMessage, error.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "empty-projection")]
    public void EmptyProjection_ReturnsFailure()
    {
        var assembly = BuildAssembly();

        var result = Verify(assembly);

        Assert.False(result.IsSuccess);
        Assert.Contains("binds no requirement variant", result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "duplicate-projected-key")]
    public void DuplicateProjectedRequirementVariant_ReturnsFailure()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        var entry = Entry(assembly, "ProvesRequirement", "REQ-1", "variant-1");

        var result = Verify(assembly, entry, entry);

        Assert.False(result.IsSuccess);
        Assert.Contains("requirement projection contains the requirement-variant key 'REQ-1/variant-1' more than once", result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "duplicate-compiled-key")]
    public void DuplicateCompiledRequirementVariant_ReturnsFailure()
    {
        var assembly = BuildAssembly(
            Method("FirstProof", "REQ-1", "variant-1"),
            Method("SecondProof", "REQ-1", "variant-1"));

        var result = Verify(assembly, Entry(assembly, "FirstProof", "REQ-1", "variant-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains("compiled metadata contains the requirement-variant key 'REQ-1/variant-1' more than once", result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "multiple-coverage-attributes")]
    public void MultipleCoverageAttributesOnOneMethod_AreRejected()
    {
        var assembly = BuildAssembly(new MethodDefinition(
            "ProvesRequirement",
            true,
            [],
            [new CoverageIdentity("REQ-1", "variant-1"), new CoverageIdentity("REQ-2", "variant-2")]));
        using var projection = JsonStream(Entry(assembly, "ProvesRequirement", "REQ-1", "variant-1"));

        var error = Assert.Throws<InvalidDataException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod));

        Assert.Contains("has multiple coverage attributes", error.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "missing-compiled-proof")]
    public void ProjectedRequirementWithoutCompiledProof_ReturnsFailure()
    {
        var assembly = BuildAssembly();

        var result = Verify(assembly, Entry(assembly, "MissingProof", "REQ-1", "variant-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains("resolves to 0 declared methods", result.Diagnostics);
        Assert.Contains("has no compiled test method", result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "unprojected-compiled-proof")]
    public void CompiledProofWithoutProjection_ReturnsFailure()
    {
        var assembly = BuildAssembly(Method("UnexpectedProof", "REQ-1", "variant-1"));

        var result = Verify(assembly);

        Assert.False(result.IsSuccess);
        Assert.Contains("Compiled test method is not part of the requirement projection", result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "wrong-assembly")]
    public void ProjectionForAnotherAssembly_ReturnsFailure()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        var entry = Entry(assembly, "ProvesRequirement", "REQ-1", "variant-1") with
        {
            TestAssembly = "Another.Tests",
        };

        var result = Verify(assembly, entry);

        Assert.False(result.IsSuccess);
        Assert.Contains("names test assembly 'Another.Tests'", result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "unknown-type")]
    public void ProjectionForUnknownType_IsRejected()
    {
        var assembly = BuildAssembly(Method("ProvesRequirement", "REQ-1", "variant-1"));
        var entry = Entry(assembly, "ProvesRequirement", "REQ-1", "variant-1") with
        {
            TestType = "Example.Tests.UnknownType",
        };

        using var projection = JsonStream(entry);
        var error = Assert.Throws<InvalidDataException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod));

        Assert.Contains("does not exist", error.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "overloaded-method")]
    public void ProjectionForOverloadedMethod_ReturnsFailure()
    {
        var assembly = BuildAssembly(
            Method("ProvesRequirement", "REQ-1", "variant-1"),
            new MethodDefinition("ProvesRequirement", false, [typeof(int)], []));

        var result = Verify(assembly, Entry(assembly, "ProvesRequirement", "REQ-1", "variant-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains("resolves to 2 declared methods", result.Diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "attributed-non-test")]
    public void AttributedNonTestMethod_IsRejected()
    {
        var method = new MethodDefinition(
            "NotATest",
            false,
            [],
            [new CoverageIdentity("REQ-1", "variant-1")]);
        var assembly = BuildAssembly(method);
        using var projection = JsonStream(Entry(assembly, "NotATest", "REQ-1", "variant-1"));

        var error = Assert.Throws<InvalidDataException>(() =>
            RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod));

        Assert.Contains("is not exactly one test", error.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "deterministic-diagnostics")]
    public void Diagnostics_AreOrdinallySorted()
    {
        var assembly = BuildAssembly(
            Method("Zeta", "REQ-Z", "variant-z"),
            Method("Alpha", "REQ-A", "variant-a"));

        var result = Verify(assembly);
        var lines = result.Diagnostics.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.False(result.IsSuccess);
        Assert.Equal(lines.Order(StringComparer.Ordinal), lines);
    }

    private static RequirementCoverageVerificationResult Verify(
        Assembly assembly,
        params ProjectionEntry[] entries)
    {
        using var projection = JsonStream(entries);
        return RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsTestMethod);
    }

    private static bool IsTestMethod(MethodInfo method) =>
        method.GetCustomAttributesData()
            .Count(attribute => attribute.AttributeType == typeof(ProjectionTestMethodAttribute)) == 1;

    private static ProjectionEntry Entry(
        Assembly assembly,
        string method,
        string requirement,
        string variant) =>
        new(requirement, variant, assembly.GetName().Name!, FixtureTypeName, method);

    private static MethodDefinition Method(string name, string requirement, string variant) =>
        new(name, true, [], [new CoverageIdentity(requirement, variant)]);

    private static Assembly BuildAssembly(params MethodDefinition[] methods)
    {
        var name = new AssemblyName($"ProjectionFixture_{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(name.Name!);
        var type = module.DefineType(
            FixtureTypeName,
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);

        foreach (var definition in methods)
        {
            var method = type.DefineMethod(
                definition.Name,
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void),
                definition.ParameterTypes);
            method.GetILGenerator().Emit(OpCodes.Ret);

            if (definition.IsTest)
            {
                method.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(ProjectionTestMethodAttribute).GetConstructor(Type.EmptyTypes)!,
                    []));
            }

            foreach (var coverage in definition.Coverage)
            {
                method.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(RequirementCoverageAttribute).GetConstructor([typeof(string), typeof(string)])!,
                    [coverage.RequirementId, coverage.VariantKey]));
            }
        }

        _ = type.CreateType();
        return assembly;
    }

    private static MemoryStream JsonStream(params ProjectionEntry[] entries) =>
        new(JsonSerializer.SerializeToUtf8Bytes(entries, SerializerOptions));

    private static MemoryStream TextStream(string text) =>
        new(Encoding.UTF8.GetBytes(text));

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed record CoverageIdentity(string RequirementId, string VariantKey);

    private sealed record MethodDefinition(
        string Name,
        bool IsTest,
        Type[] ParameterTypes,
        CoverageIdentity[] Coverage);

    private sealed record ProjectionEntry(
        string RequirementId,
        string VariantKey,
        string TestAssembly,
        string TestType,
        string TestMethod);
}
