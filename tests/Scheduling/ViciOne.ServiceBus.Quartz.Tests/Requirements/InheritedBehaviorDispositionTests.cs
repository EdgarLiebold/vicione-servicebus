using System.Reflection;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Requirements;

public sealed class InheritedBehaviorDispositionTests
{
    private const string DispositionResourceName =
        "ViciOne.ServiceBus.Quartz.Tests.Requirements.QuartzInheritedBehaviorDisposition.json";
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.Quartz.Tests.Requirements.QuartzIntegrationRequirements.json";
    private const string ReplacedExecuting = "REPLACED_EXECUTING";

    private static readonly string[] DispositionProperties =
    [
        "disposition",
        "obligationId",
        "profile",
        "rationale",
        "sourceFile",
        "sourceSymbol",
        "targetRequirement",
        "targetTest",
    ];

    [Fact]
    public void InheritedQuartzObligations_AreCompletelyAndExecutablyDisposed()
    {
        Assembly assembly = typeof(InheritedBehaviorDispositionTests).Assembly;
        using JsonDocument dispositions = ReadEmbeddedJson(assembly, DispositionResourceName);
        using JsonDocument projection = ReadEmbeddedJson(assembly, ProjectionResourceName);

        IReadOnlyDictionary<(string RequirementId, string VariantKey), ProjectedTest> projectedTests =
            ReadProjection(projection.RootElement);
        JsonElement[] entries = dispositions.RootElement.EnumerateArray().ToArray();

        Assert.Equal(87, entries.Length);

        for (var index = 0; index < entries.Length; index++)
        {
            JsonElement entry = entries[index];
            Assert.Equal(DispositionProperties, entry.EnumerateObject().Select(property => property.Name).Order().ToArray());
            Assert.Equal($"OBL-R0-PER-{index + 200:D4}", RequiredString(entry, "obligationId"));
            Assert.StartsWith("tests/Scheduling/ViciOne.ServiceBus.Quartz.Tests/", RequiredString(entry, "sourceFile"));
            Assert.StartsWith("ViciOne.ServiceBus.Quartz.Tests.", RequiredString(entry, "sourceSymbol"));
            Assert.NotEmpty(RequiredString(entry, "rationale"));
        }

        Assert.Equal(85, entries.Count(entry => RequiredString(entry, "disposition") == ReplacedExecuting));
        Assert.Equal(2, entries.Count(entry => RequiredString(entry, "disposition") != ReplacedExecuting));

        foreach (JsonElement entry in entries.Where(entry => RequiredString(entry, "disposition") == ReplacedExecuting))
        {
            Assert.Equal("UnitArchitecture", RequiredString(entry, "profile"));

            string[] requirementParts = RequiredString(entry, "targetRequirement").Split('/');
            Assert.Equal(2, requirementParts.Length);

            var requirementKey = (RequirementId: requirementParts[0], VariantKey: requirementParts[1]);
            Assert.True(
                projectedTests.TryGetValue(requirementKey, out ProjectedTest? projectedTest),
                $"{RequiredString(entry, "obligationId")} targets an unknown requirement variant " +
                $"'{requirementKey.RequirementId}/{requirementKey.VariantKey}'.");
            Assert.Equal(RequiredString(entry, "targetTest"), projectedTest.QualifiedMethodName);

            Type targetType = assembly.GetType(projectedTest.TestType, throwOnError: true, ignoreCase: false)!;
            MethodInfo targetMethod = Assert.Single(
                targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.DeclaredOnly),
                method => string.Equals(method.Name, projectedTest.TestMethod, StringComparison.Ordinal));

            Assert.Equal(1, targetMethod.GetCustomAttributesData()
                .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)));

            RequirementCoverageAttribute coverage =
                Assert.Single(targetMethod.GetCustomAttributes<RequirementCoverageAttribute>(inherit: false));
            Assert.Equal(requirementKey.RequirementId, coverage.RequirementId);
            Assert.Equal(requirementKey.VariantKey, coverage.VariantKey);
        }

        AssertTerminalDisposition(
            entries[12],
            "UPSTREAM_FRAMEWORK_COMPATIBILITY",
            "UPSTREAM-QUARTZ/job-property-binding",
            "Quartz upstream test suite",
            "UPSTREAM");
        AssertTerminalDisposition(
            entries[13],
            "INVALID_DUPLICATE_RETIRED",
            "INVALID-DUPLICATE/no-custom-factory-configured",
            "none",
            "INVALID_SOURCE");
    }

    private static JsonDocument ReadEmbeddedJson(Assembly assembly, string resourceName)
    {
        using Stream resource = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded resource '{resourceName}' is missing.");

        return JsonDocument.Parse(resource, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
        });
    }

    private static IReadOnlyDictionary<(string RequirementId, string VariantKey), ProjectedTest> ReadProjection(
        JsonElement root)
    {
        Assert.Equal(JsonValueKind.Array, root.ValueKind);

        return root.EnumerateArray().ToDictionary(
            entry => (RequiredString(entry, "requirementId"), RequiredString(entry, "variantKey")),
            entry => new ProjectedTest(
                RequiredString(entry, "testType"),
                RequiredString(entry, "testMethod")));
    }

    private static void AssertTerminalDisposition(
        JsonElement entry,
        string disposition,
        string targetRequirement,
        string targetTest,
        string profile)
    {
        Assert.Equal(disposition, RequiredString(entry, "disposition"));
        Assert.Equal(targetRequirement, RequiredString(entry, "targetRequirement"));
        Assert.Equal(targetTest, RequiredString(entry, "targetTest"));
        Assert.Equal(profile, RequiredString(entry, "profile"));
    }

    private static string RequiredString(JsonElement entry, string propertyName)
    {
        Assert.Equal(JsonValueKind.Object, entry.ValueKind);
        Assert.True(entry.TryGetProperty(propertyName, out JsonElement property));
        Assert.Equal(JsonValueKind.String, property.ValueKind);

        string value = property.GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(value));
        Assert.Equal(value.Trim(), value);
        return value;
    }

    private sealed record ProjectedTest(string TestType, string TestMethod)
    {
        public string QualifiedMethodName => $"{TestType}.{TestMethod}";
    }
}
