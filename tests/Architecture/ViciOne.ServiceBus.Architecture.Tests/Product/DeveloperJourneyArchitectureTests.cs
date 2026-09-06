using System.Text.Json;
using System.Xml.Linq;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class DeveloperJourneyArchitectureTests
{
    private static readonly string[] ExpectedJourneys = Enumerable.Range(1, 18)
        .Select(static value => $"Journey{value:00}")
        .ToArray();

    private static readonly string[] ExpectedViciOnePackages =
    [
        "ViciOne.ServiceBus",
        "ViciOne.ServiceBus.AzureServiceBus",
        "ViciOne.ServiceBus.AzureServiceBus.Testing",
        "ViciOne.ServiceBus.EntityFrameworkCore",
        "ViciOne.ServiceBus.EventHubs",
        "ViciOne.ServiceBus.EventHubs.Testing",
        "ViciOne.ServiceBus.MessagePack",
        "ViciOne.ServiceBus.Quartz",
        "ViciOne.ServiceBus.RabbitMq",
        "ViciOne.ServiceBus.RabbitMq.Testing",
        "ViciOne.ServiceBus.Testing",
    ];

    private static readonly IReadOnlyDictionary<string, string> ExpectedIsolatedTestingConsumers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AzureServiceBusTesting"] = "ViciOne.ServiceBus.AzureServiceBus.Testing",
            ["EventHubsTesting"] = "ViciOne.ServiceBus.EventHubs.Testing",
            ["RabbitMqTesting"] = "ViciOne.ServiceBus.RabbitMq.Testing",
        };

    [Fact]
    [RequirementCoverage("REQ-VSB-DEVELOPER-JOURNEYS", "eighteen-locked-package-only-consumer-scenarios")]
    public void EighteenJourneys_AreLockedPackageConsumersWithoutSourceReferences()
    {
        string directory = Path.Combine(RepositoryLayout.Root, "samples", "DeveloperJourneys");
        string projectPath = Path.Combine(directory, "ViciOne.ServiceBus.Samples.DeveloperJourneys.csproj");
        XDocument project = XDocument.Load(projectPath);

        Assert.Empty(project.Descendants("ProjectReference"));
        Assert.Equal("true", project.Descendants("ViciOnePackageConsumer").Single().Value);
        Assert.DoesNotContain(
            "samples/DeveloperJourneys/ViciOne.ServiceBus.Samples.DeveloperJourneys.csproj",
            File.ReadAllText(Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Engineering.slnx")),
            StringComparison.Ordinal);
        Assert.Equal(ExpectedJourneys, Directory.GetFiles(directory, "Journey*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(static name => name![..9])
            .Order(StringComparer.Ordinal));

        XElement[] packageReferences = project.Descendants("PackageReference").ToArray();
        Assert.Equal(ExpectedViciOnePackages, packageReferences
            .Where(static reference => reference.Attribute("Include")!.Value.StartsWith("ViciOne.", StringComparison.Ordinal))
            .Select(static reference => reference.Attribute("Include")!.Value)
            .Order(StringComparer.Ordinal));
        Assert.All(packageReferences.Where(static reference =>
                reference.Attribute("Include")!.Value.StartsWith("ViciOne.", StringComparison.Ordinal)),
            static reference => Assert.Equal("1.0.0", reference.Attribute("Version")!.Value));

        using JsonDocument lockFile = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "packages.lock.json")));
        JsonElement dependencies = lockFile.RootElement.GetProperty("dependencies").GetProperty("net10.0");
        Assert.All(ExpectedViciOnePackages, package =>
        {
            JsonElement dependency = dependencies.GetProperty(package);
            Assert.Equal("Direct", dependency.GetProperty("type").GetString());
            Assert.Equal("1.0.0", dependency.GetProperty("resolved").GetString());
        });

        string verifier = Path.Combine(RepositoryLayout.Root, "tools", "ci", "verify_developer_journeys.sh");
        Assert.True(File.Exists(verifier));
        string script = File.ReadAllText(verifier);
        Assert.Contains("\"$dotnet_cli\" pack", script, StringComparison.Ordinal);
        Assert.Contains("--no-restore", script, StringComparison.Ordinal);
        Assert.Contains("-p:RestoreLockedMode=true", script, StringComparison.Ordinal);
        Assert.Contains("-p:TreatWarningsAsErrors=true", script, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "fresh-package-assemblies-generate-deterministic-baseline")]
    public void PackageGate_GeneratesAHashedPublicApiBaselineFromRestoredPackageAssemblies()
    {
        string verifier = Path.Combine(RepositoryLayout.Root, "tools", "ci", "verify_developer_journeys.sh");
        string generator = Path.Combine(RepositoryLayout.Root, "tools", "public-api-baseline", "PublicApiBaseline.cs");

        Assert.True(File.Exists(generator));
        string script = File.ReadAllText(verifier);
        Assert.Contains("PUBLIC_API_BASELINE_OUTPUT", script, StringComparison.Ordinal);
        Assert.Contains("--file \"$repository_root/tools/public-api-baseline/PublicApiBaseline.cs\"", script, StringComparison.Ordinal);
        Assert.Contains("\"$global_packages\"", script, StringComparison.Ordinal);
        Assert.Contains("\"$package_feed\"", script, StringComparison.Ordinal);

        string source = File.ReadAllText(generator);
        Assert.Contains("/lib/net10.0/", source, StringComparison.Ordinal);
        Assert.Contains("GetTypes().Where(IsExternallyVisible)", source, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Sha256(package)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Sha256(assemblyFile)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectReference", source, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage(
        "REQ-VSB-TESTING-PACKAGE-CONSUMERS",
        "all-testing-packages-are-packed-consumed-and-ci-required")]
    public void PackageConsumerGate_PacksEveryReferencedViciOnePackageAndRunsInCi()
    {
        string verifier = Path.Combine(RepositoryLayout.Root, "tools", "ci", "verify_developer_journeys.sh");
        string script = File.ReadAllText(verifier);

        Assert.All(ExpectedViciOnePackages, package =>
        {
            Assert.Contains($"/{package}.csproj\"", script, StringComparison.Ordinal);
            Assert.Contains($"\"{package}.1.0.0.nupkg\"", script, StringComparison.Ordinal);
        });

        string consumerRoot = Path.Combine(RepositoryLayout.Root, "samples", "PackageConsumers");
        string[] actualConsumers = Directory.GetDirectories(consumerRoot)
            .Select(static path => Path.GetFileName(path)
                ?? throw new InvalidOperationException($"Package consumer directory has no name: {path}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedIsolatedTestingConsumers.Keys.Order(StringComparer.Ordinal), actualConsumers);

        Assert.All(ExpectedIsolatedTestingConsumers, expected =>
        {
            string directory = Path.Combine(consumerRoot, expected.Key);
            string projectPath = Assert.Single(Directory.GetFiles(directory, "*.csproj"));
            XDocument project = XDocument.Load(projectPath);
            XElement package = Assert.Single(
                project.Descendants("PackageReference"),
                static reference => reference.Attribute("Include")!.Value.StartsWith("ViciOne.", StringComparison.Ordinal));

            Assert.Equal(expected.Value, package.Attribute("Include")!.Value);
            Assert.Equal("1.0.0", package.Attribute("Version")!.Value);
            Assert.Empty(project.Descendants("ProjectReference"));
            Assert.Equal("true", project.Descendants("ViciOnePackageConsumer").Single().Value);
            string relativePath = RepositoryLayout.RelativeToRoot(projectPath);
            Assert.Contains($"$repository_root/{relativePath}", script, StringComparison.Ordinal);
        });

        string workflow = File.ReadAllText(Path.Combine(
            RepositoryLayout.Root,
            ".github",
            "workflows",
            "native-tests.yml"));
        Assert.Contains("tools/ci/verify_developer_journeys.sh", workflow, StringComparison.Ordinal);
    }
}
