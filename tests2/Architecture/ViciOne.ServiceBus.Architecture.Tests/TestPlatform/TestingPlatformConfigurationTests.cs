using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
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
    private const int ExpectedUnitTestFloor = 2222;
    private const int ExpectedLocalIntegrationTestFloor = 244;

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
    [RequirementCoverage("REQ-TEST-203", "public-local-integration-commands-use-predeclared-floor")]
    public void PublicLocalIntegrationProfileCommands_UseThePredeclaredFloor()
    {
        string[] commandOwners =
        [
            Path.Combine(RepositoryLayout.Root, "docs", "build.md"),
            Path.Combine(RepositoryLayout.Root, ".github", "workflows", "native-tests.yml"),
        ];

        Assert.All(commandOwners, path =>
        {
            Assert.Equal(ExpectedLocalIntegrationTestFloor, ReadLocalIntegrationProfileFloor(path));

            MatchCollection outageControls = Regex.Matches(
                File.ReadAllText(path),
                @"--allow-broker-outage\s+activemq\b",
                RegexOptions.CultureInvariant);

            Assert.Single(outageControls);
        });
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

    [Fact]
    [RequirementCoverage("REQ-TEST-203", "active-mq-local-integration-uses-causal-barriers")]
    public void ActiveMqLocalIntegrationSources_UseNoWallClockWaits()
    {
        var sourceRoot = Path.Combine(
            RepositoryLayout.Root,
            "tests2",
            "Transports",
            "ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests");

        var violations = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(
                segment => segment is "bin" or "obj"))
            .SelectMany(FindWallClockWaits)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("class C { async void M() { await System.Threading.Tasks.Task.Delay(1); } }")]
    [InlineData("using ClockTask = System.Threading.Tasks.Task; class C { async void M() { await ClockTask.Delay(1); } }")]
    [InlineData("using static System.Threading.Tasks.Task; class C { async void M() { await Delay(1); } }")]
    [InlineData("using ClockThread = System.Threading.Thread; class C { void M() { ClockThread.Sleep(1); } }")]
    [InlineData("class C { async System.Threading.Tasks.Task M() { System.Func<int, System.Threading.Tasks.Task> wait = System.Threading.Tasks.Task.Delay; await wait(1); } }")]
    [RequirementCoverage("REQ-TEST-203", "wall-clock-guard-rejects-direct-alias-static-import-and-method-group-forms")]
    public void ActiveMqWallClockGuard_RejectsEveryForbiddenReferenceShape(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel semanticModel = CreateWallClockSemanticModel(syntaxTree);

        SimpleNameSyntax name = Assert.Single(
            syntaxTree
                .GetRoot(TestContext.Current.CancellationToken)
                .DescendantNodes()
                .OfType<SimpleNameSyntax>(),
            candidate => IsWallClockWait(candidate, semanticModel));

        Assert.True(IsWallClockWait(name, semanticModel));
    }

    private static IEnumerable<string> FindWallClockWaits(string path)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
        SemanticModel semanticModel = CreateWallClockSemanticModel(syntaxTree);

        return syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<SimpleNameSyntax>()
            .Where(name => IsWallClockWait(name, semanticModel))
            .Select(name =>
                $"{RepositoryLayout.RelativeToRoot(path)}:{name.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
    }

    private static SemanticModel CreateWallClockSemanticModel(SyntaxTree syntaxTree)
    {
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
        ];
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ViciOne.ServiceBus.ActiveMqWallClockAnalysis",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
    }

    private static bool IsWallClockWait(SimpleNameSyntax name, SemanticModel semanticModel)
    {
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(name);

        return IsWallClockWait(symbolInfo.Symbol)
            || symbolInfo.CandidateSymbols.Any(IsWallClockWait);
    }

    private static bool IsWallClockWait(ISymbol? symbol)
    {
        if (symbol is not IMethodSymbol method)
            return false;

        string containingNamespace = method.ContainingType.ContainingNamespace.ToDisplayString();
        return (method.Name, method.ContainingType.Name, containingNamespace) switch
        {
            ("Delay", "Task", "System.Threading.Tasks") => true,
            ("Sleep", "Thread", "System.Threading") => true,
            _ => false,
        };
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

    private static int ReadLocalIntegrationProfileFloor(string path)
    {
        const string localCommandPattern =
            @"dotnet test\s+--solution\s+ViciOne\.ServiceBus\.Tests\.LocalIntegration\.slnx.*?--minimum-expected-tests\s+(?<floor>\d+)";
        MatchCollection matches = Regex.Matches(
            File.ReadAllText(path),
            localCommandPattern,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.True(
            matches.Count == 1,
            $"expected exactly one documented LocalIntegration profile command in {path}, found {matches.Count}");
        return int.Parse(matches[0].Groups["floor"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
