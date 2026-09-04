#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=PropagateNullableArguments.packages.lock.json

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run PropagateNullableArguments.cs -- <project.csproj>");
    return 2;
}

var projectPath = Path.GetFullPath(args[0]);
var repositoryRoot = FindRepositoryRoot(projectPath);
var sourceRoot = Path.Combine(repositoryRoot, "src") + Path.DirectorySeparatorChar;

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create();
workspace.RegisterWorkspaceFailedHandler(eventArgs => Console.Error.WriteLine(eventArgs.Diagnostic.Message));

var project = await workspace.OpenProjectAsync(projectPath);
var compilation = await project.GetCompilationAsync()
    ?? throw new InvalidOperationException($"Unable to compile '{projectPath}'.");

var targets = new Dictionary<string, HashSet<TextSpan>>(StringComparer.Ordinal);
var unresolved = new List<string>();

foreach (var diagnostic in compilation.GetDiagnostics().Where(diagnostic => diagnostic.Id is "CS8604" or "CS8625" && diagnostic.Location.IsInSource))
{
    var syntaxTree = diagnostic.Location.SourceTree;
    if (syntaxTree == null)
        continue;

    var syntaxRoot = await syntaxTree.GetRootAsync();
    var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
    var argument = node.FirstAncestorOrSelf<ArgumentSyntax>();
    if (argument == null)
        continue;

    var semanticModel = compilation.GetSemanticModel(syntaxTree);
    var parameter = (semanticModel.GetOperation(argument) as IArgumentOperation)?.Parameter;
    if (parameter == null || parameter.NullableAnnotation == NullableAnnotation.Annotated)
        continue;

    var declaration = parameter.DeclaringSyntaxReferences
        .Select(reference => reference.GetSyntax())
        .OfType<ParameterSyntax>()
        .FirstOrDefault(candidate => candidate.Type != null && candidate.Type is not NullableTypeSyntax);
    if (declaration == null)
    {
        unresolved.Add($"{diagnostic.Location.GetLineSpan()}: {parameter.ContainingType}.{parameter.Name}");
        continue;
    }

    var declarationPath = declaration.SyntaxTree.FilePath;
    if (string.IsNullOrEmpty(declarationPath)
        || !Path.GetFullPath(declarationPath).StartsWith(sourceRoot, StringComparison.Ordinal))
    {
        unresolved.Add($"{diagnostic.Location.GetLineSpan()}: external {parameter.ContainingType}.{parameter.Name}");
        continue;
    }

    if (!targets.TryGetValue(declarationPath, out var spans))
        targets.Add(declarationPath, spans = []);
    spans.Add(declaration.Span);
}

var updatedFiles = 0;
var updatedParameters = 0;
foreach (var pair in targets)
{
    var document = workspace.CurrentSolution.Projects
        .SelectMany(candidate => candidate.Documents)
        .FirstOrDefault(candidate => string.Equals(candidate.FilePath, pair.Key, StringComparison.Ordinal));
    if (document == null)
    {
        unresolved.Add($"Document not loaded: {pair.Key}");
        continue;
    }

    var root = await document.GetSyntaxRootAsync()
        ?? throw new InvalidOperationException($"Unable to parse '{pair.Key}'.");
    var parameters = pair.Value
        .Select(span => root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<ParameterSyntax>())
        .Where(parameter => parameter?.Type != null && parameter.Type is not NullableTypeSyntax)
        .Cast<ParameterSyntax>()
        .DistinctBy(parameter => parameter.Span)
        .ToArray();

    if (parameters.Length == 0)
        continue;

    var updatedRoot = root.ReplaceNodes(parameters, (original, _) =>
    {
        var originalType = original.Type!;
        var nullableType = SyntaxFactory.NullableType(originalType.WithoutTrailingTrivia())
            .WithTrailingTrivia(originalType.GetTrailingTrivia());
        return original.WithType(nullableType);
    });
    await File.WriteAllTextAsync(pair.Key, updatedRoot.ToFullString());
    updatedFiles++;
    updatedParameters += parameters.Length;
}

Console.WriteLine($"Annotated {updatedParameters} bound parameters across {updatedFiles} files; unresolved calls: {unresolved.Count}.");
foreach (var item in unresolved.Take(50))
    Console.WriteLine(item);
if (unresolved.Count > 50)
    Console.WriteLine($"... {unresolved.Count - 50} additional unresolved calls");

return 0;

static string FindRepositoryRoot(string path)
{
    var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ViciOne.ServiceBus.Engineering.slnx")))
        directory = directory.Parent;

    return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
}
