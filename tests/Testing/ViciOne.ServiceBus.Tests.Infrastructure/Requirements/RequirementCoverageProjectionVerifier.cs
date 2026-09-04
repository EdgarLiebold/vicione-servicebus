using System.Reflection;
using System.Text.Json;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Requirements;

/// <summary>
/// Compares an immutable requirement projection with the passive coverage
/// metadata that a test assembly compiled into itself.
/// </summary>
/// <remarks>
/// The comparison lives here, beside the attribute it reads, because more than one executable test project
/// now makes it. A second copy would be a second definition of what "covered" means, and the two
/// would drift silently: both would stay green while disagreeing about the same projection.
/// <para>
/// Nothing in this file references xUnit or Microsoft Testing Platform. The caller passes in what
/// counts as a test method and owns the single verdict; this type produces evidence and never a
/// result, so no second verdict path can grow out of it.
/// </para>
/// <para>
/// Two failure classes are kept apart.
/// A projection that cannot be read at all - a wrong root, a wrong entry shape, an unknown field, a
/// non-canonical value, a duplicate field, a type that does not exist, a method carrying more than
/// one coverage attribute, or an attributed method that is not a test - throws. There is nothing to
/// compare in those cases, and reporting "no differences" over unreadable input is exactly how a
/// broken projection passes. Everything that is a genuine disagreement between a readable projection
/// and the compiled metadata comes back as a diagnostic, so the caller's own assertion carries it.
/// </para>
/// </remarks>
public static class RequirementCoverageProjectionVerifier
{
    private static readonly HashSet<string> RequiredProperties = new(StringComparer.Ordinal)
    {
        "requirementId",
        "variantKey",
        "testAssembly",
        "testType",
        "testMethod",
    };

    /// <summary>
    /// Compares the projection read from <paramref name="projection" /> with the coverage metadata
    /// compiled into <paramref name="testAssembly" />.
    /// </summary>
    /// <param name="testAssembly">The compiled test assembly whose metadata is the actual side.</param>
    /// <param name="projection">The immutable requirement projection, as a readable JSON stream.</param>
    /// <param name="isTestMethod">
    /// The caller's test-framework definition of a method that is exactly one test case. It is a
    /// parameter rather than a reference so that this assembly stays framework-neutral.
    /// </param>
    /// <returns>An immutable result whose diagnostics are ordinal-sorted and therefore stable.</returns>
    /// <exception cref="ArgumentNullException">An argument is missing.</exception>
    /// <exception cref="InvalidDataException">The projection or the compiled metadata cannot be read.</exception>
    public static RequirementCoverageVerificationResult Verify(
        Assembly testAssembly,
        Stream projection,
        Func<MethodInfo, bool> isTestMethod)
    {
        ArgumentNullException.ThrowIfNull(testAssembly);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(isTestMethod);

        var assemblyName = testAssembly.GetName().Name
            ?? throw new InvalidDataException("The test assembly has no simple name.");

        var expected = ReadProjection(projection);
        var actual = ReadCompiledMetadata(testAssembly, assemblyName, isTestMethod);

        var diagnostics = new List<string>();

        // Fail-closed on an empty projection. Every other check below compares two sets, and two
        // empty sets agree; a projection that bound nothing would otherwise be the quietest possible
        // way to switch this verification off.
        if (expected.Count == 0)
        {
            diagnostics.Add("The requirement projection binds no requirement variant at all.");
        }

        CollectDuplicateCoverageKeys(expected, "requirement projection", diagnostics);
        CollectDuplicateCoverageKeys(actual, "compiled metadata", diagnostics);
        CollectUnresolvableProjectedMethods(testAssembly, assemblyName, expected, diagnostics);

        foreach (var entry in expected.Except(actual))
        {
            diagnostics.Add($"Projected requirement variant has no compiled test method: {Format(entry)}.");
        }

        foreach (var entry in actual.Except(expected))
        {
            diagnostics.Add($"Compiled test method is not part of the requirement projection: {Format(entry)}.");
        }

        return RequirementCoverageVerificationResult.From(diagnostics);
    }

    private static IReadOnlyList<CoverageEntry> ReadProjection(Stream projection)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(projection, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The requirement projection is not valid JSON.", exception);
        }

        using (document)
        {
            return ReadProjection(document.RootElement);
        }
    }

    private static IReadOnlyList<CoverageEntry> ReadProjection(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("The requirement projection root must be a JSON array.");
        }

        var entries = new List<CoverageEntry>();

        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Every requirement projection entry must be a JSON object.");
            }

            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var property in element.EnumerateObject())
            {
                if (!RequiredProperties.Contains(property.Name))
                {
                    throw new InvalidDataException(
                        $"Requirement projection property '{property.Name}' is not part of the five-field contract.");
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidDataException(
                        $"Requirement projection property '{property.Name}' must be a string.");
                }

                var value = property.Value.GetString()!;

                if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
                {
                    throw new InvalidDataException(
                        $"Requirement projection property '{property.Name}' must be non-empty and canonical.");
                }

                if (!values.TryAdd(property.Name, value))
                {
                    throw new InvalidDataException(
                        $"Requirement projection property '{property.Name}' occurs more than once.");
                }
            }

            var missingProperties = RequiredProperties.Except(values.Keys).Order(StringComparer.Ordinal).ToArray();

            if (missingProperties.Length != 0)
            {
                throw new InvalidDataException(
                    $"Requirement projection entry is missing: {string.Join(", ", missingProperties)}.");
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

    private static IReadOnlyList<CoverageEntry> ReadCompiledMetadata(
        Assembly testAssembly,
        string assemblyName,
        Func<MethodInfo, bool> isTestMethod)
    {
        var entries = new List<CoverageEntry>();

        foreach (var type in testAssembly.DefinedTypes)
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

                if (!isTestMethod(method))
                {
                    throw new InvalidDataException(
                        $"Attributed method '{type.FullName}.{method.Name}' is not exactly one test.");
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

    private static void CollectDuplicateCoverageKeys(
        IReadOnlyList<CoverageEntry> entries,
        string source,
        List<string> diagnostics)
    {
        var duplicates = entries
            .GroupBy(entry => (entry.RequirementId, entry.VariantKey))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.RequirementId}/{group.Key.VariantKey}");

        foreach (var duplicate in duplicates)
        {
            diagnostics.Add($"The {source} contains the requirement-variant key '{duplicate}' more than once.");
        }
    }

    private static void CollectUnresolvableProjectedMethods(
        Assembly testAssembly,
        string assemblyName,
        IReadOnlyList<CoverageEntry> entries,
        List<string> diagnostics)
    {
        foreach (var entry in entries)
        {
            // Ordinal, exact: a projection that names another assembly describes another test project, and
            // looking its type up in this one would answer a question nobody asked.
            if (!string.Equals(assemblyName, entry.TestAssembly, StringComparison.Ordinal))
            {
                diagnostics.Add(
                    $"Projected entry names test assembly '{entry.TestAssembly}' but the compiled assembly is " +
                    $"'{assemblyName}': {Format(entry)}.");
                continue;
            }

            var type = testAssembly.GetType(entry.TestType, throwOnError: false, ignoreCase: false)
                ?? throw new InvalidDataException(
                    $"Projected test type '{entry.TestType}' does not exist in '{entry.TestAssembly}'.");
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Count(method => string.Equals(method.Name, entry.TestMethod, StringComparison.Ordinal));

            if (methods != 1)
            {
                diagnostics.Add(
                    $"Projected method identity '{entry.TestType}.{entry.TestMethod}' resolves to " +
                    $"{methods} declared methods; it must resolve to exactly one non-overloaded method.");
            }
        }
    }

    private static string Format(CoverageEntry entry) =>
        $"[{entry.RequirementId} | {entry.VariantKey} | {entry.TestAssembly} | " +
        $"{entry.TestType} | {entry.TestMethod}]";

    private sealed record CoverageEntry(
        string RequirementId,
        string VariantKey,
        string TestAssembly,
        string TestType,
        string TestMethod);
}

/// <summary>
/// The immutable outcome of one projection verification.
/// </summary>
/// <remarks>
/// It carries evidence, not a verdict. The caller's own test framework decides what a failure is;
/// this type only guarantees that the same disagreement always reads the same way, which is what
/// makes a diff of the failure text meaningful.
/// </remarks>
public sealed class RequirementCoverageVerificationResult
{
    private RequirementCoverageVerificationResult(bool isSuccess, string diagnostics)
    {
        IsSuccess = isSuccess;
        Diagnostics = diagnostics;
    }

    /// <summary>Whether projection and compiled metadata agree in every direction.</summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Every disagreement, one per line, ordinal-sorted so the order cannot depend on reflection or
    /// enumeration order. Empty exactly when <see cref="IsSuccess" /> is <see langword="true" />.
    /// </summary>
    public string Diagnostics { get; }

    internal static RequirementCoverageVerificationResult From(IEnumerable<string> diagnostics)
    {
        var ordered = diagnostics.Order(StringComparer.Ordinal).ToArray();

        return new RequirementCoverageVerificationResult(
            ordered.Length == 0,
            string.Join(Environment.NewLine, ordered));
    }
}
