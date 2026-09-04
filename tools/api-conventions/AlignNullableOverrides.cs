#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=AlignNullableOverrides.packages.lock.json

using System.Text.RegularExpressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run AlignNullableOverrides.cs -- <project.csproj>");
    return 2;
}

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create();
workspace.RegisterWorkspaceFailedHandler(eventArgs => Console.Error.WriteLine(eventArgs.Diagnostic.Message));

var project = await workspace.OpenProjectAsync(Path.GetFullPath(args[0]));
var compilation = await project.GetCompilationAsync()
    ?? throw new InvalidOperationException("Unable to create the project compilation.");
var targets = new Dictionary<string, HashSet<TextSpan>>(StringComparer.Ordinal);
var unresolved = 0;

foreach (var diagnostic in compilation.GetDiagnostics()
             .Where(diagnostic => diagnostic.Location.IsInSource && diagnostic.Id is ("CS8765" or "CS8767" or "CS8769")))
{
    var nameMatch = Regex.Match(diagnostic.GetMessage(), "parameter(?:s)? [\\\"'„](?<name>[^\\\"'“]+)", RegexOptions.IgnoreCase);
    if (!nameMatch.Success)
    {
        unresolved++;
        continue;
    }

    var tree = diagnostic.Location.SourceTree!;
    var root = await tree.GetRootAsync();
    var locationNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
    var declaration = locationNode.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>()
        ?? locationNode.DescendantNodesAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
    var parameter = declaration?.ParameterList.Parameters.FirstOrDefault(candidate =>
        candidate.Identifier.ValueText == nameMatch.Groups["name"].Value);
    if (parameter?.Type == null || parameter.Type is NullableTypeSyntax)
    {
        unresolved++;
        continue;
    }

    if (!targets.TryGetValue(tree.FilePath, out var spans))
        targets.Add(tree.FilePath, spans = []);
    spans.Add(parameter.Span);
}

var changedFiles = 0;
var changedParameters = 0;
foreach (var pair in targets)
{
    var original = await File.ReadAllTextAsync(pair.Key);
    var tree = CSharpSyntaxTree.ParseText(original, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), pair.Key);
    var root = await tree.GetRootAsync();
    var parameters = pair.Value
        .Select(span => root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<ParameterSyntax>())
        .Where(parameter => parameter?.Type != null && parameter.Type is not NullableTypeSyntax)
        .Cast<ParameterSyntax>()
        .DistinctBy(parameter => parameter.Span)
        .ToArray();
    if (parameters.Length == 0)
        continue;

    var updated = root.ReplaceNodes(parameters, (originalParameter, _) =>
    {
        var type = originalParameter.Type!;
        return originalParameter.WithType(SyntaxFactory.NullableType(type.WithoutTrailingTrivia())
            .WithTrailingTrivia(type.GetTrailingTrivia()));
    });
    await File.WriteAllTextAsync(pair.Key, updated.ToFullString());
    changedFiles++;
    changedParameters += parameters.Length;
}

Console.WriteLine($"Aligned {changedParameters} implementation parameters across {changedFiles} files; unresolved diagnostics: {unresolved}.");
return 0;
