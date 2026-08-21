using System.Reflection;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Requirements;

/// <summary>
/// Compares the immutable Lead projection with passive metadata on compiled test methods.
/// </summary>
public sealed class RequirementCoverageProjectionTests
{
    private const string ProjectionResourceName =
        "ViciOne.ServiceBus.Architecture.Tests.Requirements.F1B.ArchitectureFoundation.json";

    private static readonly HashSet<string> RequiredProperties = new(StringComparer.Ordinal)
    {
        "requirementId",
        "variantKey",
        "testAssembly",
        "testType",
        "testMethod",
    };

    [Fact]
    public void LeadBoundProjection_MatchesCompiledRequirementMetadata()
    {
        var assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        var expected = ReadProjection(assembly);
        var actual = ReadCompiledMetadata(assembly);

        Assert.NotEmpty(expected);
        AssertUniqueCoverageKeys(expected, "Lead projection");
        AssertUniqueCoverageKeys(actual, "compiled metadata");
        AssertProjectedMethodsAreUnambiguous(assembly, expected);

        var missing = expected.Except(actual).OrderBy(EntrySortKey, StringComparer.Ordinal).ToArray();
        var unknown = actual.Except(expected).OrderBy(EntrySortKey, StringComparer.Ordinal).ToArray();

        Assert.True(
            missing.Length == 0 && unknown.Length == 0,
            $"Requirement coverage projection differs from compiled test metadata.{Environment.NewLine}" +
            $"Missing compiled tuples: {Format(missing)}{Environment.NewLine}" +
            $"Unprojected compiled tuples: {Format(unknown)}");
    }

    private static IReadOnlyList<CoverageEntry> ReadProjection(Assembly assembly)
    {
        using var stream = assembly.GetManifestResourceStream(ProjectionResourceName)
            ?? throw new InvalidDataException(
                $"Embedded Lead projection '{ProjectionResourceName}' is missing.");
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
        });

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("The Lead projection root must be a JSON array.");
        }

        var entries = new List<CoverageEntry>();

        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Every Lead projection entry must be a JSON object.");
            }

            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var property in element.EnumerateObject())
            {
                if (!RequiredProperties.Contains(property.Name))
                {
                    throw new InvalidDataException(
                        $"Lead projection property '{property.Name}' is not part of the five-field contract.");
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidDataException(
                        $"Lead projection property '{property.Name}' must be a string.");
                }

                var value = property.Value.GetString()!;

                if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
                {
                    throw new InvalidDataException(
                        $"Lead projection property '{property.Name}' must be non-empty and canonical.");
                }

                if (!values.TryAdd(property.Name, value))
                {
                    throw new InvalidDataException(
                        $"Lead projection property '{property.Name}' occurs more than once.");
                }
            }

            var missingProperties = RequiredProperties.Except(values.Keys).Order(StringComparer.Ordinal).ToArray();

            if (missingProperties.Length != 0)
            {
                throw new InvalidDataException(
                    $"Lead projection entry is missing: {string.Join(", ", missingProperties)}.");
            }

            entries.Add(new CoverageEntry(
                values["requirementId"],
                values["variantKey"],
                values["testAssembly"],
                values["testType"],
                values["testMethod"]));
        }

        return entries;
    }

    private static IReadOnlyList<CoverageEntry> ReadCompiledMetadata(Assembly assembly)
    {
        var entries = new List<CoverageEntry>();
        var assemblyName = assembly.GetName().Name
            ?? throw new InvalidDataException("The architecture-test assembly has no simple name.");

        foreach (var type in assembly.DefinedTypes)
        {
            foreach (var method in type.DeclaredMethods)
            {
                var attributes = method
                    .GetCustomAttributes<RequirementCoverageAttribute>(inherit: false)
                    .ToArray();

                if (attributes.Length == 0)
                {
                    continue;
                }

                if (attributes.Length != 1)
                {
                    throw new InvalidDataException(
                        $"Compiled method '{type.FullName}.{method.Name}' has multiple coverage attributes.");
                }

                var testAttributeCount = method.GetCustomAttributesData()
                    .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType));

                if (testAttributeCount != 1)
                {
                    throw new InvalidDataException(
                        $"Attributed method '{type.FullName}.{method.Name}' is not exactly one xUnit test.");
                }

                var attribute = attributes[0];
                entries.Add(new CoverageEntry(
                    attribute.RequirementId,
                    attribute.VariantKey,
                    assemblyName,
                    type.FullName ?? throw new InvalidDataException("An attributed test type has no full name."),
                    method.Name));
            }
        }

        return entries;
    }

    private static void AssertUniqueCoverageKeys(
        IReadOnlyList<CoverageEntry> entries,
        string source)
    {
        var duplicates = entries
            .GroupBy(entry => (entry.RequirementId, entry.VariantKey))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.RequirementId}/{group.Key.VariantKey}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            $"{source} contains duplicate requirement-variant keys: {string.Join(", ", duplicates)}.");
    }

    private static void AssertProjectedMethodsAreUnambiguous(
        Assembly assembly,
        IReadOnlyList<CoverageEntry> entries)
    {
        var assemblyName = assembly.GetName().Name;

        foreach (var entry in entries)
        {
            Assert.Equal(assemblyName, entry.TestAssembly);

            var type = assembly.GetType(entry.TestType, throwOnError: false, ignoreCase: false)
                ?? throw new InvalidDataException(
                    $"Projected test type '{entry.TestType}' does not exist in '{entry.TestAssembly}'.");
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == entry.TestMethod)
                .ToArray();

            Assert.True(
                methods.Length == 1,
                $"Projected method identity '{entry.TestType}.{entry.TestMethod}' resolves to " +
                $"{methods.Length} declared methods; it must resolve to exactly one non-overloaded method.");
        }
    }

    private static string EntrySortKey(CoverageEntry entry) =>
        $"{entry.RequirementId}\u001f{entry.VariantKey}\u001f{entry.TestAssembly}\u001f" +
        $"{entry.TestType}\u001f{entry.TestMethod}";

    private static string Format(IReadOnlyCollection<CoverageEntry> entries) =>
        entries.Count == 0
            ? "none"
            : string.Join(", ", entries.Select(entry =>
                $"[{entry.RequirementId} | {entry.VariantKey} | {entry.TestAssembly} | " +
                $"{entry.TestType} | {entry.TestMethod}]"));

    private sealed record CoverageEntry(
        string RequirementId,
        string VariantKey,
        string TestAssembly,
        string TestType,
        string TestMethod);
}
