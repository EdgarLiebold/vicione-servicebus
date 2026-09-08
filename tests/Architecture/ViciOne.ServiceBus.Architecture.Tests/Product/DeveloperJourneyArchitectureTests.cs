using System.Text.Json;
using System.Xml.Linq;
using ViciOne.ServiceBus.Architecture.Tests.Build;
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

    private static readonly string[] ExpectedDeliveredPackages =
    [
        "ViciOne.ServiceBus",
        "ViciOne.ServiceBus.Abstractions",
        "ViciOne.ServiceBus.ActiveMq",
        "ViciOne.ServiceBus.AmazonS3",
        "ViciOne.ServiceBus.AmazonSqs",
        "ViciOne.ServiceBus.Analyzers",
        "ViciOne.ServiceBus.Azure.Storage",
        "ViciOne.ServiceBus.Azure.Table",
        "ViciOne.ServiceBus.AzureServiceBus",
        "ViciOne.ServiceBus.AzureServiceBus.Testing",
        "ViciOne.ServiceBus.Courier",
        "ViciOne.ServiceBus.DynamoDb",
        "ViciOne.ServiceBus.EntityFrameworkCore",
        "ViciOne.ServiceBus.EntityFrameworkCore.Sagas",
        "ViciOne.ServiceBus.EventHubs",
        "ViciOne.ServiceBus.EventHubs.Testing",
        "ViciOne.ServiceBus.Futures",
        "ViciOne.ServiceBus.Initializers",
        "ViciOne.ServiceBus.JobService",
        "ViciOne.ServiceBus.Mediator",
        "ViciOne.ServiceBus.MessagePack",
        "ViciOne.ServiceBus.Quartz",
        "ViciOne.ServiceBus.RabbitMq",
        "ViciOne.ServiceBus.RabbitMq.Testing",
        "ViciOne.ServiceBus.Sagas",
        "ViciOne.ServiceBus.SignalR",
        "ViciOne.ServiceBus.SqlTransport.PostgreSql",
        "ViciOne.ServiceBus.SqlTransport.SqlServer",
        "ViciOne.ServiceBus.StateMachineVisualizer",
        "ViciOne.ServiceBus.Testing",
    ];

    private static readonly string[] ExpectedRuntimePackages = ExpectedDeliveredPackages
        .Where(static package => package != "ViciOne.ServiceBus.Analyzers")
        .ToArray();

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
    [RequirementCoverage("REQ-VSB-DEVELOPER-JOURNEYS", "fresh-package-lock-evaluation-is-worktree-neutral")]
    public void PackageGate_EvaluatesFreshPackagesAgainstTransientLocks()
    {
        string verifier = Path.Combine(RepositoryLayout.Root, "tools", "ci", "verify_developer_journeys.sh");
        string script = File.ReadAllText(verifier);

        Assert.Contains("temporary_lock_root=\"$temporary_root/locks\"", script, StringComparison.Ordinal);
        Assert.Contains("cp \"$tracked_lock\" \"$transient_lock\"", script, StringComparison.Ordinal);
        Assert.Contains("-p:NuGetLockFilePath=$transient_lock", script, StringComparison.Ordinal);
        Assert.Contains("-p:RestoreLockedMode=true", script, StringComparison.Ordinal);
        Assert.Contains("if $update_lock; then", script, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "fresh-package-api-must-match-versioned-complete-baseline")]
    public void PackageGate_EnforcesVersionedPublicApiBaselineForEveryRuntimePackage()
    {
        string verifier = Path.Combine(RepositoryLayout.Root, "tools", "ci", "verify_developer_journeys.sh");
        string generator = Path.Combine(RepositoryLayout.Root, "tools", "public-api-baseline", "PublicApiBaseline.cs");
        string baseline = Path.Combine(RepositoryLayout.Root, "docs", "api", "packed-public-api.txt");
        string consumer = Path.Combine(
            RepositoryLayout.Root,
            "samples",
            "PackageConsumers",
            "PublicApiBaseline",
            "ViciOne.ServiceBus.Samples.PublicApiBaselinePackageConsumer.csproj");

        Assert.True(File.Exists(generator));
        Assert.True(File.Exists(baseline));
        Assert.True(new FileInfo(baseline).Length > 1_000);
        Assert.True(File.Exists(consumer));
        string script = File.ReadAllText(verifier);
        Assert.Contains("PUBLIC_API_CONTRACT_OUTPUT", script, StringComparison.Ordinal);
        Assert.Contains("--file \"$repository_root/tools/public-api-baseline/PublicApiBaseline.cs\"", script, StringComparison.Ordinal);
        Assert.Contains("\"$global_packages\"", script, StringComparison.Ordinal);
        Assert.Contains("\"$package_feed\"", script, StringComparison.Ordinal);
        Assert.Contains("docs/api/packed-public-api.txt", script, StringComparison.Ordinal);
        Assert.Contains("cmp -s", script, StringComparison.Ordinal);
        Assert.Contains("diff -u", script, StringComparison.Ordinal);
        Assert.Contains("--update-public-api-contract", script, StringComparison.Ordinal);

        XDocument consumerProject = XDocument.Load(consumer);
        Assert.Empty(consumerProject.Descendants("ProjectReference"));
        Assert.Equal("true", consumerProject.Descendants("ViciOnePackageConsumer").Single().Value);
        XElement[] consumerPackages = consumerProject.Descendants("PackageReference")
            .Where(static reference => reference.Attribute("Include")!.Value.StartsWith("ViciOne.", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(ExpectedRuntimePackages, consumerPackages
            .Select(static reference => reference.Attribute("Include")!.Value)
            .Order(StringComparer.Ordinal));
        Assert.All(consumerPackages, static reference => Assert.Equal("1.0.0", reference.Attribute("Version")!.Value));

        using JsonDocument lockFile = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(consumer)!, "packages.lock.json")));
        JsonElement dependencies = lockFile.RootElement.GetProperty("dependencies").GetProperty("net10.0");
        Assert.All(ExpectedRuntimePackages, package =>
        {
            JsonElement dependency = dependencies.GetProperty(package);
            Assert.Equal("Direct", dependency.GetProperty("type").GetString());
            Assert.Equal("1.0.0", dependency.GetProperty("resolved").GetString());
        });

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

        string[] packableProjectPackages = RepositoryLayout.ProductProjects
            .Where(project => string.Equals(
                MsBuildEvaluation.PropertyOf(project, "IsPackable"),
                "true",
                StringComparison.OrdinalIgnoreCase))
            .Select(ReadPackageId)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedDeliveredPackages, packableProjectPackages);

        Assert.All(ExpectedDeliveredPackages, package =>
        {
            Assert.Contains($"\"{package}.1.0.0.nupkg\"", script, StringComparison.Ordinal);
        });

        Assert.Contains("$repository_root/ViciOne.ServiceBus.slnx", script, StringComparison.Ordinal);
        Assert.Contains(
            "$repository_root/src/ViciOne.ServiceBus.Testing/ViciOne.ServiceBus.Testing.csproj",
            script,
            StringComparison.Ordinal);

        string consumerRoot = Path.Combine(RepositoryLayout.Root, "samples", "PackageConsumers");
        string[] actualConsumers = Directory.GetDirectories(consumerRoot)
            .Select(static path => Path.GetFileName(path)
                ?? throw new InvalidOperationException($"Package consumer directory has no name: {path}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            ExpectedIsolatedTestingConsumers.Keys.Append("PublicApiBaseline").Order(StringComparer.Ordinal),
            actualConsumers);

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

    private static string ReadPackageId(string project)
    {
        string? packageId = XDocument.Load(project).Descendants("PackageId")
            .Select(static element => element.Value)
            .SingleOrDefault();
        return packageId ?? Path.GetFileNameWithoutExtension(project);
    }
}
