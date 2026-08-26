using System.Text.Json;
using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.TestPlatform;

/// <summary>
/// The single MTP configuration, checked in the canonical source and in the built artifact.
/// </summary>
/// <remarks>
/// Two different failures are covered here. The canonical file could say the wrong thing, and the
/// artifact could carry a different file than the canonical one - or two of them, in which case
/// whichever wins is an accident. Both would end in the same silent place: skipped tests and
/// warnings quietly stop being failures and the run still reports success.
/// </remarks>
public sealed class TestingPlatformConfigurationTests
{
    private const int ExpectedUnitTestFloor = 1868;

    [Fact]
    public void CanonicalConfiguration_TurnsSkipsAndWarningsIntoFailures()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllBytes(RepositoryLayout.CanonicalTestingPlatformConfiguration));

        var xunit = document.RootElement.GetProperty("xUnit");

        Assert.True(xunit.GetProperty("failSkips").GetBoolean());
        Assert.True(xunit.GetProperty("failWarns").GetBoolean());
    }

    [Fact]
    public void BuiltArtifact_CarriesTheTestingPlatformConfigurationExactlyOnce()
    {
        // MTP discovers the assembly-specific name in the artifact. Search recursively for the
        // complete name family so a project-specific override cannot coexist unnoticed.
        var copies = Directory.GetFiles(
            AppContext.BaseDirectory,
            "*testconfig.json",
            SearchOption.AllDirectories);

        Assert.Single(copies);
        Assert.Equal(
            $"{typeof(TestingPlatformConfigurationTests).Assembly.GetName().Name}.testconfig.json",
            Path.GetFileName(copies[0]),
            ignoreCase: false);
    }

    [Fact]
    public void SourceTree_CarriesOnlyTheCanonicalTestingPlatformConfiguration()
    {
        var configurations = Directory.GetFiles(
            Path.Combine(RepositoryLayout.Root, "tests2"),
            "*testconfig.json",
            SearchOption.AllDirectories);

        var configuration = Assert.Single(configurations);
        Assert.Equal(
            Path.GetFullPath(RepositoryLayout.CanonicalTestingPlatformConfiguration),
            Path.GetFullPath(configuration));
    }

    [Fact]
    public void SourceTree_CarriesNoNativeXunitRunnerConfiguration()
    {
        var nativeRunnerConfigurations = Directory.GetFiles(
            Path.Combine(RepositoryLayout.Root, "tests2"),
            "*xunit.runner.json",
            SearchOption.AllDirectories);

        Assert.Empty(nativeRunnerConfigurations);
    }

    [Fact]
    public void BuiltArtifact_CarriesTheCanonicalBytes()
    {
        // Byte equality rather than "a file with that name exists": a same-named file from anywhere
        // else would satisfy existence and still change what the runner does.
        var deployed = Path.Combine(
            AppContext.BaseDirectory,
            $"{typeof(TestingPlatformConfigurationTests).Assembly.GetName().Name}.testconfig.json");

        Assert.Equal(
            File.ReadAllBytes(RepositoryLayout.CanonicalTestingPlatformConfiguration),
            File.ReadAllBytes(deployed));
    }

    [Fact]
    public void BuiltArtifact_CarriesTheCheckedInTestSettings()
    {
        var deployed = Path.Combine(AppContext.BaseDirectory, "testsettings.json");

        Assert.True(File.Exists(deployed), $"expected the checked-in defaults at {deployed}");
        Assert.Equal(
            File.ReadAllBytes(Path.Combine(RepositoryLayout.Root, "tests2", "testsettings.json")),
            File.ReadAllBytes(deployed));
    }

    [Fact]
    public void PublicUnitProfileCommands_UseThePredeclaredFloor()
    {
        string[] commandOwners =
        [
            Path.Combine(RepositoryLayout.Root, "README.md"),
            Path.Combine(RepositoryLayout.Root, "docs", "build.md"),
            Path.Combine(RepositoryLayout.Root, ".github", "workflows", "native-tests.yml"),
        ];

        Assert.All(commandOwners, path => Assert.Equal(ExpectedUnitTestFloor, ReadUnitProfileFloor(path)));
    }

    [Fact]
    public void PublicBuildCommands_DoNotUseConcurrentRebuild()
    {
        string[] commandOwners =
        [
            Path.Combine(RepositoryLayout.Root, "README.md"),
            Path.Combine(RepositoryLayout.Root, "docs", "build.md"),
            Path.Combine(RepositoryLayout.Root, ".github", "workflows", "native-tests.yml"),
        ];

        Assert.All(commandOwners, path =>
        {
            var content = Regex.Replace(File.ReadAllText(path), @"\\\r?\n\s*", " ");
            MatchCollection buildCommands = Regex.Matches(
                content,
                @"(?m)^\s*(?:run:\s*)?dotnet\s+build\b[^\r\n]*",
                RegexOptions.CultureInvariant);

            Assert.NotEmpty(buildCommands);
            Assert.All(buildCommands.Cast<Match>(), match => Assert.DoesNotContain("--no-incremental", match.Value));
        });
    }

    private static int ReadUnitProfileFloor(string path)
    {
        const string unitCommandPattern =
            @"dotnet test\s+--solution\s+ViciOne\.ServiceBus\.Tests\.Unit\.slnx.*?--minimum-expected-tests\s+(?<floor>\d+)";
        MatchCollection matches = Regex.Matches(
            File.ReadAllText(path),
            unitCommandPattern,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.True(matches.Count == 1, $"expected exactly one documented Unit profile command in {path}, found {matches.Count}");
        Match match = matches[0];
        return int.Parse(match.Groups["floor"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
