using ViciOne.ServiceBus.Architecture.Tests.Build;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;
/// <summary>
/// Locates the repository and the project files the evaluated-graph tests inspect.
/// </summary>
/// <remarks>
/// Repository markers locate the checkout without a machine-specific path. Project and build-file
/// inventories derive from the governed code trees and exclude filesystem aliases. Discovery reads
/// the checkout; isolated scope fixtures are created outside it.
/// </remarks>
internal static class RepositoryLayout
{
    private static readonly Lazy<DirectoryInfo> RootDirectory = new(Locate);
    private static readonly string[] GovernedTrees = ["src", "tests", "samples", "benchmarks", "tools"];

    /// <summary>Absolute path of the repository root.</summary>
    internal static string Root => RootDirectory.Value.FullName;

    /// <summary>Filesystem path equality for the current operating system.</summary>
    internal static StringComparer PathComparer =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>Filesystem path comparison for the current operating system.</summary>
    internal static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>The executable architecture test project.</summary>
    internal static string ArchitectureTestProject => Path.Combine(
        Root, "tests", "Architecture", "ViciOne.ServiceBus.Architecture.Tests",
        "ViciOne.ServiceBus.Architecture.Tests.csproj");

    /// <summary>The framework-neutral, xUnit-free support library.</summary>
    internal static string TestingSupportProject => Path.Combine(
        Root, "tests", "Testing", "ViciOne.ServiceBus.Tests.Infrastructure",
        "ViciOne.ServiceBus.Tests.Infrastructure.csproj");

    /// <summary>
    /// A product project outside tests, used as the comparison point for settings that must come
    /// from the SDK rather than from this tree.
    /// </summary>
    internal static string ProductComparisonProject => Path.Combine(
        Root, "src", "ViciOne.ServiceBus.Abstractions", "ViciOne.ServiceBus.Abstractions.csproj");

    /// <summary>The removed compatibility-test directory, which must remain absent.</summary>
    internal static string RetiredTestFrameworkDirectory => Path.Combine(
        Root, "src", "ViciOne.ServiceBus.TestFramework");

    /// <summary>The single core test project.</summary>
    internal static string NativeCoreTestProject => Path.Combine(
        Root, "tests", "ViciOne.ServiceBus.Tests", "ViciOne.ServiceBus.Tests.csproj");

    /// <summary>The one central Microsoft Testing Platform configuration of the tree.</summary>
    internal static string CanonicalTestingPlatformConfiguration => Path.Combine(
        Root, "tests", "testconfig.json");

    /// <summary>
    /// Product projects discovered below the source tree.
    /// </summary>
    internal static IReadOnlyList<string> ProductProjects => EnumerateProjects("src");

    /// <summary>Every test project in the current repository tree.</summary>
    internal static IReadOnlyList<string> NativeTestProjects => EnumerateProjects("tests");

    /// <summary>Every compile-verified, non-deliverable sample project.</summary>
    internal static IReadOnlyList<string> SampleProjects => EnumerateProjects("samples");

    /// <summary>Every MSBuild project governed by the repository root build contract.</summary>
    internal static IReadOnlyList<string> GovernedProjects => GovernedTrees
        .SelectMany(EnumerateProjects)
        .ToArray();

    /// <summary>Top-level build policies and files below governed code trees, excluding filesystem aliases.</summary>
    internal static IReadOnlyList<string> GovernedBuildFiles => EnumerateGovernedBuildFiles(Root);

    /// <summary>Enumerates build policies and projects for an explicitly supplied repository root.</summary>
    /// <param name="root">The root containing the governed code trees and top-level build policies.</param>
    /// <returns>Build-file paths in ordinal order, without entering ungoverned trees or filesystem aliases.</returns>
    internal static IReadOnlyList<string> EnumerateGovernedBuildFiles(string root) =>
        EnumerateScopedFiles(root, "*", recursive: false)
            .Concat(GovernedTrees.SelectMany(tree => EnumerateScopedFiles(Path.Combine(root, tree), "*", recursive: true)))
            .Where(IsBuildPolicyFile)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Samples intentionally compiled only against freshly packed packages by their dedicated gate.</summary>
    internal static IReadOnlyList<string> PackageConsumerProjects => SampleProjects
        .Where(project => string.Equals(
            MsBuildEvaluation.PropertyOf(project, "ViciOnePackageConsumer"),
            "true",
            StringComparison.OrdinalIgnoreCase))
        .ToArray();

    /// <summary>Every currently materialized test-profile solution.</summary>
    internal static IReadOnlyList<string> TestProfileSolutions =>
        Directory.GetFiles(Root, "ViciOne.ServiceBus.Tests.*.slnx", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    internal static string RelativeToRoot(string path) =>
        Path.GetRelativePath(Root, path).Replace('\\', '/');

    private static IReadOnlyList<string> EnumerateProjects(string directory) =>
        EnumerateScopedFiles(Path.Combine(Root, directory), "*.csproj", recursive: true)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<string> EnumerateScopedFiles(string directory, string pattern, bool recursive) =>
        Directory.EnumerateFiles(directory, pattern, new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = false,
            AttributesToSkip = FileAttributes.ReparsePoint,
        });

    private static bool IsBuildPolicyFile(string path) =>
        path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);

    private static DirectoryInfo Locate()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")) &&
                File.Exists(Path.Combine(directory.FullName, "ViciOne.ServiceBus.slnx")))
            {
                return directory;
            }
        }

        throw new InvalidOperationException(
            $"No repository root above {AppContext.BaseDirectory}: expected a directory holding both global.json and ViciOne.ServiceBus.slnx.");
    }
}
