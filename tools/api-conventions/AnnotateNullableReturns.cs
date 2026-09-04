#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=AnnotateNullableReturns.packages.lock.json

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run AnnotateNullableReturns.cs -- <project.csproj>");
    return 2;
}

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create();
workspace.RegisterWorkspaceFailedHandler(eventArgs => Console.Error.WriteLine(eventArgs.Diagnostic.Message));
var project = await workspace.OpenProjectAsync(Path.GetFullPath(args[0]));
var compilation = await project.GetCompilationAsync()
    ?? throw new InvalidOperationException("Unable to create the project compilation.");

var targets = new Dictionary<string, Dictionary<TextSpan, bool>>(StringComparer.Ordinal);
var unresolved = new List<string>();

foreach (var diagnostic in compilation.GetDiagnostics().Where(candidate => candidate.Id == "CS8603" && candidate.Location.IsInSource))
{
    var tree = diagnostic.Location.SourceTree!;
    var root = await tree.GetRootAsync();
    var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
    var returnExpression = node.FirstAncestorOrSelf<ReturnStatementSyntax>()?.Expression
        ?? node.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>()?.Expression;
    if (returnExpression?.ToString().Contains("Activator.CreateInstance", StringComparison.Ordinal) == true)
    {
        unresolved.Add($"Factory invariant: {diagnostic.Location.GetLineSpan()}");
        continue;
    }

    var isIterator = node.FirstAncestorOrSelf<YieldStatementSyntax>() != null;
    TypeSyntax? returnType = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>()?.Type
        ?? node.FirstAncestorOrSelf<MethodDeclarationSyntax>()?.ReturnType
        ?? node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>()?.ReturnType;
    if (returnType == null || returnType is NullableTypeSyntax)
    {
        unresolved.Add($"Unsupported return: {diagnostic.Location.GetLineSpan()}");
        continue;
    }

    if (!targets.TryGetValue(tree.FilePath, out var fileTargets))
        targets.Add(tree.FilePath, fileTargets = []);
    fileTargets[returnType.Span] = fileTargets.GetValueOrDefault(returnType.Span) || isIterator;
}

var changedFiles = 0;
var changedReturns = 0;
foreach (var pair in targets)
{
    var original = await File.ReadAllTextAsync(pair.Key);
    var tree = CSharpSyntaxTree.ParseText(original, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), pair.Key);
    var root = await tree.GetRootAsync();
    var types = pair.Value.Keys
        .Select(span => root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<TypeSyntax>())
        .Where(type => type != null && type is not NullableTypeSyntax)
        .Cast<TypeSyntax>()
        .DistinctBy(type => type.Span)
        .ToArray();
    if (types.Length == 0)
        continue;

    var updated = root.ReplaceNodes(types, (type, _) => MakeNullable(type, pair.Value.GetValueOrDefault(type.Span)));
    await File.WriteAllTextAsync(pair.Key, updated.ToFullString());
    changedFiles++;
    changedReturns += types.Length;
}

Console.WriteLine($"Annotated {changedReturns} nullable returns across {changedFiles} files; unresolved returns: {unresolved.Count}.");
foreach (var item in unresolved.Take(30))
    Console.WriteLine(item);
return 0;

static TypeSyntax MakeNullable(TypeSyntax type, bool iterator)
{
    if (iterator && TryGetSingleTypeArgument(type, out var genericName, out var elementType))
    {
        if (elementType is NullableTypeSyntax)
            return type;

        var nullableElement = SyntaxFactory.NullableType(elementType.WithoutTrailingTrivia())
            .WithTrailingTrivia(elementType.GetTrailingTrivia());
        return type.ReplaceNode(genericName, genericName.WithTypeArgumentList(
            genericName.TypeArgumentList.WithArguments(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(nullableElement))));
    }

    return SyntaxFactory.NullableType(type.WithoutTrailingTrivia()).WithTrailingTrivia(type.GetTrailingTrivia());
}

static bool TryGetSingleTypeArgument(TypeSyntax type, out GenericNameSyntax genericName, out TypeSyntax elementType)
{
    genericName = type switch
    {
        GenericNameSyntax direct => direct,
        QualifiedNameSyntax { Right: GenericNameSyntax qualified } => qualified,
        AliasQualifiedNameSyntax { Name: GenericNameSyntax aliased } => aliased,
        _ => null!,
    };
    if (genericName?.TypeArgumentList.Arguments is not [{ } argument])
    {
        elementType = null!;
        return false;
    }

    elementType = argument;
    return true;
}
