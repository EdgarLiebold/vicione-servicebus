using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Architecture.Tests.Dependencies;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Build;

/// <summary>
/// Assertions over the evaluated MSBuild graph of the real projects of this tree.
/// </summary>
/// <remarks>
/// Each subject is what MSBuild actually evaluated, not what the project XML says. Both the
/// executable test projects and the xUnit-free support library are inspected, because most of these
/// rules are only meaningful as a contrast between the two.
/// <para>
/// The executable projects are derived from the evaluated graph rather than named here. A written
/// list would keep passing for the projects on it while a newly added cohort stayed unchecked, and
/// nothing would say so. The three Facts that carry a requirement variant are the deliberate
/// exception: their variant keys name the architecture test project specifically.
/// </para>
/// </remarks>
public sealed class EvaluatedBuildGraphTests
{
    private static readonly string TestProject = RepositoryLayout.ArchitectureTestProject;
    private static IReadOnlyList<string> SupportLibraries => RepositoryLayout.NativeTestProjects
        .Where(project => !MsBuildEvaluation.ItemIdentities(project, "PackageReference")
            .Contains("xunit.v3.mtp-v2", StringComparer.OrdinalIgnoreCase))
        .ToArray();

    /// <summary>
    /// Every project of this tree that carries the Microsoft Testing Platform entry point.
    /// </summary>
    /// <remarks>
    /// Derived from the direct reference that produces the entry point, never from the
    /// <c>IsTestProject</c> declaration the rules below assert. Selecting on the declaration and then
    /// asserting it would be the same statement twice: a project that dropped the declaration would
    /// simply leave the set and take its own violation with it.
    /// </remarks>
    private static IReadOnlyList<string> ExecutableTestProjects => RepositoryLayout.NativeTestProjects
        .Where(project => MsBuildEvaluation.ItemIdentities(project, "PackageReference")
            .Contains("xunit.v3.mtp-v2", StringComparer.OrdinalIgnoreCase))
        .ToArray();

    [Fact]
    public void EveryExecutableTestProject_IsClassifiedAsTestProject()
    {
        Assert.NotEmpty(ExecutableTestProjects);

        foreach (var project in ExecutableTestProjects)
        {
            Assert.Equal("true", MsBuildEvaluation.PropertyOf(project, "IsTestProject"));
            Assert.Equal("true", MsBuildEvaluation.PropertyOf(project, "UseMicrosoftTestingPlatformRunner"));
            Assert.Equal("Exe", MsBuildEvaluation.PropertyOf(project, "OutputType"));
        }
    }

    [Fact]
    public void EveryExecutableTestProject_UsesPortableSymbolsRequiredByMtpDiscovery()
    {
        // The failure this prevents is silent: with the product Release symbol policy the build
        // succeeds and the run discovers zero tests. A cohort added without this exception would
        // simply contribute nothing and report success.
        Assert.NotEmpty(ExecutableTestProjects);

        foreach (var project in ExecutableTestProjects)
        {
            Assert.Equal("true", MsBuildEvaluation.PropertyOf(project, "IsTestingPlatformApplication"));
            Assert.Equal("portable", MsBuildEvaluation.PropertyOf(project, "DebugType"));
        }
    }

    [Fact]
    public void EverySupportLibrary_IsNotClassifiedAsTestProject()
    {
        Assert.NotEmpty(SupportLibraries);

        foreach (var project in SupportLibraries)
        {
            Assert.NotEqual("true", MsBuildEvaluation.PropertyOf(project, "IsTestProject"));
            Assert.NotEqual("Exe", MsBuildEvaluation.PropertyOf(project, "OutputType"));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-203", "architecture-test-project-single-native-test-entry")]
    public void ExecutableTestProject_ReferencesTheSingleTestEntryExactlyOnce()
    {
        var testEntries = MsBuildEvaluation.ItemIdentities(TestProject, "PackageReference")
            .Where(identity => string.Equals(identity, "xunit.v3.mtp-v2", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Single(testEntries);
    }

    [Fact]
    public void EverySupportLibrary_ReferencesNoXunitPackageAtAll()
    {
        Assert.NotEmpty(SupportLibraries);

        var xunitReferences = SupportLibraries
            .SelectMany(project => MsBuildEvaluation.ItemIdentities(project, "PackageReference")
                .Where(identity => identity.Contains("xunit", StringComparison.OrdinalIgnoreCase))
                .Select(identity => $"{RepositoryLayout.RelativeToRoot(project)}:{identity}"))
            .ToArray();

        Assert.Empty(xunitReferences);
    }

    [Fact]
    public void EveryNativeTestProject_InheritsTheRootBuildContract()
    {
        // ArtifactsPath comes only from the repository root Directory.Build.props and
        // ViciOneProjectIdentity only from the repository root Directory.Build.targets. Empty means
        // the corresponding parent import did not happen.
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        foreach (var project in RepositoryLayout.NativeTestProjects)
        {
            Assert.NotEqual(string.Empty, MsBuildEvaluation.PropertyOf(project, "ArtifactsPath"));
            Assert.NotEqual(string.Empty, MsBuildEvaluation.PropertyOf(project, "ViciOneProjectIdentity"));
        }
    }

    [Fact]
    public void EveryNativeTestProject_TargetsTheProductFrameworkAndIsNotPackable()
    {
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        foreach (var project in RepositoryLayout.NativeTestProjects)
        {
            Assert.Equal("net10.0", MsBuildEvaluation.PropertyOf(project, "TargetFramework"));
            Assert.Equal("false", MsBuildEvaluation.PropertyOf(project, "IsPackable"));
        }
    }

    [Fact]
    public void EveryNativeTestProject_EvaluatesToTheSdkLanguageVersion()
    {
        // The .NET 10 SDK derives C# 14, so the evaluated value is 14.0 rather than empty. Asserting
        // the concrete version is what would catch a downgrade; asserting emptiness would only have
        // caught the absence of a property that the SDK sets anyway.
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        foreach (var project in RepositoryLayout.NativeTestProjects)
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

        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        foreach (var project in RepositoryLayout.NativeTestProjects)
        {
            Assert.Equal(product, MsBuildEvaluation.PropertyOf(project, "LangVersion"));
        }
    }

    [Fact]
    public void EveryNativeTestProject_SharesTheSingleUserSecretsStore()
    {
        // One store for the tree. Two ids would mean a secret placed once is invisible to the other
        // projects, which looks like a missing secret and is actually a second store. Counting the
        // distinct values says that directly, and keeps saying it as cohorts are added.
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        var stores = RepositoryLayout.NativeTestProjects
            .Select(project => MsBuildEvaluation.PropertyOf(project, "UserSecretsId"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var shared = Assert.Single(stores);
        Assert.NotEqual(string.Empty, shared);
    }

    [Fact]
    public void EveryNativeTestProject_IsMarkedAsPartOfTheNativeTestTree()
    {
        // The marker selects test-only package versions without allowing a project to classify
        // itself. Test runners and loggers are never injected globally.
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        foreach (var project in RepositoryLayout.NativeTestProjects)
        {
            Assert.Equal("true", MsBuildEvaluation.PropertyOf(project, "ViciOneNativeTestTree"));
        }
    }

    [Fact]
    [RequirementCoverage(
        "REQ-TEST-205",
        "evaluated-product-projects-cannot-enter-native-test-tree")]
    public void ProductProjects_CannotEnterTheNativeTestTreeOrDeclareAnyTestDependency()
    {
        Assert.NotEmpty(RepositoryLayout.ProductProjects);

        foreach (var project in RepositoryLayout.ProductProjects)
        {
            Assert.Equal("false", MsBuildEvaluation.PropertyOf(project, "ViciOneNativeTestTree"));
            var testDependencies = MsBuildEvaluation.ItemIdentities(project, "PackageReference")
                .Where(ResolvedPackageGraph.IsTestDependency)
                .ToArray();

            Assert.Empty(testDependencies);
        }
    }

    [Fact]
    public void TestFrameworkPackage_IsExcludedFromProductDelivery()
    {
        Assert.Equal(
            "false",
            MsBuildEvaluation.PropertyOf(RepositoryLayout.TestFrameworkProject, "IsPackable"));
    }

    [Fact]
    [RequirementCoverage(
        "REQ-TEST-203",
        "architecture-test-project-single-canonical-platform-config-item")]
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
    public void EverySupportLibrary_DoesNotCarryTheTestingPlatformConfiguration()
    {
        Assert.NotEmpty(SupportLibraries);

        var testingPlatformConfigurations = SupportLibraries
            .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Content", "FullPath")
                .Where(path => Path.GetFileName(path) == "testconfig.json")
                .Select(path => $"{RepositoryLayout.RelativeToRoot(project)}:{path}"))
            .ToArray();

        Assert.Empty(testingPlatformConfigurations);
    }
}
