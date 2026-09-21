using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class AsyncApiConventionArchitectureTests
{
    private static readonly MetadataReference[] PlatformReferences = CreatePlatformReferences();
    private static readonly MetadataReference[] AsyncContractReferences =
        [.. PlatformReferences, CreateExternalAsyncContractReference()];
    private const string ExternalAsyncContracts = """
        namespace Azure
        {
            public abstract class AsyncPageable<T> { }
        }

        namespace Quartz
        {
            public interface IJob
            {
                global::System.Threading.Tasks.ValueTask Execute(
                    IJobExecutionContext context,
                    global::System.Threading.CancellationToken cancellationToken);
            }

            public interface IJobListener
            {
                global::System.Threading.Tasks.ValueTask JobToBeExecuted(
                    IJobExecutionContext context,
                    global::System.Threading.CancellationToken cancellationToken);

                global::System.Threading.Tasks.ValueTask JobExecutionVetoed(
                    IJobExecutionContext context,
                    global::System.Threading.CancellationToken cancellationToken);

                global::System.Threading.Tasks.ValueTask JobWasExecuted(
                    IJobExecutionContext context,
                    JobExecutionException? exception,
                    global::System.Threading.CancellationToken cancellationToken);
            }

            public interface ISchedulerListener
            {
                global::System.Threading.Tasks.ValueTask TriggerFinalized(
                    IScheduler scheduler,
                    ITrigger trigger,
                    global::System.Threading.CancellationToken cancellationToken);
            }

            public interface IJobExecutionContext { }

            public interface IScheduler { }

            public interface ITrigger { }

            public sealed class JobExecutionException { }
        }
        """;
    private const string QuartzGlobalUsings = """
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Quartz;
        """;

    [Fact]
    [RequirementCoverage("REQ-VSB-API-ASYNC-NAMING", "all-product-and-test-methods-bidirectional")]
    public void EveryMethodName_MatchesItsAsynchronousContractBidirectionally()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string[] violations = RepositoryLayout.ProductProjects.Concat(RepositoryLayout.NativeTestProjects)
            .SelectMany(project => InspectAsyncNames(project, cancellationToken))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Bidirectional asynchronous method naming violations:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [Theory]
    [InlineData("SendAsync", true, true)]
    [InlineData("SendCoreAsyncCore", true, false)]
    [InlineData("Configure", false, true)]
    [InlineData("ConfigureAsync", false, false)]
    [InlineData("EnableAsyncSend", false, true)]
    public void AsyncNameRule_RequiresAsyncAsTheOperationSuffix(
        string name,
        bool hasAsyncContract,
        bool expectedValid)
    {
        string? violation = AsyncNameViolation("synthetic.cs", name, hasAsyncContract, 1);

        Assert.Equal(expectedValid, violation is null);
    }

    [Theory]
    [InlineData("using Hidden = global::System.Threading.Tasks.Task; sealed class C { public Hidden Run() => global::System.Threading.Tasks.Task.CompletedTask; }", true)]
    [InlineData("using Task = global::System.Int32; sealed class C { public Task RunAsync() => 0; }", false)]
    [InlineData("using Hidden = global::System.Threading.Tasks.ValueTask; sealed class C { public Hidden Run() => default; }", true)]
    [InlineData("using Hidden = global::System.Collections.Generic.IAsyncEnumerable<int>; sealed class C { public Hidden Run() => default!; }", true)]
    [InlineData("using Hidden = global::Azure.AsyncPageable<int>; sealed class C { public Hidden Run() => default!; }", true)]
    [InlineData("namespace System.Threading.Tasks { internal sealed class Task { } internal sealed class C { internal Task RunAsync() => new(); } }", false)]
    [InlineData("namespace Azure { internal sealed class AsyncPageable<T> { } internal sealed class C { internal AsyncPageable<int> RunAsync() => new(); } }", false)]
    public void AsyncContractDetection_UsesResolvedTypeIdentity(string source, bool expectedAsynchronous)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
        CSharpCompilation compilation = CreateAsyncContractCompilation([syntaxTree]);
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
        MethodDeclarationSyntax method = syntaxTree.GetRoot(cancellationToken)
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        Assert.Equal(
            expectedAsynchronous,
            IsAsynchronousReturnType(method.ReturnType, semanticModel, cancellationToken));
    }

    [Fact]
    public void AsyncContractInspection_EvaluatesReleasePreprocessorSymbols()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string projectPath = Path.Combine(
            RepositoryLayout.Root,
            "tests",
            "Architecture",
            "ViciOne.ServiceBus.Architecture.Tests",
            "ViciOne.ServiceBus.Architecture.Tests.csproj");
        CSharpParseOptions parseOptions = ReleaseParseOptions(projectPath);
        const string source = """
            sealed class C
            {
            #if NET10_0
                public global::System.Threading.Tasks.Task Run() =>
                    global::System.Threading.Tasks.Task.CompletedTask;
            #endif
            }
            """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            parseOptions,
            path: "ReleaseConditional.cs",
            cancellationToken: cancellationToken);
        CSharpCompilation compilation = CreateAsyncContractCompilation([syntaxTree], parseOptions);
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
        MethodDeclarationSyntax method = syntaxTree.GetRoot(cancellationToken)
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        Assert.Contains("NET10_0", parseOptions.PreprocessorSymbolNames);
        Assert.NotNull(AsyncNameViolation(
            syntaxTree.FilePath,
            method.Identifier.ValueText,
            IsAsynchronousReturnType(method.ReturnType, semanticModel, cancellationToken),
            method.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
    }

    [Theory]
    [InlineData("sealed class Job : global::Quartz.IJob { public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", true)]
    [InlineData("sealed class Job : global::Quartz.IJob { public Task Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default!; }", false)]
    [InlineData("sealed class Job : global::Quartz.IJob { public ValueTask<int> Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", false)]
    [InlineData("sealed class Job : global::Quartz.IJob { public ValueTask Execute(string unrelatedValue) => default; }", false)]
    [InlineData("sealed class Job : global::Quartz.IJob { public ValueTask Execute<T>(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", false)]
    [InlineData("sealed class Job : global::Quartz.IJob { public static ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", false)]
    [InlineData("sealed class Job : global::Quartz.IJob { public ValueTask Execute(ref IJobExecutionContext context, CancellationToken cancellationToken) => default; }", false)]
    [InlineData("sealed class Job : global::Quartz.IJob { public System.Threading.Tasks.ValueTask Execute(Quartz.IJobExecutionContext context, System.Threading.CancellationToken cancellationToken) => default; }", true)]
    [InlineData("sealed class Job : global::Quartz.IJob { public Bogus.ValueTask Execute(Bogus.IJobExecutionContext context, Bogus.CancellationToken cancellationToken) => default; }", false)]
    [InlineData("sealed class Listener : global::Quartz.IJobListener { ValueTask global::Quartz.IJobListener.JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", true)]
    [InlineData("sealed class Listener : global::Quartz.IJobListener { ValueTask global::Quartz.IJobListener.JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", true)]
    [InlineData("sealed class Listener : global::Quartz.IJobListener { ValueTask global::Quartz.IJobListener.JobWasExecuted(IJobExecutionContext context, JobExecutionException? exception, CancellationToken cancellationToken) => default; }", true)]
    [InlineData("sealed class Listener : global::Quartz.IJobListener { ValueTask global::Quartz.IJobListener.JobWasExecuted(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", false)]
    [InlineData("sealed class Listener : global::Quartz.ISchedulerListener { public ValueTask TriggerFinalized(IScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken) => default; }", true)]
    [InlineData("sealed class Listener : global::Quartz.ISchedulerListener { public ValueTask TriggerFinalized(IScheduler scheduler, CancellationToken cancellationToken) => default; }", false)]
    [InlineData("using ValueTask = Bogus.ValueTask; using IJobExecutionContext = Bogus.IJobExecutionContext; using CancellationToken = Bogus.CancellationToken; sealed class Job : global::Quartz.IJob { System.Threading.Tasks.ValueTask global::Quartz.IJob.Execute(Quartz.IJobExecutionContext context, System.Threading.CancellationToken cancellationToken) => default; public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default; }", false)]
    [InlineData("using ValueTask = Bogus.ValueTask; using IJobExecutionContext = Bogus.IJobExecutionContext; using CancellationToken = Bogus.CancellationToken; namespace Bogus { public sealed class ValueTask { } public sealed class IJobExecutionContext { } public sealed class CancellationToken { } } public interface ShadowJob : global::Quartz.IJob { public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken); }", false)]
    public void QuartzAsyncContractAllowlist_RequiresTheExactExternalSignature(string source, bool expectedAllowed)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
        SyntaxTree globalUsings = CSharpSyntaxTree.ParseText(QuartzGlobalUsings, cancellationToken: cancellationToken);
        CSharpCompilation compilation = CreateAsyncContractCompilation([globalUsings, syntaxTree]);
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
        MethodDeclarationSyntax method = syntaxTree.GetCompilationUnitRoot(cancellationToken)
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Last();

        Assert.Equal(
            expectedAllowed,
            IsAllowedExternallyNamedAsyncContract(method, semanticModel, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-API-CANCELLATION-SHAPE", "public-token-is-final-parameter")]
    public void EveryPublicApiMethod_PlacesCancellationTokenLast()
    {
        string[] violations = SourceFiles(RepositoryLayout.ProductProjects)
            .SelectMany(InspectCancellationTokenPosition)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Public API CancellationToken parameters that are not last:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> InspectAsyncNames(string projectPath, CancellationToken cancellationToken)
    {
        CSharpParseOptions parseOptions = ReleaseParseOptions(projectPath);
        SyntaxTree[] sourceTrees = MsBuildEvaluation.ItemMetadata(projectPath, "Compile", "FullPath", "Release")
            .Select(Path.GetFullPath)
            .Where(IsRepositorySource)
            .Distinct(RepositoryLayout.PathComparer)
            .Select(path => CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                parseOptions,
                path: path,
                cancellationToken: cancellationToken))
            .ToArray();
        SyntaxTree globalUsings = CSharpSyntaxTree.ParseText(
            EvaluatedGlobalUsings(projectPath),
            parseOptions,
            path: projectPath + ".GlobalUsings.g.cs",
            cancellationToken: cancellationToken);
        CSharpCompilation compilation = CreateAsyncContractCompilation(
            [globalUsings, .. sourceTrees],
            parseOptions);

        foreach (SyntaxTree syntaxTree in sourceTrees)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
            SyntaxNode root = syntaxTree.GetRoot(cancellationToken);

            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (IsAllowedExternallyNamedAsyncContract(method, semanticModel, cancellationToken))
                    continue;

                bool hasAsyncContract = method.Modifiers.Any(SyntaxKind.AsyncKeyword)
                    || IsAsynchronousReturnType(method.ReturnType, semanticModel, cancellationToken);
                string? violation = AsyncNameViolation(
                    syntaxTree.FilePath,
                    method.Identifier.ValueText,
                    hasAsyncContract,
                    method.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
                if (violation is not null)
                    yield return violation;
            }

            foreach (LocalFunctionStatementSyntax method in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            {
                bool hasAsyncContract = method.Modifiers.Any(SyntaxKind.AsyncKeyword)
                    || IsAsynchronousReturnType(method.ReturnType, semanticModel, cancellationToken);
                string? violation = AsyncNameViolation(
                    syntaxTree.FilePath,
                    method.Identifier.ValueText,
                    hasAsyncContract,
                    method.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
                if (violation is not null)
                    yield return violation;
            }
        }
    }

    private static string? AsyncNameViolation(
        string path,
        string name,
        bool hasAsyncContract,
        int line)
    {
        bool hasAsyncName = name.EndsWith("Async", StringComparison.Ordinal);
        if (hasAsyncName == hasAsyncContract)
            return null;

        string expectation = hasAsyncContract
            ? "has an asynchronous contract but its name has no 'Async' marker"
            : "has an 'Async' marker but no asynchronous contract";
        return $"{RepositoryLayout.RelativeToRoot(path)}:{line}: {name} {expectation}.";
    }

    private static IEnumerable<string> InspectCancellationTokenPosition(string path)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
            .GetCompilationUnitRoot();

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!IsExternallyVisible(method))
                continue;

            ParameterSyntax[] parameters = method.ParameterList.Parameters.ToArray();
            int cancellationTokenIndex = Array.FindIndex(parameters, IsCancellationToken);
            if (cancellationTokenIndex < 0
                || IsCancellationToken(parameters[^1])
                || cancellationTokenIndex == 0 && parameters[0].Modifiers.Any(SyntaxKind.ThisKeyword))
                continue;

            int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return $"{RepositoryLayout.RelativeToRoot(path)}:{line}: {method.Identifier.ValueText} places "
                + $"'{parameters[cancellationTokenIndex].Identifier.ValueText}' before "
                + $"'{parameters[cancellationTokenIndex + 1].Identifier.ValueText}'.";
        }
    }

    private static bool IsExternallyVisible(MethodDeclarationSyntax method)
    {
        bool memberVisible = method.Modifiers.Any(SyntaxKind.PublicKeyword)
            || method.Modifiers.Any(SyntaxKind.ProtectedKeyword)
            || method.Parent is InterfaceDeclarationSyntax;
        if (!memberVisible)
            return false;

        return method.Ancestors().OfType<BaseTypeDeclarationSyntax>().All(type =>
            type.Modifiers.Any(SyntaxKind.PublicKeyword)
            || type.Modifiers.Any(SyntaxKind.ProtectedKeyword));
    }

    private static bool IsAllowedExternallyNamedAsyncContract(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(method, cancellationToken) is not IMethodSymbol candidate
            || candidate.ContainingType.TypeKind != TypeKind.Class)
            return false;

        foreach (INamedTypeSymbol contractType in candidate.ContainingType.AllInterfaces)
        {
            string metadataName = contractType.ToDisplayString() switch
            {
                "Quartz.IJob" => "Quartz.IJob",
                "Quartz.IJobListener" => "Quartz.IJobListener",
                "Quartz.ISchedulerListener" => "Quartz.ISchedulerListener",
                _ => string.Empty,
            };
            if (metadataName.Length == 0
                || !IsReferencedContractType(contractType, semanticModel.Compilation, metadataName))
                continue;

            foreach (IMethodSymbol contractMethod in contractType.GetMembers().OfType<IMethodSymbol>())
            {
                ISymbol? implementation = candidate.ContainingType.FindImplementationForInterfaceMember(contractMethod);
                if (SymbolEqualityComparer.Default.Equals(candidate, implementation))
                    return true;
            }
        }

        return false;
    }

    private static bool IsAsynchronousReturnType(
        TypeSyntax returnType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetTypeInfo(returnType, cancellationToken).Type is not INamedTypeSymbol type)
            return false;

        INamedTypeSymbol definition = type.OriginalDefinition;
        string metadataName = (definition.ContainingNamespace.ToDisplayString(), definition.Name, definition.Arity) switch
        {
            ("System.Threading.Tasks", "Task", 0) => "System.Threading.Tasks.Task",
            ("System.Threading.Tasks", "Task", 1) => "System.Threading.Tasks.Task`1",
            ("System.Threading.Tasks", "ValueTask", 0) => "System.Threading.Tasks.ValueTask",
            ("System.Threading.Tasks", "ValueTask", 1) => "System.Threading.Tasks.ValueTask`1",
            ("System.Collections.Generic", "IAsyncEnumerable", 1) => "System.Collections.Generic.IAsyncEnumerable`1",
            ("System.Collections.Generic", "IAsyncEnumerator", 1) => "System.Collections.Generic.IAsyncEnumerator`1",
            ("Azure", "AsyncPageable", 1) => "Azure.AsyncPageable`1",
            _ => string.Empty,
        };
        return metadataName.Length > 0
            && IsReferencedContractType(definition, semanticModel.Compilation, metadataName);
    }

    private static CSharpCompilation CreateAsyncContractCompilation(
        IEnumerable<SyntaxTree> syntaxTrees,
        CSharpParseOptions? parseOptions = null)
    {
        return CSharpCompilation.Create(
            "ViciOne.ServiceBus.AsyncContractAnalysis",
            syntaxTrees,
            AsyncContractReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static bool IsReferencedContractType(
        INamedTypeSymbol candidate,
        Compilation compilation,
        string metadataName)
    {
        foreach (MetadataReference reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
                && assembly.GetTypeByMetadataName(metadataName) is INamedTypeSymbol contract
                && SymbolEqualityComparer.Default.Equals(candidate, contract.OriginalDefinition))
                return true;
        }

        return false;
    }

    private static MetadataReference CreateExternalAsyncContractReference()
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ViciOne.ServiceBus.ExternalAsyncContracts",
            [CSharpSyntaxTree.ParseText(ExternalAsyncContracts)],
            PlatformReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                "The synthetic external asynchronous contracts failed to compile: "
                + string.Join(Environment.NewLine, result.Diagnostics));
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static CSharpParseOptions ReleaseParseOptions(string projectPath)
    {
        string declaredLanguageVersion = MsBuildEvaluation.PropertyOf(projectPath, "LangVersion", "Release");
        if (!LanguageVersionFacts.TryParse(declaredLanguageVersion, out LanguageVersion languageVersion))
        {
            throw new InvalidOperationException(
                $"Project '{RepositoryLayout.RelativeToRoot(projectPath)}' declares unsupported LangVersion '{declaredLanguageVersion}'.");
        }

        string[] preprocessorSymbols = MsBuildEvaluation
            .ImplicitDefineConstantsOf(projectPath, "Release")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return CSharpParseOptions.Default
            .WithLanguageVersion(languageVersion)
            .WithPreprocessorSymbols(preprocessorSymbols);
    }

    private static MetadataReference[] CreatePlatformReferences()
    {
        string trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
            ?? throw new InvalidOperationException("The runtime did not expose its trusted platform assemblies.");
        return trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(RepositoryLayout.PathComparer)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private static string EvaluatedGlobalUsings(string projectPath)
    {
        JsonElement items = MsBuildEvaluation.Evaluate(projectPath, "Release").GetProperty("Items");
        return items.TryGetProperty("Using", out JsonElement usingItems)
            ? string.Join(Environment.NewLine, usingItems.EnumerateArray().Select(FormatGlobalUsing))
            : string.Empty;
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

    private static bool IsCancellationToken(ParameterSyntax parameter) =>
        parameter.Type switch
        {
            SimpleNameSyntax name => name.Identifier.ValueText == nameof(CancellationToken),
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText == nameof(CancellationToken),
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText == nameof(CancellationToken),
            _ => false,
        };

    private static IEnumerable<string> SourceFiles(IEnumerable<string> projects) => projects
        .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath", "Release"))
        .Select(Path.GetFullPath)
        .Where(IsRepositorySource)
        .Distinct(RepositoryLayout.PathComparer);

    private static bool IsRepositorySource(string path) =>
        path.StartsWith(RepositoryLayout.Root + Path.DirectorySeparatorChar, RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison);
}
