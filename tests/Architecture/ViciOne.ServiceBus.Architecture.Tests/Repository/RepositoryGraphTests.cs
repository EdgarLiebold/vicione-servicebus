using System.Text.Json;
using System.Xml.Linq;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

/// <summary>Repository-wide architecture rules derived from the actual project and solution graph.</summary>
public sealed class RepositoryGraphTests
{
    [Fact]
    public void EveryProductProject_StaysIndependentOfTheNativeTestTree()
    {
        Assert.NotEmpty(RepositoryLayout.ProductProjects);

        var violations = RepositoryLayout.ProductProjects
            .SelectMany(project => ProjectReferences(project)
                .Where(reference => RepositoryLayout.RelativeToRoot(reference)
                    .StartsWith("tests/", RepositoryLayout.PathComparison))
                .Select(reference =>
                    $"{RepositoryLayout.RelativeToRoot(project)} -> {RepositoryLayout.RelativeToRoot(reference)}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void EveryRepositoryProject_HasARestoreGraphBesideIt()
    {
        var projects = Directory.GetFiles(
            RepositoryLayout.Root,
            "*.csproj",
            SearchOption.AllDirectories)
            .Where(project => !RepositoryLayout.RelativeToRoot(project)
                .StartsWith("artifacts/", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(projects);

        var missing = projects
            .Where(project => !File.Exists(Path.Combine(
                Path.GetDirectoryName(project)
                    ?? throw new InvalidOperationException($"No directory for {project}."),
                "packages.lock.json")))
            .Select(RepositoryLayout.RelativeToRoot)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void EverySampleProject_IsNotPackable()
    {
        Assert.NotEmpty(RepositoryLayout.SampleProjects);

        var packable = RepositoryLayout.SampleProjects
            .Where(project => MsBuildEvaluation.PropertyOf(project, "IsPackable") != "false")
            .Select(RepositoryLayout.RelativeToRoot)
            .ToArray();

        Assert.Empty(packable);
    }

    [Fact]
    public void NativeTestProjects_DeclareNoLocalPackageVersion()
    {
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        var violations = RepositoryLayout.NativeTestProjects
            .SelectMany(project => XDocument.Load(project).Descendants("PackageReference")
                .Where(reference =>
                    reference.Attribute("Version") is not null ||
                    reference.Attribute("VersionOverride") is not null ||
                    reference.Element("Version") is not null ||
                    reference.Element("VersionOverride") is not null)
                .Select(reference =>
                    $"{RepositoryLayout.RelativeToRoot(project)}:{reference.Attribute("Include")?.Value}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void RepositoryDeclaresNoExactLanguageVersionPin()
    {
        var buildFiles = Directory.EnumerateFiles(RepositoryLayout.Root, "*", SearchOption.AllDirectories)
            .Where(path =>
                path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))
            .Where(path => !RepositoryLayout.RelativeToRoot(path)
                .StartsWith("artifacts/", StringComparison.Ordinal));

        var actualPins = buildFiles
            .SelectMany(path => XDocument.Load(path).Descendants("LangVersion")
                .Where(element => !string.Equals(element.Value.Trim(), "latest", StringComparison.OrdinalIgnoreCase))
                .Select(element => $"{RepositoryLayout.RelativeToRoot(path)}={element.Value.Trim()}"))
            .ToArray();

        Assert.Empty(actualPins);
    }

    [Fact]
    public void RepositorySelectsTheStableDotNetTenChannelWithoutAnSdkPatchPin()
    {
        using var globalJson = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(RepositoryLayout.Root, "global.json")));

        Assert.False(globalJson.RootElement.TryGetProperty("sdk", out _));
        Assert.Equal(
            "Microsoft.Testing.Platform",
            globalJson.RootElement.GetProperty("test").GetProperty("runner").GetString());

        string workflow = File.ReadAllText(Path.Combine(
            RepositoryLayout.Root,
            ".github",
            "workflows",
            "native-tests.yml"));

        Assert.Contains("DOTNET_VERSION: '10.0.x'", workflow, StringComparison.Ordinal);
        Assert.DoesNotMatch("""DOTNET_VERSION:\s*['"]?10\.0\.\d+""", workflow);
    }

    [Fact]
    public void EveryMaterializedProfileSolution_HasAnExecutableTestProject()
    {
        Assert.NotEmpty(RepositoryLayout.TestProfileSolutions);

        foreach (var solution in RepositoryLayout.TestProfileSolutions)
        {
            var executableProjects = SolutionProjects(solution)
                .Where(path => path.StartsWith(
                    Path.Combine(RepositoryLayout.Root, "tests") + Path.DirectorySeparatorChar,
                    RepositoryLayout.PathComparison))
                .Where(File.Exists)
                .Where(path => MsBuildEvaluation.PropertyOf(path, "IsTestProject") == "true")
                .ToArray();

            Assert.True(
                executableProjects.Length > 0,
                $"{Path.GetFileName(solution)} must not exist before its first executable cohort.");
        }
    }

    [Fact]
    public void EveryExecutableNativeTestProject_BelongsToExactlyOneProfile()
    {
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);
        Assert.NotEmpty(RepositoryLayout.TestProfileSolutions);

        var memberships = RepositoryLayout.TestProfileSolutions
            .SelectMany(solution => SolutionProjects(solution)
                .Select(project => (Project: Path.GetFullPath(project), Solution: Path.GetFileName(solution))))
            .ToLookup(entry => entry.Project, entry => entry.Solution, RepositoryLayout.PathComparer);

        var violations = RepositoryLayout.NativeTestProjects
            .Where(project => MsBuildEvaluation.PropertyOf(project, "IsTestProject") == "true")
            .Where(project => memberships[Path.GetFullPath(project)].Count() != 1)
            .Select(project =>
                $"{RepositoryLayout.RelativeToRoot(project)}: {memberships[Path.GetFullPath(project)].Count()} profiles")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void UnitArchitectureProfile_HasExactlyTheExpectedProjectClosure()
    {
        // Exact rather than "contains": a profile that quietly lost a project would still contain
        // everything this list names, and the run would go green having executed less. The product
        // projects are members rather than mere ProjectReference targets, because only membership
        // puts them into the solution configuration mapping.
        var solution = Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Tests.Unit.slnx");
        var actual = SolutionProjects(solution)
            .Select(RepositoryLayout.RelativeToRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "benchmarks/ViciOne.ServiceBus.Benchmark/ViciOne.ServiceBus.Benchmark.csproj",
                "benchmarks/ViciOne.ServiceBus.BenchmarkConsole/ViciOne.ServiceBus.BenchmarkConsole.csproj",
                "src/Persistence/ViciOne.ServiceBus.AmazonS3/ViciOne.ServiceBus.AmazonS3.csproj",
                "src/Persistence/ViciOne.ServiceBus.Azure.Storage/ViciOne.ServiceBus.Azure.Storage.csproj",
                "src/Persistence/ViciOne.ServiceBus.Azure.Table/ViciOne.ServiceBus.Azure.Table.csproj",
                "src/Persistence/ViciOne.ServiceBus.DynamoDb/ViciOne.ServiceBus.DynamoDb.csproj",
                "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ViciOne.ServiceBus.EntityFrameworkCore.csproj",
                "src/Scheduling/ViciOne.ServiceBus.Quartz/ViciOne.ServiceBus.Quartz.csproj",
                "src/Transports/ViciOne.ServiceBus.ActiveMq/ViciOne.ServiceBus.ActiveMq.csproj",
                "src/Transports/ViciOne.ServiceBus.AmazonSqs/ViciOne.ServiceBus.AmazonSqs.csproj",
                "src/Transports/ViciOne.ServiceBus.AzureServiceBus/ViciOne.ServiceBus.AzureServiceBus.csproj",
                "src/Transports/ViciOne.ServiceBus.RabbitMq.Testing/ViciOne.ServiceBus.RabbitMq.Testing.csproj",
                "src/Transports/ViciOne.ServiceBus.RabbitMq/ViciOne.ServiceBus.RabbitMq.csproj",
                "src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/ViciOne.ServiceBus.SqlTransport.PostgreSql.csproj",
                "src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/ViciOne.ServiceBus.SqlTransport.SqlServer.csproj",
                "src/ViciOne.ServiceBus.Abstractions/ViciOne.ServiceBus.Abstractions.csproj",
                "src/ViciOne.ServiceBus.Analyzers.CodeFixes/ViciOne.ServiceBus.Analyzers.CodeFixes.csproj",
                "src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj",
                "src/ViciOne.ServiceBus.MessagePack/ViciOne.ServiceBus.MessagePack.csproj",
                "src/ViciOne.ServiceBus.SignalR/ViciOne.ServiceBus.SignalR.csproj",
                "src/ViciOne.ServiceBus.StateMachineVisualizer/ViciOne.ServiceBus.StateMachineVisualizer.csproj",
                "src/ViciOne.ServiceBus.Testing/ViciOne.ServiceBus.Testing.csproj",
                "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj",
                "tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj",
                "tests/Benchmarks/ViciOne.ServiceBus.Benchmark.Tests/ViciOne.ServiceBus.Benchmark.Tests.csproj",
                "tests/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/ViciOne.ServiceBus.AmazonS3.Tests.csproj",
                "tests/Persistence/ViciOne.ServiceBus.Azure.Storage.Tests/ViciOne.ServiceBus.Azure.Storage.Tests.csproj",
                "tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests/ViciOne.ServiceBus.Azure.Table.Tests.csproj",
                "tests/Persistence/ViciOne.ServiceBus.DynamoDb.Tests/ViciOne.ServiceBus.DynamoDb.Tests.csproj",
                "tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Tests/ViciOne.ServiceBus.EntityFrameworkCore.Tests.csproj",
                "tests/Scheduling/ViciOne.ServiceBus.Quartz.Tests/ViciOne.ServiceBus.Quartz.Tests.csproj",
                "tests/Testing/ViciOne.ServiceBus.Analyzers.Tests.Infrastructure/ViciOne.ServiceBus.Analyzers.Tests.Infrastructure.csproj",
                "tests/Testing/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure.Tests/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure.Tests.csproj",
                "tests/Testing/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure.csproj",
                "tests/Testing/ViciOne.ServiceBus.Tests.Infrastructure.Tests/ViciOne.ServiceBus.Tests.Infrastructure.Tests.csproj",
                "tests/Testing/ViciOne.ServiceBus.Tests.Infrastructure/ViciOne.ServiceBus.Tests.Infrastructure.csproj",
                "tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/ViciOne.ServiceBus.Tests.InternalAccess.csproj",
                "tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests/ViciOne.ServiceBus.Diagnostics.Tests.csproj",
                "tests/Transports/ViciOne.ServiceBus.ActiveMq.Tests/ViciOne.ServiceBus.ActiveMq.Tests.csproj",
                "tests/Transports/ViciOne.ServiceBus.AmazonSqs.Tests/ViciOne.ServiceBus.AmazonSqs.Tests.csproj",
                "tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests/ViciOne.ServiceBus.AzureServiceBus.Tests.csproj",
                "tests/Transports/ViciOne.ServiceBus.RabbitMq.Tests/ViciOne.ServiceBus.RabbitMq.Tests.csproj",
                "tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests/ViciOne.ServiceBus.SqlTransport.Tests.csproj",
                "tests/ViciOne.ServiceBus.Abstractions.Tests/ViciOne.ServiceBus.Abstractions.Tests.csproj",
                "tests/ViciOne.ServiceBus.Analyzers.CodeFixes.Tests/ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.csproj",
                "tests/ViciOne.ServiceBus.Analyzers.Tests/ViciOne.ServiceBus.Analyzers.Tests.csproj",
                "tests/ViciOne.ServiceBus.MessagePack.Tests/ViciOne.ServiceBus.MessagePack.Tests.csproj",
                "tests/ViciOne.ServiceBus.SignalR.Tests/ViciOne.ServiceBus.SignalR.Tests.csproj",
                "tests/ViciOne.ServiceBus.StateMachineVisualizer.Tests/ViciOne.ServiceBus.StateMachineVisualizer.Tests.csproj",
                "tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj",
                "tools/diagnostics/ViciOne.ServiceBus.Diagnostics/ViciOne.ServiceBus.Diagnostics.csproj",
            ],
            actual);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-203", "unit-profile-contains-complete-project-reference-closure")]
    public void UnitArchitectureProfile_ContainsEveryProjectReferenceTargetInItsClosure()
    {
        var solution = Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Tests.Unit.slnx");
        var members = SolutionProjects(solution)
            .Select(Path.GetFullPath)
            .ToHashSet(RepositoryLayout.PathComparer);

        var missing = members
            .SelectMany(project => ProjectReferences(project, "Release")
                .Where(reference => !members.Contains(Path.GetFullPath(reference)))
                .Select(reference =>
                    $"{RepositoryLayout.RelativeToRoot(project)} -> {RepositoryLayout.RelativeToRoot(reference)}"))
            .OrderBy(edge => edge, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void EngineeringSolution_ContainsEveryNativeTestProject()
    {
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        var engineering = Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Engineering.slnx");
        var members = SolutionProjects(engineering)
            .Select(Path.GetFullPath)
            .ToHashSet(RepositoryLayout.PathComparer);

        var missing = RepositoryLayout.NativeTestProjects
            .Where(project => !members.Contains(Path.GetFullPath(project)))
            .Select(RepositoryLayout.RelativeToRoot)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    [RequirementCoverage(
        "REQ-VSB-TESTING-PACKAGE-BOUNDARY",
        "dedicated-testing-projects-and-runtime-independent-shipping-graph")]
    public void TestingHarnesses_AreDedicatedProjectsOutsideTheRuntimeShippingGraph()
    {
        string[] expectedTestingProjects =
        [
            "src/Transports/ViciOne.ServiceBus.AzureServiceBus.Testing/ViciOne.ServiceBus.AzureServiceBus.Testing.csproj",
            "src/Transports/ViciOne.ServiceBus.EventHubs.Testing/ViciOne.ServiceBus.EventHubs.Testing.csproj",
            "src/Transports/ViciOne.ServiceBus.RabbitMq.Testing/ViciOne.ServiceBus.RabbitMq.Testing.csproj",
            "src/ViciOne.ServiceBus.Testing/ViciOne.ServiceBus.Testing.csproj",
        ];
        string[] testingProjects = RepositoryLayout.ProductProjects
            .Where(project => Path.GetFileNameWithoutExtension(project)
                .EndsWith(".Testing", StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedTestingProjects, testingProjects);

        var testingProjectPaths = testingProjects
            .Select(path => Path.GetFullPath(path, RepositoryLayout.Root))
            .ToHashSet(RepositoryLayout.PathComparer);
        var shippingMembers = SolutionProjects(Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.slnx"))
            .ToHashSet(RepositoryLayout.PathComparer);
        var engineeringMembers = SolutionProjects(Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Engineering.slnx"))
            .ToHashSet(RepositoryLayout.PathComparer);

        Assert.DoesNotContain(testingProjectPaths, shippingMembers.Contains);
        Assert.All(testingProjectPaths, project => Assert.Contains(project, engineeringMembers));

        string[] runtimeReferencesToTesting = RepositoryLayout.ProductProjects
            .Where(project => !testingProjectPaths.Contains(Path.GetFullPath(project)))
            .SelectMany(project => ProjectReferences(project)
                .Where(reference => testingProjectPaths.Contains(Path.GetFullPath(reference)))
                .Select(reference =>
                    $"{RepositoryLayout.RelativeToRoot(project)} -> {RepositoryLayout.RelativeToRoot(reference)}"))
            .OrderBy(edge => edge, StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(runtimeReferencesToTesting);

        string[] embeddedTestingSources = RepositoryLayout.ProductProjects
            .Where(project => !testingProjectPaths.Contains(Path.GetFullPath(project)))
            .SelectMany(project =>
            {
                var projectDirectory = Path.GetDirectoryName(project)
                    ?? throw new InvalidOperationException($"No directory for {project}.");
                return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                    .Select(source => (ProjectDirectory: projectDirectory, Source: source));
            })
            .Where(entry => !entry.Source.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                RepositoryLayout.PathComparison))
            .Where(entry => Path.GetRelativePath(entry.ProjectDirectory, entry.Source)
                .Split(Path.DirectorySeparatorChar)
                .Contains("Testing", StringComparer.Ordinal))
            .Select(entry => RepositoryLayout.RelativeToRoot(entry.Source))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(embeddedTestingSources);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-LAYOUT", "product-paths-have-no-adjacent-repeated-directory")]
    public void ProductSourcePaths_HaveNoAdjacentRepeatedDirectorySegment()
    {
        string[] repeatedSegments = Directory.EnumerateFiles(
                Path.Combine(RepositoryLayout.Root, "src"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(source => !source.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                RepositoryLayout.PathComparison))
            .Select(RepositoryLayout.RelativeToRoot)
            .Where(path =>
            {
                string[] segments = path.Split('/');
                return segments.Zip(segments.Skip(1), StringComparer.Ordinal.Equals).Any(equal => equal);
            })
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(repeatedSegments);
    }

    [Fact]
    public void EngineeringSolution_ContainsEverySampleProject()
    {
        Assert.NotEmpty(RepositoryLayout.SampleProjects);

        var engineering = Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Engineering.slnx");
        var members = SolutionProjects(engineering)
            .Select(Path.GetFullPath)
            .ToHashSet(RepositoryLayout.PathComparer);

        var sourceBoundSamples = RepositoryLayout.SampleProjects
            .Except(RepositoryLayout.PackageConsumerProjects, RepositoryLayout.PathComparer)
            .ToArray();
        var missing = sourceBoundSamples
            .Where(project => !members.Contains(Path.GetFullPath(project)))
            .Select(RepositoryLayout.RelativeToRoot)
            .ToArray();

        Assert.Empty(missing);
        Assert.All(RepositoryLayout.PackageConsumerProjects, project =>
            Assert.DoesNotContain(Path.GetFullPath(project), members));
    }

    [Fact]
    public void EverySolutionProjectPath_Exists()
    {
        var solutions = Directory.GetFiles(
            RepositoryLayout.Root,
            "*.slnx",
            SearchOption.TopDirectoryOnly);
        Assert.NotEmpty(solutions);

        var missing = solutions
            .SelectMany(solution => SolutionProjects(solution)
                .Where(project => !File.Exists(project))
                .Select(project => $"{Path.GetFileName(solution)}:{RepositoryLayout.RelativeToRoot(project)}"))
            .ToArray();

        Assert.Empty(missing);
    }

    private static IReadOnlyList<string> ProjectReferences(string project, string? configuration = null)
    {
        var fullPaths = MsBuildEvaluation.ItemMetadata(
            project,
            "ProjectReference",
            "FullPath",
            configuration);

        if (fullPaths.Count > 0)
        {
            return fullPaths.Select(Path.GetFullPath).ToArray();
        }

        var directory = Path.GetDirectoryName(project)
            ?? throw new InvalidOperationException($"No directory for {project}.");

        var identities = configuration is null
            ? MsBuildEvaluation.ItemIdentities(project, "ProjectReference")
            : MsBuildEvaluation.ItemIdentities(project, "ProjectReference", configuration);

        return identities
            .Select(reference => Path.GetFullPath(reference, directory))
            .ToArray();
    }

    private static IReadOnlyList<string> SolutionProjects(string solution) =>
        XDocument.Load(solution)
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!, RepositoryLayout.Root))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
}
