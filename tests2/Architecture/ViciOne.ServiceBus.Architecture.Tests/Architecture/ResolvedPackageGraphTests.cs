using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Architecture;

/// <summary>
/// Assertions over the fully resolved package closure of both projects of this tree.
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

    [Fact]
    public void ExecutableTestProject_ResolvesNoForbiddenPackage()
    {
        var resolved = ResolvedPackageGraph.PackagesOf(TestProject);

        var forbidden = resolved
            .Where(ResolvedPackageGraph.IsForbidden)
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void SupportLibrary_ResolvesNoForbiddenPackage()
    {
        var resolved = ResolvedPackageGraph.PackagesOf(SupportLibrary);

        var forbidden = resolved
            .Where(ResolvedPackageGraph.IsForbidden)
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void ExecutableTestProject_ResolvesTheTestEntryAndItsPlatform()
    {
        // The positive half. Without it the forbidden-package test would still pass on a project
        // that resolved no test platform at all, which is the state where nothing runs and nothing
        // complains.
        var resolved = ResolvedPackageGraph.PackagesOf(TestProject);

        Assert.Contains("xunit.v3.mtp-v2", resolved, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("xunit.v3.core.mtp-v2", resolved, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("TngTech.ArchUnitNET", resolved, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void SupportLibrary_ResolvesNoXunitPackageInItsWholeClosure()
    {
        // Framework neutrality has to hold transitively too: a configuration package that happened
        // to depend on xUnit would breach it just as thoroughly as a direct reference.
        var xunit = ResolvedPackageGraph.PackagesOf(SupportLibrary)
            .Where(package => package.StartsWith("xunit", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(xunit);
    }

    [Fact]
    public void ExclusionPolicy_BansTheVsTestBridgeWithoutBanningTheTestingPlatform()
    {
        // The policy has to separate two things that look alike. Microsoft.Testing.* is the platform
        // this tree runs on and must stay resolvable; the VSTest bridge is a second executor and
        // must not. Banning the prefix would break the tree, banning nothing would let the bridge in.
        Assert.Contains("Microsoft.Testing.Extensions.VSTestBridge", ResolvedPackageGraph.ForbiddenIdentities);
        Assert.DoesNotContain("Microsoft.Testing.Platform", ResolvedPackageGraph.ForbiddenIdentities);

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
        Assert.True(ResolvedPackageGraph.IsForbidden(identity));

    [Fact]
    public void ExclusionPolicy_AllowsOnlyTheArchUnitCore() =>
        Assert.False(ResolvedPackageGraph.IsForbidden("TngTech.ArchUnitNET"));
}
