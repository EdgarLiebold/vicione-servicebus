using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class ConfigurationExceptionArchitectureTests
{
    static readonly ConcurrentDictionary<string, SyntaxTree[]> ProjectImports = new(StringComparer.Ordinal);
    // Match the SDK imports selected by this repository's ImplicitUsings setting.
    static readonly SyntaxTree ImplicitImports = CSharpSyntaxTree.ParseText("""
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """);
    static readonly MetadataReference MessageFactoryReference = MetadataReference.CreateFromFile(typeof(ConfigurationMessages).Assembly.Location);
    static readonly Lazy<MetadataReference[]> References = new(() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Runtime metadata references are unavailable."))
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Where(static path => path != typeof(ConfigurationMessages).Assembly.Location)
        .Distinct(StringComparer.Ordinal)
        .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append(MessageFactoryReference)
        .ToArray());

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-DIAGNOSTICS", "every-exception-has-feature-bus-problem-and-fix")]
    public void EveryConfigurationException_UsesTheActionableMessageFactoryOrAnExplicitConformingMessage()
    {
        VerifyLocalMessageAnalysis();
        VerifyPartialContextMessageAnalysis();

        string[] paths = Directory
            .EnumerateFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToArray();
        IReadOnlySet<string> configurationExceptions = FindConfigurationExceptionTypes(paths);
        SyntaxTree[] exceptionTrees = paths
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
            .Where(tree => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Any(declaration => configurationExceptions.Contains(declaration.Identifier.ValueText)
                    && typeof(ConfigurationException).Assembly.GetType(
                        string.Join(".", declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                            .Reverse().Select(scope => scope.Name.ToString()).Append(declaration.Identifier.ValueText))) == null))
            .ToArray();
        string[] violations = paths
            .SelectMany(path => FindViolations(CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path),
                configurationExceptions, exceptionTrees))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "ConfigurationException message-shape violations:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));

        string normalized = ConfigurationMessages.Create(
            "Reliable messaging",
            "orders-v1",
            "InitialRetryDelay is invalid\nbecause it is negative",
            "Set a positive duration");
        Assert.Equal(
            "Reliable messaging for bus 'orders-v1': InitialRetryDelay is invalid because it is negative. Set a positive duration.",
            normalized);
        Assert.DoesNotContain(Environment.NewLine, normalized, StringComparison.Ordinal);
    }

    static IReadOnlySet<string> FindConfigurationExceptionTypes(IEnumerable<string> paths)
    {
        ClassDeclarationSyntax[] declarations = paths
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
                .GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>())
            .ToArray();
        var result = new HashSet<string>(StringComparer.Ordinal) { "ConfigurationException" };
        bool changed;
        do
        {
            changed = false;
            foreach (ClassDeclarationSyntax declaration in declarations)
            {
                bool derivesFromConfigurationException = declaration.BaseList?.Types.Any(baseType =>
                    baseType.Type.DescendantNodesAndSelf()
                        .OfType<SimpleNameSyntax>()
                        .Any(name => result.Contains(name.Identifier.ValueText))) == true;
                if (derivesFromConfigurationException)
                    changed |= result.Add(declaration.Identifier.ValueText);
            }
        }
        while (changed);

        return result;
    }

    static IEnumerable<string> FindViolations(SyntaxTree tree, IReadOnlySet<string> configurationExceptions,
        IReadOnlyList<SyntaxTree>? exceptionTrees = null)
    {
        SyntaxNode root = tree.GetRoot();
        SemanticModel? model = null;
        foreach (ObjectCreationExpressionSyntax creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            string? typeName = creation.Type.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .LastOrDefault()
                ?.Identifier.ValueText;
            if (typeName is null || !configurationExceptions.Contains(typeName))
                continue;

            model ??= CreateCompilation(tree, exceptionTrees).GetSemanticModel(tree);
            ExpressionSyntax? message = FindMessageArgument(creation, model)?.Expression;
            bool factory = message is InvocationExpressionSyntax invocation && IsMessageFactory(invocation, model);
            bool explicitShape = message is LiteralExpressionSyntax or InterpolatedStringExpressionSyntax
                && message.ToString().Contains(" for bus '", StringComparison.Ordinal)
                && message.ToString().Contains("':", StringComparison.Ordinal);
            bool localFactory = message is IdentifierNameSyntax identifier
                && IsUnmodifiedFactoryLocal(identifier, creation, model);
            if (factory || explicitShape || localFactory)
                continue;

            FileLinePositionSpan span = creation.GetLocation().GetLineSpan();
            yield return $"{RepositoryLayout.RelativeToRoot(tree.FilePath)}:{span.StartLinePosition.Line + 1}";
        }
    }

    static CSharpCompilation CreateCompilation(SyntaxTree tree, IReadOnlyList<SyntaxTree>? exceptionTrees = null)
    {
        SyntaxTree[] trees = new[] { tree }
            .Concat(exceptionTrees?.Where(candidate => candidate.FilePath != tree.FilePath) ?? []).ToArray();
        IEnumerable<SyntaxTree> imports = trees.SelectMany(FindProjectImports).Distinct();
        return CSharpCompilation.Create("ConfigurationDiagnosticScan", trees.Append(ImplicitImports).Concat(imports),
            References.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    static IEnumerable<SyntaxTree> FindProjectImports(SyntaxTree tree)
    {
        DirectoryInfo? directory = new FileInfo(tree.FilePath).Directory;
        while (directory != null && directory.FullName.StartsWith(RepositoryLayout.Root + Path.DirectorySeparatorChar,
                   StringComparison.Ordinal))
        {
            if (directory.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return ProjectImports.GetOrAdd(directory.FullName, static project => Directory
                    .EnumerateFiles(project, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !Path.GetRelativePath(project, path).Split(Path.DirectorySeparatorChar)
                        .Any(part => part is "obj" or "bin"))
                    .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
                    .Select(source => (Tree: source, Usings: ((CompilationUnitSyntax)source.GetRoot()).Usings
                        .Where(directive => directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)).ToArray()))
                    .Where(source => source.Usings.Length != 0)
                    .Select(source => (SyntaxTree)CSharpSyntaxTree.Create(SyntaxFactory.CompilationUnit()
                        .WithUsings(SyntaxFactory.List(source.Usings)), path: source.Tree.FilePath))
                    .ToArray());
            }
            directory = directory.Parent;
        }
        return [];
    }

    static bool IsMessageFactory(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (invocation.Expression is GenericNameSyntax or MemberAccessExpressionSyntax { Name: GenericNameSyntax }
            || model.Compilation.GetAssemblyOrModuleSymbol(MessageFactoryReference) is not IAssemblySymbol assembly)
            return false;
        INamedTypeSymbol? factory = assembly.GetTypeByMetadataName(typeof(ConfigurationMessages).FullName!);
        IMethodSymbol[] methods = model.GetMemberGroup(invocation.Expression).OfType<IMethodSymbol>()
            .Where(method => method.Arity == 0 && method.Name is "Create" or "Aggregate"
                && SymbolEqualityComparer.Default.Equals(method.ContainingType, factory)).ToArray();
        return methods.Length == 1 && TryMapArguments(invocation.ArgumentList.Arguments, methods[0].Parameters, out _);
    }

    static ArgumentSyntax? FindMessageArgument(ObjectCreationExpressionSyntax creation, SemanticModel model,
        bool useSignatureRecovery = false)
    {
        if (!useSignatureRecovery && (model.GetOperation(creation) as IObjectCreationOperation)?.Arguments
            .SingleOrDefault(argument => argument.Parameter?.Name == "message")?.Syntax is ArgumentSyntax bound)
            return bound;
        if (creation.ArgumentList == null || model.GetTypeInfo(creation.Type).Type is not INamedTypeSymbol type)
            return null;

        // Individual source files lack some provider dependency types. Recover only a
        // message position agreed by every viable actual constructor signature.
        var candidates = new List<ArgumentSyntax>();
        foreach (IMethodSymbol constructor in type.InstanceConstructors.Where(method => method.DeclaredAccessibility == Accessibility.Public))
        {
            if (!TryMapArguments(creation.ArgumentList.Arguments, constructor.Parameters, out ArgumentSyntax[] mapped))
                continue;
            bool compatible = true;
            ArgumentSyntax? message = null;
            for (int index = 0; index < mapped.Length; index++)
            {
                ExpressionSyntax expression = mapped[index].Expression;
                ITypeSymbol? argumentType = expression is InvocationExpressionSyntax invocation && IsMessageFactory(invocation, model)
                    ? model.Compilation.GetSpecialType(SpecialType.System_String) : model.GetTypeInfo(expression).Type;
                if (argumentType != null && argumentType.TypeKind != TypeKind.Error
                    && !((CSharpCompilation)model.Compilation).ClassifyConversion(argumentType, constructor.Parameters[index].Type).IsImplicit)
                    compatible = false;
                if (constructor.Parameters[index].Name == "message") message = mapped[index];
            }
            if (compatible && message != null) candidates.Add(message);
        }
        return candidates.Count != 0 && candidates.All(argument => argument == candidates[0]) ? candidates[0] : null;
    }

    static bool TryMapArguments(SeparatedSyntaxList<ArgumentSyntax> arguments, IReadOnlyList<IParameterSymbol> parameters,
        out ArgumentSyntax[] mapped)
    {
        mapped = new ArgumentSyntax[parameters.Count];
        if (arguments.Count != parameters.Count) return false;
        for (int position = 0; position < arguments.Count; position++)
        {
            ArgumentSyntax argument = arguments[position];
            int index = argument.NameColon == null ? position : Array.FindIndex(parameters.ToArray(),
                parameter => parameter.Name == argument.NameColon.Name.Identifier.ValueText);
            if (index < 0 || mapped[index] != null || !argument.RefKindKeyword.IsKind(SyntaxKind.None)) return false;
            mapped[index] = argument;
        }
        return mapped.All(argument => argument != null);
    }

    static bool IsUnmodifiedFactoryLocal(IdentifierNameSyntax identifier,
        ObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        if (model.GetSymbolInfo(identifier).Symbol is not ILocalSymbol local
            || local.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is not VariableDeclaratorSyntax declaration
            || declaration.Parent?.Parent is not LocalDeclarationStatementSyntax statement
            || statement.Parent is not BlockSyntax block
            || creation.Ancestors().OfType<BlockSyntax>().FirstOrDefault() != block
            || statement.Span.End >= creation.SpanStart
            || declaration.Initializer?.Value is not InvocationExpressionSyntax invocation
            || !IsMessageFactory(invocation, model))
            return false;

        // A same-block declaration dominates the throw. Bind uses to the actual local,
        // so a field or a shadowing declaration cannot borrow another message's initializer.
        foreach (IdentifierNameSyntax use in block.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(use).Symbol, local))
                continue;

            if (use.Ancestors().TakeWhile(node => node != block).Any(node =>
                    node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or RefExpressionSyntax
                    || node is AssignmentExpressionSyntax assignment && assignment.Left.Span.Contains(use.Span)
                    || node is PrefixUnaryExpressionSyntax prefix && prefix.Kind() is
                        SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression
                    || node is PostfixUnaryExpressionSyntax postfix && postfix.Kind() is
                        SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
                    || node is ArgumentSyntax argument && !argument.RefKindKeyword.IsKind(SyntaxKind.None)))
                return false;
        }

        return true;
    }

    static void VerifyLocalMessageAnalysis()
    {
        const string factory = "ConfigurationMessages.Create(\"Batch timing\", \"orders\", \"Invalid duration\", \"Set a supported duration\")";
        string[] validBodies =
        [
            $"string message = {factory}; var failure = Failure(message); throw new ConfigurationException([failure], message);",
            $"string message = {factory}; if (bad) {{ _ = \"unrelated\"; }} throw new ConfigurationException(message);",
            $"string message = global::ViciOne.ServiceBus.Providers.Configuration.{factory}; throw new ConfigurationException(message);",
            $"throw new ConfigurationException(innerException: new Exception(), message: {factory});",
        ];
        string[] invalidBodies =
        [
            "string message = \"bad\"; throw new ConfigurationException(message);",
            $"string message = {factory}; message = \"bad\"; throw new ConfigurationException(message);",
            $"string message = {factory}; if (bad) message = \"bad\"; throw new ConfigurationException(message);",
            $"string message = {factory}; MutateRef(ref message); throw new ConfigurationException(message);",
            $"string message = {factory}; MutateOut(out message); throw new ConfigurationException(message);",
            $"string message = {factory}; Action mutate = () => message = \"bad\"; throw new ConfigurationException(message);",
            $"string message = {factory}; void Mutate() {{ message = \"bad\"; }} throw new ConfigurationException(message);",
            $"if (bad) {{ string message = {factory}; _ = message; }} throw new ConfigurationException(message);",
            $"string message = Transform({factory}); throw new ConfigurationException(message);",
            $"string message = {factory}; ref string alias = ref message; alias = \"bad\"; throw new ConfigurationException(message);",
            "string message = Fake.ConfigurationMessages.Create(); throw new ConfigurationException(message);",
            $"throw new ConfigurationException(\"bad\", new Exception({factory}));",
            $"throw new ConfigurationException([Failure({factory})], \"bad\");",
            $"throw new ConfigurationException(Transform({factory}));",
        ];
        IReadOnlySet<string> exceptions = new HashSet<string>(StringComparer.Ordinal) { "ConfigurationException" };
        foreach (string body in validBodies)
            Assert.Empty(FindViolations(CompileFixture(body), exceptions));
        foreach (string body in invalidBodies)
            Assert.Single(FindViolations(CompileFixture(body), exceptions));
        Assert.Single(FindViolations(CompileFixture($"string message = {factory}; throw new ConfigurationException(message);",
            shadowFactory: true), exceptions));
    }

    static void VerifyPartialContextMessageAnalysis()
    {
        const string factory = "ConfigurationMessages.Create(\"RabbitMQ\", context.DestinationAddress, \"Invalid exchange\", \"Use a valid exchange\")";
        string[] accepted =
        [
            $"new ConfigurationException({factory})",
            $"new ConfigurationException({factory}, interrupted)",
            $"new ConfigurationException(innerException: interrupted, message: {factory})",
        ];
        string[] rejected =
        [
            $"new ConfigurationException(\"bad\", new Exception({factory}))",
            "new ConfigurationException(context.Failures, \"bad\")",
            "new ConfigurationException(context.First, context.Second)",
            "new ConfigurationException(context.Message, interrupted)",
            "new ConfigurationException(Fake.ConfigurationMessages.Create(), interrupted)",
        ];
        IReadOnlySet<string> exceptions = new HashSet<string>(StringComparer.Ordinal) { "ConfigurationException" };
        foreach (string expression in accepted.Concat(rejected))
        {
            // These deliberately incomplete dependency contexts exercise recovery;
            // they are separate from the compiler-valid local-message fixtures.
            SyntaxTree tree = CSharpSyntaxTree.ParseText($$"""
                using System;
                using ViciOne.ServiceBus;
                using ViciOne.ServiceBus.Providers.Configuration;
                class PartialFixture
                {
                    void Run(MissingContext context, MissingException interrupted) { throw {{expression}}; }
                }
                namespace Fake { static class ConfigurationMessages { public static string Create() => "bad"; } }
                """, path: Path.Combine(RepositoryLayout.Root, "partial-fixture.cs"));
            CSharpCompilation compilation = CreateCompilation(tree);
            Assert.Contains(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            ObjectCreationExpressionSyntax creation = tree.GetRoot().DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>().First();
            if (accepted.Contains(expression))
            {
                SemanticModel model = compilation.GetSemanticModel(tree);
                ArgumentSyntax? recovered = FindMessageArgument(creation, model, useSignatureRecovery: true);
                Assert.NotNull(recovered);
                Assert.True(recovered.Expression is InvocationExpressionSyntax invocation && IsMessageFactory(invocation, model));
                Assert.Empty(FindViolations(tree, exceptions));
            }
            else Assert.Single(FindViolations(tree, exceptions));
        }
    }

    static SyntaxTree CompileFixture(string body, bool shadowFactory = false)
    {
        string source = $$"""
            using System;
            using ViciOne.ServiceBus;
            using ViciOne.ServiceBus.Configuration;
            using ViciOne.ServiceBus.Providers.Configuration;
            class Fixture
            {
                string message = "bad";
                bool bad = true;
                static ValidationResult Failure(string value) => ValidationResultExtensions.Failure(null, "test", value);
                static string Transform(string value) => "bad";
                static void MutateRef(ref string value) => value = "bad";
                static void MutateOut(out string value) => value = "bad";
                void Run() { {{body}} }
            }
            namespace Fake { static class ConfigurationMessages { public static string Create() => "bad"; } }
            """;
        if (shadowFactory)
            source += "namespace ViciOne.ServiceBus.Providers.Configuration { static class ConfigurationMessages { public static string Create(string a, string b, string c, string d) => \"bad\"; } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: Path.Combine(RepositoryLayout.Root, "fixture.cs"));
        Diagnostic[] errors = CreateCompilation(tree).GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        return tree;
    }
}
