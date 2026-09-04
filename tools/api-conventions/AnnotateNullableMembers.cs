#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=AnnotateNullableMembers.packages.lock.json

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run AnnotateNullableMembers.cs -- <project.csproj>");
    return 2;
}

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create();
workspace.RegisterWorkspaceFailedHandler(eventArgs => Console.Error.WriteLine(eventArgs.Diagnostic.Message));
var project = await workspace.OpenProjectAsync(Path.GetFullPath(args[0]));
var compilation = await project.GetCompilationAsync()
    ?? throw new InvalidOperationException("Unable to create the project compilation.");

var targets = new Dictionary<string, HashSet<TextSpan>>(StringComparer.Ordinal);
var unresolved = new List<string>();
foreach (var diagnostic in compilation.GetDiagnostics()
             .Where(candidate => candidate.Id == "CS8601" && candidate.Location.IsInSource))
{
    var tree = diagnostic.Location.SourceTree!;
    var root = await tree.GetRootAsync();
    var model = compilation.GetSemanticModel(tree);
    var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

    ISymbol? symbol = null;
    var assignment = node.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
    if (assignment != null)
        symbol = model.GetSymbolInfo(assignment.Left).Symbol;

    if (symbol == null)
    {
        var declarator = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
        if (declarator != null)
            symbol = model.GetDeclaredSymbol(declarator);
    }

    if (symbol is not IFieldSymbol and not IPropertySymbol
        || symbol.DeclaredAccessibility != Accessibility.Private
        || symbol.GetSymbolType().IsReferenceType != true)
    {
        unresolved.Add($"{diagnostic.Location.GetLineSpan()}: {symbol?.Kind} {symbol?.DeclaredAccessibility} {symbol?.ToDisplayString()}");
        continue;
    }

    var syntax = await symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntaxAsync()!;
    TypeSyntax? type = syntax switch
    {
        VariableDeclaratorSyntax variable when variable.Parent is VariableDeclarationSyntax declaration && declaration.Variables.Count == 1 => declaration.Type,
        PropertyDeclarationSyntax property => property.Type,
        _ => null
    };

    if (type == null || type is NullableTypeSyntax)
    {
        unresolved.Add($"{diagnostic.Location.GetLineSpan()}: unsupported declaration for {symbol.ToDisplayString()}");
        continue;
    }

    var filePath = syntax.SyntaxTree.FilePath;
    if (!targets.TryGetValue(filePath, out var fileTargets))
        targets.Add(filePath, fileTargets = []);
    fileTargets.Add(type.Span);
}

var changedFiles = 0;
var changedTypes = 0;
foreach (var pair in targets)
{
    var original = await File.ReadAllTextAsync(pair.Key);
    var tree = CSharpSyntaxTree.ParseText(original, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), pair.Key);
    var root = await tree.GetRootAsync();
    var types = pair.Value
        .Select(span => root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<TypeSyntax>())
        .Where(type => type != null && type is not NullableTypeSyntax)
        .Cast<TypeSyntax>()
        .DistinctBy(type => type.Span)
        .ToArray();
    if (types.Length == 0)
        continue;

    var updated = root.ReplaceNodes(types, (type, _) => SyntaxFactory.NullableType(type.WithoutTrailingTrivia())
        .WithTrailingTrivia(type.GetTrailingTrivia()));
    await File.WriteAllTextAsync(pair.Key, updated.ToFullString());
    changedFiles++;
    changedTypes += types.Length;
}

Console.WriteLine($"Annotated {changedTypes} nullable private member types across {changedFiles} files; unresolved diagnostics: {unresolved.Count}.");
foreach (var item in unresolved.Take(100))
    Console.WriteLine(item);
return 0;

static class SymbolExtensions
{
    public static ITypeSymbol GetSymbolType(this ISymbol symbol)
    {
        return symbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol))
        };
    }
}
