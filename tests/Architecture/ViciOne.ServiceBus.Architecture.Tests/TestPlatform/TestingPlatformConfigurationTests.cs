using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
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
    private const int ExpectedUnitTestFloor = 2933;
    private const int ExpectedLocalIntegrationTestFloor = 326;
    private const int ExpectedSqlServerLocalIntegrationTestFloor = 60;
    private const int ExpectedAzureServiceBusLocalIntegrationTestFloor = 24;
    private const int ExpectedRabbitMqLocalIntegrationTestFloor = 17;

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
            Path.Combine(RepositoryLayout.Root, "tests"),
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
            Path.Combine(RepositoryLayout.Root, "tests"),
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
            File.ReadAllBytes(Path.Combine(RepositoryLayout.Root, "tests", "testsettings.json")),
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
            Assert.Equal(
                ExpectedSqlServerLocalIntegrationTestFloor,
                ReadSqlServerLocalIntegrationProfileFloor(path));
            Assert.Equal(
                ExpectedAzureServiceBusLocalIntegrationTestFloor,
                ReadAzureServiceBusLocalIntegrationProfileFloor(path));
            Assert.Equal(
                ExpectedRabbitMqLocalIntegrationTestFloor,
                ReadRabbitMqLocalIntegrationProfileFloor(path));

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
    [RequirementCoverage("REQ-TEST-203", "broker-local-integration-projects-use-causal-barriers")]
    public void BrokerLocalIntegrationSources_UseNoWallClockWaits()
    {
        string[] projectNames =
        [
            "ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests",
            "ViciOne.ServiceBus.EventHubIntegration.LocalIntegration.Tests",
            "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests",
        ];

        foreach (string projectName in projectNames)
        {
            string projectPath = Path.Combine(
                RepositoryLayout.Root,
                "tests",
                "Transports",
                projectName,
                $"{projectName}.csproj");
            CSharpParseOptions parseOptions = ReleaseParseOptions(projectPath);

            SyntaxTree[] projectSyntaxTrees = MsBuildEvaluation
                .ItemMetadata(projectPath, "Compile", "FullPath", "Release")
                .Select(path => CSharpSyntaxTree.ParseText(
                    File.ReadAllText(path),
                    parseOptions,
                    path))
                .ToArray();
            Assert.NotEmpty(projectSyntaxTrees);
            SyntaxTree[] syntaxTrees =
            [
                ImplicitGlobalUsingsSyntaxTree(projectPath, parseOptions),
                .. projectSyntaxTrees,
            ];
            CSharpCompilation compilation = CreateWallClockCompilation(syntaxTrees);

            var violations = syntaxTrees
                .SelectMany(syntaxTree => FindWallClockWaits(syntaxTree, compilation))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(violations);
        }
    }

    [Theory]
    [InlineData(null, "class C { async void M() { await System.Threading.Tasks.Task.Delay(1); } }")]
    [InlineData(null, "using ClockTask = System.Threading.Tasks.Task; class C { async void M() { await ClockTask.Delay(1); } }")]
    [InlineData(null, "using static System.Threading.Tasks.Task; class C { async void M() { await Delay(1); } }")]
    [InlineData(null, "using ClockThread = System.Threading.Thread; class C { void M() { ClockThread.Sleep(1); } }")]
    [InlineData(null, "class C { async System.Threading.Tasks.Task M() { System.Func<int, System.Threading.Tasks.Task> wait = System.Threading.Tasks.Task.Delay; await wait(1); } }")]
    [InlineData("global using ClockTask = System.Threading.Tasks.Task;", "class C { async void M() { await ClockTask.Delay(1); } }")]
    [InlineData(null, "#if NET10_0\nclass C { async void M() { await System.Threading.Tasks.Task.Delay(1); } }\n#endif")]
    [InlineData(null, "class C { async void M() { await Task.Delay(1); } }")]
    [RequirementCoverage("REQ-TEST-203", "wall-clock-guard-rejects-release-compiled-reference-forms")]
    public void ActiveMqWallClockGuard_RejectsEveryForbiddenReferenceShape(string? globalUsing, string source)
    {
        string projectPath = Path.Combine(
            RepositoryLayout.Root,
            "tests",
            "Transports",
            "ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests",
            "ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.csproj");
        CSharpParseOptions parseOptions = ReleaseParseOptions(projectPath);
        var syntaxTrees = new List<SyntaxTree>
        {
            ImplicitGlobalUsingsSyntaxTree(projectPath, parseOptions),
        };
        if (globalUsing is not null)
        {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                globalUsing,
                parseOptions,
                path: "GlobalUsings.cs",
                cancellationToken: TestContext.Current.CancellationToken));
        }

        syntaxTrees.Add(CSharpSyntaxTree.ParseText(
            source,
            parseOptions,
            path: "Subject.cs",
            cancellationToken: TestContext.Current.CancellationToken));
        SyntaxTree subjectTree = syntaxTrees[^1];
        CSharpCompilation compilation = CreateWallClockCompilation(syntaxTrees);
        SemanticModel semanticModel = compilation.GetSemanticModel(subjectTree, ignoreAccessibility: true);

        SimpleNameSyntax name = Assert.Single(
            subjectTree
                .GetRoot(TestContext.Current.CancellationToken)
                .DescendantNodes()
                .OfType<SimpleNameSyntax>(),
            candidate => IsWallClockWait(candidate, semanticModel));

        Assert.True(IsWallClockWait(name, semanticModel));
    }

    private static IEnumerable<string> FindWallClockWaits(
        SyntaxTree syntaxTree,
        CSharpCompilation compilation)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);

        return syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<SimpleNameSyntax>()
            .Where(name => IsWallClockWait(name, semanticModel))
            .Select(name =>
                $"{RepositoryLayout.RelativeToRoot(syntaxTree.FilePath)}:{name.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
    }

    private static CSharpCompilation CreateWallClockCompilation(IEnumerable<SyntaxTree> syntaxTrees)
    {
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
        ];
        return CSharpCompilation.Create(
            "ViciOne.ServiceBus.WallClockAnalysis",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static CSharpParseOptions ReleaseParseOptions(string projectPath)
    {
        string declaredLanguageVersion = MsBuildEvaluation.PropertyOf(projectPath, "LangVersion", "Release");
        if (!LanguageVersionFacts.TryParse(declaredLanguageVersion, out LanguageVersion languageVersion))
        {
            throw new InvalidOperationException(
                $"The LocalIntegration project declares unsupported LangVersion '{declaredLanguageVersion}'.");
        }

        string[] preprocessorSymbols = MsBuildEvaluation
            .ImplicitDefineConstantsOf(projectPath, "Release")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return CSharpParseOptions.Default
            .WithLanguageVersion(languageVersion)
            .WithPreprocessorSymbols(preprocessorSymbols);
    }

    private static SyntaxTree ImplicitGlobalUsingsSyntaxTree(
        string projectPath,
        CSharpParseOptions parseOptions)
    {
        JsonElement evaluatedItems = MsBuildEvaluation
            .Evaluate(projectPath, "Release")
            .GetProperty("Items");
        string source = evaluatedItems.TryGetProperty("Using", out JsonElement usingItems)
            ? string.Join(Environment.NewLine, usingItems.EnumerateArray().Select(FormatGlobalUsing))
            : string.Empty;
        string generatedPath = Path.Combine(
            Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException($"No directory for {projectPath}."),
            "obj",
            "WallClockImplicitGlobalUsings.g.cs");

        return CSharpSyntaxTree.ParseText(source, parseOptions, generatedPath);
    }

    private static string FormatGlobalUsing(JsonElement item)
    {
        string identity = item.GetProperty("Identity").GetString()
            ?? throw new InvalidOperationException("An evaluated Using item has no identity.");
        string alias = item.TryGetProperty("Alias", out JsonElement aliasValue)
            ? aliasValue.GetString() ?? string.Empty
            : string.Empty;
        bool isStatic = item.TryGetProperty("Static", out JsonElement staticValue)
            && bool.TryParse(staticValue.GetString(), out bool parsedStatic)
            && parsedStatic;

        if (alias.Length > 0 && isStatic)
            throw new InvalidOperationException($"Using '{identity}' cannot be both aliased and static.");
        if (alias.Length > 0)
            return $"global using {alias} = {identity};";
        return isStatic
            ? $"global using static {identity};"
            : $"global using {identity};";
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

    private static int ReadAzureServiceBusLocalIntegrationProfileFloor(string path)
    {
        const string localCommandPattern =
            @"dotnet test\s+--solution\s+ViciOne\.ServiceBus\.Tests\.AzureServiceBusLocalIntegration\.slnx.*?--minimum-expected-tests\s+(?<floor>\d+)";
        MatchCollection matches = Regex.Matches(
            File.ReadAllText(path),
            localCommandPattern,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.True(
            matches.Count == 1,
            $"expected exactly one documented AzureServiceBusLocalIntegration profile command in {path}, found {matches.Count}");
        return int.Parse(matches[0].Groups["floor"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int ReadSqlServerLocalIntegrationProfileFloor(string path)
    {
        const string localCommandPattern =
            @"dotnet test\s+--solution\s+ViciOne\.ServiceBus\.Tests\.SqlServerLocalIntegration\.slnx.*?--minimum-expected-tests\s+(?<floor>\d+)";
        MatchCollection matches = Regex.Matches(
            File.ReadAllText(path),
            localCommandPattern,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.True(
            matches.Count == 1,
            $"expected exactly one documented SqlServerLocalIntegration profile command in {path}, found {matches.Count}");
        return int.Parse(matches[0].Groups["floor"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int ReadRabbitMqLocalIntegrationProfileFloor(string path)
    {
        const string localCommandPattern =
            @"dotnet test\s+--solution\s+ViciOne\.ServiceBus\.Tests\.RabbitMqLocalIntegration\.slnx.*?--minimum-expected-tests\s+(?<floor>\d+)";
        MatchCollection matches = Regex.Matches(
            File.ReadAllText(path),
            localCommandPattern,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.True(
            matches.Count == 1,
            $"expected exactly one documented RabbitMqLocalIntegration profile command in {path}, found {matches.Count}");
        return int.Parse(matches[0].Groups["floor"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
