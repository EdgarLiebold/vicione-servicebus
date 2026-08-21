using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Architecture;

/// <summary>
/// Assertions over the evaluated MSBuild graph of the two real projects of this tree.
/// </summary>
/// <remarks>
/// Each subject is what MSBuild actually evaluated, not what the project XML says. Both an
/// executable test project and the xUnit-free support library are inspected, because most of these
/// rules are only meaningful as a contrast between the two.
/// </remarks>
public sealed class EvaluatedBuildGraphTests
{
    private static readonly string TestProject = RepositoryLayout.ArchitectureTestProject;
    private static readonly string SupportLibrary = RepositoryLayout.TestingSupportProject;

    [Fact]
    public void ExecutableTestProject_IsClassifiedAsTestProject()
    {
        Assert.Equal("true", MsBuildEvaluation.PropertyOf(TestProject, "IsTestProject"));
        Assert.Equal("true", MsBuildEvaluation.PropertyOf(TestProject, "UseMicrosoftTestingPlatformRunner"));
        Assert.Equal("Exe", MsBuildEvaluation.PropertyOf(TestProject, "OutputType"));
    }

    [Fact]
    public void ExecutableTestProject_UsesPortableSymbolsRequiredByMtpDiscovery()
    {
        Assert.Equal("true", MsBuildEvaluation.PropertyOf(TestProject, "IsTestingPlatformApplication"));
        Assert.Equal("portable", MsBuildEvaluation.PropertyOf(TestProject, "DebugType"));
    }

    [Fact]
    public void SupportLibrary_IsNotClassifiedAsTestProject()
    {
        // The contrast is the point: a blanket IsTestProject for the whole tree would label this
        // library a test project too, and the classification would stop meaning anything.
        Assert.NotEqual("true", MsBuildEvaluation.PropertyOf(SupportLibrary, "IsTestProject"));
        Assert.NotEqual("Exe", MsBuildEvaluation.PropertyOf(SupportLibrary, "OutputType"));
    }

    [Fact]
    public void ExecutableTestProject_ReferencesTheSingleTestEntryExactlyOnce()
    {
        var testEntries = MsBuildEvaluation.ItemIdentities(TestProject, "PackageReference")
            .Where(identity => identity == "xunit.v3.mtp-v2")
            .ToArray();

        Assert.Single(testEntries);
    }

    [Fact]
    public void SupportLibrary_ReferencesNoXunitPackageAtAll()
    {
        // Framework neutrality is not a comment, it is the absence of any xunit package in the
        // evaluated references of this project.
        var xunitReferences = MsBuildEvaluation.ItemIdentities(SupportLibrary, "PackageReference")
            .Where(identity => identity.Contains("xunit", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(xunitReferences);
    }

    [Fact]
    public void BothProjects_InheritTheRootBuildContract()
    {
        // ArtifactsPath comes only from the repository root Directory.Build.props and
        // ViciOneProjectIdentity only from the repository root Directory.Build.targets. Empty means
        // the corresponding parent import did not happen.
        foreach (var project in new[] { TestProject, SupportLibrary })
        {
            Assert.NotEqual(string.Empty, MsBuildEvaluation.PropertyOf(project, "ArtifactsPath"));
            Assert.NotEqual(string.Empty, MsBuildEvaluation.PropertyOf(project, "ViciOneProjectIdentity"));
        }
    }

    [Fact]
    public void BothProjects_TargetTheProductFrameworkAndAreNotPackable()
    {
        foreach (var project in new[] { TestProject, SupportLibrary })
        {
            Assert.Equal("net10.0", MsBuildEvaluation.PropertyOf(project, "TargetFramework"));
            Assert.Equal("false", MsBuildEvaluation.PropertyOf(project, "IsPackable"));
        }
    }

    [Fact]
    public void BothProjects_EvaluateToTheSdkLanguageVersion()
    {
        // The .NET 10 SDK derives C# 14, so the evaluated value is 14.0 rather than empty. Asserting
        // the concrete version is what would catch a downgrade; asserting emptiness would only have
        // caught the absence of a property that the SDK sets anyway.
        foreach (var project in new[] { TestProject, SupportLibrary })
        {
            Assert.Equal("14.0", MsBuildEvaluation.PropertyOf(project, "LangVersion"));
        }
    }

    [Fact]
    public void NoTreeSpecificPinOverridesTheSdkLanguageVersion()
    {
        // Separate fact, separate failure. A pin anywhere above these projects would make them
        // differ from a project outside tests2; equality with the product project is what shows the
        // version comes from the SDK and not from something this tree set. The repository-wide C# 12
        // pin that was removed earlier would fail exactly here.
        var product = MsBuildEvaluation.PropertyOf(RepositoryLayout.ProductComparisonProject, "LangVersion");

        Assert.Equal(product, MsBuildEvaluation.PropertyOf(TestProject, "LangVersion"));
        Assert.Equal(product, MsBuildEvaluation.PropertyOf(SupportLibrary, "LangVersion"));
    }

    [Fact]
    public void BothProjects_ShareTheSingleUserSecretsStore()
    {
        // One store for the tree. Two ids would mean a secret placed once is invisible to the other
        // project, which looks like a missing secret and is actually a second store.
        var shared = MsBuildEvaluation.PropertyOf(TestProject, "UserSecretsId");

        Assert.NotEqual(string.Empty, shared);
        Assert.Equal(shared, MsBuildEvaluation.PropertyOf(SupportLibrary, "UserSecretsId"));
    }

    [Fact]
    public void BothProjects_AreMarkedAsPartOfTheNativeTestTree()
    {
        // The marker is what keeps the repository-wide GitHubActionsTestLogger
        // GlobalPackageReference out of this tree.
        foreach (var project in new[] { TestProject, SupportLibrary })
        {
            Assert.Equal("true", MsBuildEvaluation.PropertyOf(project, "ViciOneNativeTestTree"));
        }
    }

    [Fact]
    public void ProductProjects_CannotEnterTheNativeTestTreeOrReferenceItsEntryPackage()
    {
        Assert.NotEmpty(RepositoryLayout.ProductProjects);

        foreach (var project in RepositoryLayout.ProductProjects)
        {
            Assert.Equal("false", MsBuildEvaluation.PropertyOf(project, "ViciOneNativeTestTree"));
            Assert.DoesNotContain(
                "xunit.v3.mtp-v2",
                MsBuildEvaluation.ItemIdentities(project, "PackageReference"));
        }
    }

    [Fact]
    public void InheritedTestFramework_IsMigrationInputAndNotPackable()
    {
        Assert.Equal(
            "false",
            MsBuildEvaluation.PropertyOf(RepositoryLayout.InheritedTestFrameworkProject, "IsPackable"));
    }

    [Fact]
    public void ExecutableTestProject_CarriesExactlyOneCanonicalTestingPlatformConfigurationItem()
    {
        var testingPlatformConfigurations = MsBuildEvaluation.ItemMetadata(TestProject, "Content", "FullPath")
            .Where(path => Path.GetFileName(path) == "testconfig.json")
            .ToArray();

        var canonical = Path.GetFullPath(RepositoryLayout.CanonicalTestingPlatformConfiguration);

        Assert.Single(testingPlatformConfigurations);
        Assert.Equal(canonical, Path.GetFullPath(testingPlatformConfigurations[0]));
    }

    [Fact]
    public void SupportLibrary_DoesNotCarryTheTestingPlatformConfiguration()
    {
        // It is not an executable test artifact, so MTP never reads a configuration from it.
        var testingPlatformConfigurations = MsBuildEvaluation.ItemMetadata(SupportLibrary, "Content", "FullPath")
            .Where(path => Path.GetFileName(path) == "testconfig.json")
            .ToArray();

        Assert.Empty(testingPlatformConfigurations);
    }
}
