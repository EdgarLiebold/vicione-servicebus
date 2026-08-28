using System.Xml.Linq;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

/// <summary>Repository-wide architecture rules derived from the actual project and solution graph.</summary>
public sealed class RepositoryGraphTests
{
    private static readonly HashSet<string> AllowedLanguageVersionPins =
    [
        "src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj",
        "src/ViciOne.ServiceBus.Analyzers.CodeFixes/ViciOne.ServiceBus.Analyzers.CodeFixes.csproj",
    ];

    [Fact]
    public void EveryProductProject_StaysIndependentOfTheNativeTestTree()
    {
        Assert.NotEmpty(RepositoryLayout.ProductProjects);

        var violations = RepositoryLayout.ProductProjects
            .SelectMany(project => ProjectReferences(project)
                .Where(reference => RepositoryLayout.RelativeToRoot(reference)
                    .StartsWith("tests2/", RepositoryLayout.PathComparison))
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
    public void LanguageVersion_IsPinnedOnlyForTheTwoRoslynComponents()
    {
        var buildFiles = Directory.EnumerateFiles(RepositoryLayout.Root, "*", SearchOption.AllDirectories)
            .Where(path =>
                path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))
            .Where(path => !RepositoryLayout.RelativeToRoot(path)
                .StartsWith("artifacts/", StringComparison.Ordinal));

        var actualPins = buildFiles
            .Where(path => XDocument.Load(path).Descendants("LangVersion").Any())
            .Select(RepositoryLayout.RelativeToRoot)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            AllowedLanguageVersionPins.OrderBy(path => path, StringComparer.Ordinal),
            actualPins.OrderBy(path => path, StringComparer.Ordinal));
    }

    [Fact]
    public void EveryMaterializedProfileSolution_HasAnExecutableTestProject()
    {
        Assert.NotEmpty(RepositoryLayout.TestProfileSolutions);

        foreach (var solution in RepositoryLayout.TestProfileSolutions)
        {
            var executableProjects = SolutionProjects(solution)
                .Where(path => path.StartsWith(
                    Path.Combine(RepositoryLayout.Root, "tests2") + Path.DirectorySeparatorChar,
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
        // Exact rather than "contains": a profile that quietly lost a cohort would still contain
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
                "src/Persistence/ViciOne.ServiceBus.AmazonS3/ViciOne.ServiceBus.AmazonS3.csproj",
                "src/Persistence/ViciOne.ServiceBus.Azure.Table/ViciOne.ServiceBus.Azure.Table.csproj",
                "src/Persistence/ViciOne.ServiceBus.DynamoDbIntegration/ViciOne.ServiceBus.DynamoDbIntegration.csproj",
                "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.csproj",
                "src/Scheduling/ViciOne.ServiceBus.QuartzIntegration/ViciOne.ServiceBus.QuartzIntegration.csproj",
                "src/Transports/ViciOne.ServiceBus.ActiveMqTransport/ViciOne.ServiceBus.ActiveMqTransport.csproj",
                "src/Transports/ViciOne.ServiceBus.AmazonSqsTransport/ViciOne.ServiceBus.AmazonSqsTransport.csproj",
                "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/ViciOne.ServiceBus.RabbitMqTransport.csproj",
                "src/ViciOne.ServiceBus.Abstractions/ViciOne.ServiceBus.Abstractions.csproj",
                "src/ViciOne.ServiceBus.Analyzers.CodeFixes/ViciOne.ServiceBus.Analyzers.CodeFixes.csproj",
                "src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj",
                "src/ViciOne.ServiceBus.MessagePack/ViciOne.ServiceBus.MessagePack.csproj",
                "src/ViciOne.ServiceBus.SignalR/ViciOne.ServiceBus.SignalR.csproj",
                "src/ViciOne.ServiceBus.StateMachineVisualizer/ViciOne.ServiceBus.StateMachineVisualizer.csproj",
                "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj",
                "tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj",
                "tests2/Benchmarks/ViciOne.ServiceBus.Benchmark.Tests/ViciOne.ServiceBus.Benchmark.Tests.csproj",
                "tests2/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/ViciOne.ServiceBus.AmazonS3.Tests.csproj",
                "tests2/Persistence/ViciOne.ServiceBus.Azure.Table.Tests/ViciOne.ServiceBus.Azure.Table.Tests.csproj",
                "tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/ViciOne.ServiceBus.DynamoDbIntegration.Tests.csproj",
                "tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.csproj",
                "tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests/ViciOne.ServiceBus.QuartzIntegration.Tests.csproj",
                "tests2/Testing/ViciOne.ServiceBus.Analyzers.Tests.Infrastructure/ViciOne.ServiceBus.Analyzers.Tests.Infrastructure.csproj",
                "tests2/Testing/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure.Tests/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure.Tests.csproj",
                "tests2/Testing/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure/ViciOne.ServiceBus.Roslyn.Tests.Infrastructure.csproj",
                "tests2/Testing/ViciOne.ServiceBus.Tests.Infrastructure.Tests/ViciOne.ServiceBus.Tests.Infrastructure.Tests.csproj",
                "tests2/Testing/ViciOne.ServiceBus.Tests.Infrastructure/ViciOne.ServiceBus.Tests.Infrastructure.csproj",
                "tests2/Testing/ViciOne.ServiceBus.Tests.InternalAccess/ViciOne.ServiceBus.Tests.InternalAccess.csproj",
                "tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests/ViciOne.ServiceBus.ActiveMqTransport.Tests.csproj",
                "tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests/ViciOne.ServiceBus.AmazonSqsTransport.Tests.csproj",
                "tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj",
                "tests2/ViciOne.ServiceBus.Abstractions.Tests/ViciOne.ServiceBus.Abstractions.Tests.csproj",
                "tests2/ViciOne.ServiceBus.Analyzers.CodeFixes.Tests/ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.csproj",
                "tests2/ViciOne.ServiceBus.Analyzers.Tests/ViciOne.ServiceBus.Analyzers.Tests.csproj",
                "tests2/ViciOne.ServiceBus.MessagePack.Tests/ViciOne.ServiceBus.MessagePack.Tests.csproj",
                "tests2/ViciOne.ServiceBus.SignalR.Tests/ViciOne.ServiceBus.SignalR.Tests.csproj",
                "tests2/ViciOne.ServiceBus.StateMachineVisualizer.Tests/ViciOne.ServiceBus.StateMachineVisualizer.Tests.csproj",
                "tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj",
            ],
            actual);
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
    public void EngineeringSolution_ContainsEverySampleProject()
    {
        Assert.NotEmpty(RepositoryLayout.SampleProjects);

        var engineering = Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Engineering.slnx");
        var members = SolutionProjects(engineering)
            .Select(Path.GetFullPath)
            .ToHashSet(RepositoryLayout.PathComparer);

        var missing = RepositoryLayout.SampleProjects
            .Where(project => !members.Contains(Path.GetFullPath(project)))
            .Select(RepositoryLayout.RelativeToRoot)
            .ToArray();

        Assert.Empty(missing);
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

    private static IReadOnlyList<string> ProjectReferences(string project)
    {
        var fullPaths = MsBuildEvaluation.ItemMetadata(project, "ProjectReference", "FullPath");

        if (fullPaths.Count > 0)
        {
            return fullPaths.Select(Path.GetFullPath).ToArray();
        }

        var directory = Path.GetDirectoryName(project)
            ?? throw new InvalidOperationException($"No directory for {project}.");

        return MsBuildEvaluation.ItemIdentities(project, "ProjectReference")
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
