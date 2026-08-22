namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

/// <summary>
/// Locates the repository and the project files the evaluated-graph tests inspect.
/// </summary>
/// <remarks>
/// The root is found by walking up from the running artifact until the two marker files that only
/// the repository root carries are both present. Nothing here is a checked-in path list, and the
/// tests never write below the located root: the canonical checkout stays read-only to this suite.
/// </remarks>
internal static class RepositoryLayout
{
    private static readonly Lazy<DirectoryInfo> RootDirectory = new(Locate);

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
        Root, "tests2", "Architecture", "ViciOne.ServiceBus.Architecture.Tests",
        "ViciOne.ServiceBus.Architecture.Tests.csproj");

    /// <summary>The framework-neutral, xUnit-free support library.</summary>
    internal static string TestingSupportProject => Path.Combine(
        Root, "tests2", "Testing", "ViciOne.ServiceBus.Tests.Infrastructure",
        "ViciOne.ServiceBus.Tests.Infrastructure.csproj");

    /// <summary>
    /// A product project outside tests2, used as the comparison point for settings that must come
    /// from the SDK rather than from this tree.
    /// </summary>
    internal static string ProductComparisonProject => Path.Combine(
        Root, "src", "ViciOne.ServiceBus.Abstractions", "ViciOne.ServiceBus.Abstractions.csproj");

    /// <summary>The non-deliverable compatibility test framework awaiting semantic replacement.</summary>
    internal static string TestFrameworkProject => Path.Combine(
        Root, "src", "ViciOne.ServiceBus.TestFramework", "ViciOne.ServiceBus.TestFramework.csproj");

    /// <summary>The one central Microsoft Testing Platform configuration of the tree.</summary>
    internal static string CanonicalTestingPlatformConfiguration => Path.Combine(
        Root, "tests2", "testconfig.json");

    /// <summary>
    /// Every shipped product project, derived from the source tree. The compatibility test framework
    /// temporarily remains under <c>src</c> for semantic comparison and is never product delivery.
    /// </summary>
    internal static IReadOnlyList<string> ProductProjects => EnumerateProjects("src")
        .Where(project => !PathComparer.Equals(project, TestFrameworkProject))
        .ToArray();

    /// <summary>Every native-test project in the current replacement tree.</summary>
    internal static IReadOnlyList<string> NativeTestProjects => EnumerateProjects("tests2");

    /// <summary>Every currently materialized native test-profile solution.</summary>
    internal static IReadOnlyList<string> TestProfileSolutions =>
        Directory.GetFiles(Root, "ViciOne.ServiceBus.Tests.*.slnx", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    internal static string RelativeToRoot(string path) =>
        Path.GetRelativePath(Root, path).Replace('\\', '/');

    private static IReadOnlyList<string> EnumerateProjects(string directory) =>
        Directory.GetFiles(Path.Combine(Root, directory), "*.csproj", SearchOption.AllDirectories)
            .Where(path => !RelativeToRoot(path).StartsWith("artifacts/", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

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
