using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Dependencies;

/// <summary>
/// Assertions over the fully resolved package closure of every project of this tree.
/// </summary>
/// <remarks>
/// These are the checks that the MSBuild-level ones cannot make. A declaration says what a project
/// asked for; the lock file says what restore resolved, transitive entries included. A forbidden
/// runner that arrives through someone else's dependency is invisible to the first and caught by
/// the second.
/// </remarks>
public sealed class ResolvedPackageGraphTests
{
    private static readonly string TestProject = RepositoryLayout.ArchitectureTestProject;
    private static readonly string SupportLibrary = RepositoryLayout.TestingSupportProject;

    public static TheoryData<string> NativeTestProjects
    {
        get
        {
            var projects = new TheoryData<string>();

            foreach (var project in RepositoryLayout.NativeTestProjects)
            {
                projects.Add(RepositoryLayout.RelativeToRoot(project));
            }

            return projects;
        }
    }

    public static TheoryData<string> ExecutableNativeTestProjects
    {
        get
        {
            var projects = new TheoryData<string>();

            foreach (var project in RepositoryLayout.NativeTestProjects.Where(project =>
                         MsBuildEvaluation.ItemIdentities(project, "PackageReference")
                             .Contains("xunit.v3.mtp-v2", StringComparer.OrdinalIgnoreCase)))
            {
                projects.Add(RepositoryLayout.RelativeToRoot(project));
            }

            return projects;
        }
    }

    [Theory]
    [MemberData(nameof(NativeTestProjects))]
    [RequirementCoverage(
        "REQ-TEST-205",
        "resolved-native-test-graphs-contain-no-forbidden-package")]
    public void EveryNativeTestProject_ResolvesNoForbiddenPackage(string relativeProjectPath)
    {
        var project = Path.Combine(RepositoryLayout.Root, relativeProjectPath);
        var resolved = ResolvedPackageGraph.PackagesOf(project);
        var forbiddenIdentities = ResolvedPackageGraph.ForbiddenIdentitiesOf(project);

        var forbidden = resolved
            .Where(package => ResolvedPackageGraph.IsForbidden(package, forbiddenIdentities))
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Theory]
    [MemberData(nameof(ExecutableNativeTestProjects))]
    public void ExecutableTestProject_ResolvesTheTestEntryAndItsPlatform(string relativeProjectPath)
    {
        // The positive half. Without it the forbidden-package test would still pass on a project
        // that resolved no test platform at all, which is the state where nothing runs and nothing
        // complains.
        var project = Path.Combine(RepositoryLayout.Root, relativeProjectPath);

        var resolved = ResolvedPackageGraph.PackagesOf(project);

        Assert.Contains("xunit.v3.mtp-v2", resolved, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("xunit.v3.core.mtp-v2", resolved, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ArchitectureTestProject_ResolvesTheArchUnitCore() =>
        Assert.Contains(
            "TngTech.ArchUnitNET",
            ResolvedPackageGraph.PackagesOf(TestProject),
            StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void SupportLibrary_ResolvesNoTestFrameworkOrPlatformPackage()
    {
        // Framework neutrality has to hold transitively too: a configuration package that happened
        // to depend on a framework, adapter, architecture library or test platform would breach it
        // just as thoroughly as a direct reference.
        var testPackages = ResolvedPackageGraph.PackagesOf(SupportLibrary)
            .Where(package =>
                package.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                package.StartsWith("nunit", StringComparison.OrdinalIgnoreCase) ||
                package.StartsWith("TngTech.ArchUnitNET", StringComparison.OrdinalIgnoreCase) ||
                package.StartsWith("Microsoft.Testing", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(testPackages);
    }

    [Fact]
    public void ExclusionPolicy_BansTheVsTestBridgeWithoutBanningTheTestingPlatform()
    {
        // The policy has to separate two things that look alike. Microsoft.Testing.* is the platform
        // this tree runs on and must stay resolvable; the VSTest bridge is a second executor and
        // must not. Banning the prefix would break the tree, banning nothing would let the bridge in.
        var forbiddenIdentities = ResolvedPackageGraph.ForbiddenIdentitiesOf(TestProject);

        Assert.Contains("Microsoft.Testing.Extensions.VSTestBridge", forbiddenIdentities);
        Assert.DoesNotContain("Microsoft.Testing.Platform", forbiddenIdentities);

        var resolved = ResolvedPackageGraph.PackagesOf(TestProject);

        Assert.Contains("Microsoft.Testing.Platform", resolved, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("TngTech.ArchUnitNET.xUnit")]
    [InlineData("TngTech.ArchUnitNET.xUnitV3")]
    [InlineData("TngTech.ArchUnitNET.NUnit")]
    [InlineData("TngTech.ArchUnitNET.MSTestV2")]
    [InlineData("TngTech.ArchUnitNET.TUnit")]
    public void ExclusionPolicy_BansEveryArchUnitFrameworkAdapter(string identity) =>
        Assert.True(ResolvedPackageGraph.IsForbidden(
            identity,
            ResolvedPackageGraph.ForbiddenIdentitiesOf(TestProject)));

    [Fact]
    public void ExclusionPolicy_AllowsOnlyTheArchUnitCore() =>
        Assert.False(ResolvedPackageGraph.IsForbidden(
            "TngTech.ArchUnitNET",
            ResolvedPackageGraph.ForbiddenIdentitiesOf(TestProject)));

    [Fact]
    public void ProductProjects_ResolveNoTestDependency()
    {
        Assert.NotEmpty(RepositoryLayout.ProductProjects);

        var violations = RepositoryLayout.ProductProjects
            .SelectMany(project => ResolvedPackageGraph.PackagesOf(project)
                .Where(ResolvedPackageGraph.IsTestDependency)
                .Select(package => $"{RepositoryLayout.RelativeToRoot(project)}: {package}"))
            .ToArray();

        Assert.Empty(violations);
    }
}
