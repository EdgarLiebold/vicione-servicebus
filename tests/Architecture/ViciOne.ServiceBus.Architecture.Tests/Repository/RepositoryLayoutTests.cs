using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

public sealed class RepositoryLayoutTests
{
    private static readonly string[] ExpectedBuildFiles =
    [
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "benchmarks/nested/Build.props",
        "benchmarks/nested/Build.targets",
        "benchmarks/nested/Project.csproj",
        "samples/nested/Build.props",
        "samples/nested/Build.targets",
        "samples/nested/Project.csproj",
        "signing.props",
        "src/nested/Build.props",
        "src/nested/Build.targets",
        "src/nested/Project.csproj",
        "tests/nested/Build.props",
        "tests/nested/Build.targets",
        "tests/nested/Project.csproj",
        "tools/nested/Build.props",
        "tools/nested/Build.targets",
        "tools/nested/Project.csproj",
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-OWNERSHIP", "build-traversal-preserves-all-governed-trees-and-excludes-ungoverned-inputs")]
    public void BuildFileTraversal_PreservesEveryOwnedTreeAndExcludesUngovernedInputs()
    {
        using var fixture = new ScopeFixture();
        string[] actual = RepositoryLayout.EnumerateGovernedBuildFiles(fixture.Root)
            .Select(path => Path.GetRelativePath(fixture.Root, path).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(ExpectedBuildFiles, actual);
        Assert.DoesNotContain(actual, path => path.StartsWith("review/", StringComparison.Ordinal));
        Assert.DoesNotContain(actual, path => path.StartsWith("TestResults/", StringComparison.Ordinal));
        Assert.DoesNotContain(actual, path => path.StartsWith("artifacts/", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-OWNERSHIP", "build-traversal-does-not-follow-directory-aliases-to-review-inputs")]
    public void BuildFileTraversal_DoesNotFollowDirectoryAliasesToReviewInputs()
    {
        using var fixture = new ScopeFixture();
        Directory.CreateSymbolicLink(
            Path.Combine(fixture.Root, "src", "Alias"),
            Path.Combine(fixture.Root, "review"));

        string[] actual = RepositoryLayout.EnumerateGovernedBuildFiles(fixture.Root)
            .Select(path => Path.GetRelativePath(fixture.Root, path).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(ExpectedBuildFiles, actual);
        Assert.DoesNotContain(actual, path => path.StartsWith("src/Alias/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("src")]
    [InlineData("tests")]
    [InlineData("samples")]
    [InlineData("benchmarks")]
    [InlineData("tools")]
    [RequirementCoverage("REQ-VSB-SOURCE-OWNERSHIP", "build-traversal-fails-if-an-owned-tree-is-missing")]
    public void BuildFileTraversal_FailsWhenAnOwnedTreeIsMissing(string missingTree)
    {
        using var fixture = new ScopeFixture();
        Directory.Delete(Path.Combine(fixture.Root, missingTree), recursive: true);

        Assert.Throws<DirectoryNotFoundException>(() => RepositoryLayout.EnumerateGovernedBuildFiles(fixture.Root));
    }

    private sealed class ScopeFixture : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("vsb-governed-tree-").FullName;

        public ScopeFixture()
        {
            foreach (string tree in new[] { "src", "tests", "samples", "benchmarks", "tools" })
            {
                string directory = Path.Combine(Root, tree, "nested");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "Build.props"), "<Project />");
                File.WriteAllText(Path.Combine(directory, "Build.targets"), "<Project />");
                File.WriteAllText(Path.Combine(directory, "Project.csproj"), "<Project />");
                File.WriteAllText(Path.Combine(directory, "not-build.json"), "{}");
            }

            foreach (string policy in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "signing.props" })
                File.WriteAllText(Path.Combine(Root, policy), "<Project />");

            foreach (string excluded in new[] { "review", "TestResults", "artifacts", "docs", "other" })
            {
                string directory = Path.Combine(Root, excluded);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "Poison.csproj"), "Not a build document.");
            }
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
