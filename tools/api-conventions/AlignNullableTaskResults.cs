#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=AlignNullableTaskResults.packages.lock.json

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run AlignNullableTaskResults.cs -- <project.csproj>");
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
             .Where(candidate => (candidate.Id is "CS8613" or "CS8616") && candidate.Location.IsInSource))
{
    var tree = diagnostic.Location.SourceTree!;
    var root = await tree.GetRootAsync();
    var method = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
        .FirstAncestorOrSelf<MethodDeclarationSyntax>();
    if (method?.ReturnType is not GenericNameSyntax
        {
            Identifier.ValueText: "Task",
            TypeArgumentList.Arguments: [{ } resultType]
        }
        || resultType is NullableTypeSyntax)
    {
        unresolved.Add($"{diagnostic.Id}: {diagnostic.Location.GetLineSpan()}");
        continue;
    }

    if (!targets.TryGetValue(tree.FilePath, out var fileTargets))
        targets.Add(tree.FilePath, fileTargets = []);
    fileTargets.Add(resultType.Span);
}

var changedFiles = 0;
var changedResults = 0;
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
    changedResults += types.Length;
}

Console.WriteLine($"Aligned {changedResults} nullable Task results across {changedFiles} files; unresolved diagnostics: {unresolved.Count}.");
foreach (var item in unresolved.Take(30))
    Console.WriteLine(item);
return unresolved.Count == 0 ? 0 : 1;
