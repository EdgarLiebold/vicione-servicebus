using System.Text.Json;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Architecture;

/// <summary>
/// The single runner configuration, checked in the canonical source and in the built artifact.
/// </summary>
/// <remarks>
/// Two different failures are covered here. The canonical file could say the wrong thing, and the
/// artifact could carry a different file than the canonical one - or two of them, in which case
/// whichever wins is an accident. Both would end in the same silent place: skipped tests and
/// warnings quietly stop being failures and the run still reports success.
/// </remarks>
public sealed class RunnerConfigurationTests
{
    [Fact]
    public void CanonicalConfiguration_TurnsSkipsAndWarningsIntoFailures()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllBytes(RepositoryLayout.CanonicalRunnerConfiguration));

        Assert.True(document.RootElement.GetProperty("failSkips").GetBoolean());
        Assert.True(document.RootElement.GetProperty("failWarns").GetBoolean());
    }

    [Fact]
    public void BuiltArtifact_CarriesTheRunnerConfigurationExactlyOnce()
    {
        var copies = Directory.GetFiles(AppContext.BaseDirectory, "xunit.runner.json", SearchOption.TopDirectoryOnly);

        Assert.Single(copies);
    }

    [Fact]
    public void BuiltArtifact_CarriesTheCanonicalBytes()
    {
        // Byte equality rather than "a file with that name exists": a same-named file from anywhere
        // else would satisfy existence and still change what the runner does.
        var deployed = Path.Combine(AppContext.BaseDirectory, "xunit.runner.json");

        Assert.Equal(
            File.ReadAllBytes(RepositoryLayout.CanonicalRunnerConfiguration),
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
}
