#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=GuardActivatorResults.packages.lock.json

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run GuardActivatorResults.cs -- <project.csproj>");
    return 2;
}

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create();
workspace.RegisterWorkspaceFailedHandler(eventArgs => Console.Error.WriteLine(eventArgs.Diagnostic.Message));
var project = await workspace.OpenProjectAsync(Path.GetFullPath(args[0]));
var compilation = await project.GetCompilationAsync()
    ?? throw new InvalidOperationException("Unable to create the project compilation.");

var targets = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
foreach (var diagnostic in compilation.GetDiagnostics()
             .Where(candidate => (candidate.Id is "CS8600" or "CS8602" or "CS8603" or "CS8604") && candidate.Location.IsInSource))
{
    var tree = diagnostic.Location.SourceTree!;
    var root = await tree.GetRootAsync();
    var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
    var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>()
        ?? node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
    if (invocation?.Expression.ToString() is not ("Activator.CreateInstance" or "System.Activator.CreateInstance"))
        continue;
    if (invocation.Parent is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression })
        continue;

    if (!targets.TryGetValue(tree.FilePath, out var fileTargets))
        targets.Add(tree.FilePath, fileTargets = []);
    fileTargets.Add(invocation.SpanStart);
}

var changedFiles = 0;
var changedInvocations = 0;
foreach (var pair in targets)
{
    var original = await File.ReadAllTextAsync(pair.Key);
    var tree = CSharpSyntaxTree.ParseText(original, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), pair.Key);
    var root = await tree.GetRootAsync();
    var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
        .Where(invocation => pair.Value.Contains(invocation.SpanStart))
        .ToArray();
    if (invocations.Length == 0)
        continue;

    var updated = root.ReplaceNodes(invocations, (invocation, _) =>
        SyntaxFactory.ParseExpression(
                $"({invocation.WithoutTrivia()} ?? throw new System.InvalidOperationException(\"The requested runtime type could not be activated.\"))")
            .WithTriviaFrom(invocation));
    await File.WriteAllTextAsync(pair.Key, updated.ToFullString());
    changedFiles++;
    changedInvocations += invocations.Length;
}

Console.WriteLine($"Guarded {changedInvocations} Activator results across {changedFiles} files.");
return 0;
