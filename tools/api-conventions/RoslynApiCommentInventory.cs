#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=RoslynApiCommentInventory.packages.lock.json

using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

if (args.Length != 3 || !Directory.Exists(args[0]) || !File.Exists(args[2]) || Directory.Exists(args[1]))
{
    Console.Error.WriteLine("Usage: dotnet run tools/api-conventions/RoslynApiCommentInventory.cs -- <repositories-root> <new-output-directory> <expected-projects.txt>");
    return 2;
}

string repositoriesRoot = Path.GetFullPath(args[0]);
string outputRoot = Path.GetFullPath(args[1]);
string[] expectedProjects = File.ReadAllLines(args[2])
    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
    .Order(StringComparer.Ordinal).ToArray();
IEnumerable<string> discoveryRepositories = Directory.Exists(Path.Combine(repositoriesRoot, "src"))
    ? [repositoriesRoot]
    : Directory.EnumerateDirectories(repositoriesRoot).Order(StringComparer.Ordinal);
string[] actualProjects = discoveryRepositories
    .SelectMany(repository => Directory.Exists(Path.Combine(repository, "src"))
        ? Directory.EnumerateFiles(Path.Combine(repository, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar)
                .Any(component => component is "obj" or "bin" or "artifacts"))
        : Enumerable.Empty<string>())
    .Select(path => Path.GetRelativePath(repositoriesRoot, path).Replace('\\', '/'))
    .Order(StringComparer.Ordinal).ToArray();
if (expectedProjects.Length == 0 || expectedProjects.Distinct(StringComparer.Ordinal).Count() != expectedProjects.Length ||
    !expectedProjects.SequenceEqual(actualProjects, StringComparer.Ordinal))
{
    Console.Error.WriteLine("The discovered projects do not match the expected project manifest.");
    Console.Error.WriteLine($"Expected {expectedProjects.Length}; found {actualProjects.Length}.");
    return 3;
}

Directory.CreateDirectory(outputRoot);
MSBuildLocator.RegisterDefaults();
var summaries = new List<ProjectSummary>();

IEnumerable<string> repositories = Directory.Exists(Path.Combine(repositoriesRoot, "src"))
    ? [repositoriesRoot]
    : Directory.EnumerateDirectories(repositoriesRoot).Order(StringComparer.Ordinal);
foreach (string repository in repositories)
{
    string source = Path.Combine(repository, "src");
    if (!Directory.Exists(source))
        continue;

    string repoName = Path.GetFileName(repository);
    string[] projectPaths = Directory.EnumerateFiles(source, "*.csproj", SearchOption.AllDirectories)
        .Where(path => !path.Split(Path.DirectorySeparatorChar)
            .Any(component => component is "obj" or "bin" or "artifacts"))
        .Order(StringComparer.Ordinal)
        .ToArray();
    Directory.CreateDirectory(Path.Combine(outputRoot, repoName));
    using var workspace = MSBuildWorkspace.Create();
    var workspaceFailures = new List<string>();
    workspace.RegisterWorkspaceFailedHandler(e => workspaceFailures.Add(e.Diagnostic.Message));

    foreach (string projectPath in projectPaths)
    {
        string projectName = Path.GetFileNameWithoutExtension(projectPath);
        int priorFailures = workspaceFailures.Count;
        var rows = new List<ApiRow>();
        var compilerErrors = Array.Empty<string>();
        int compilerErrorCount = 0;
        string? loadFailure = null;
        int documentCount = 0;
        string? assemblyName = null;

        try
        {
            Project project = workspace.CurrentSolution.Projects.FirstOrDefault(candidate =>
                    string.Equals(candidate.FilePath, projectPath, StringComparison.OrdinalIgnoreCase))
                ?? await workspace.OpenProjectAsync(projectPath);
            documentCount = project.Documents.Count();
            Compilation compilation = await project.GetCompilationAsync()
                ?? throw new InvalidOperationException("Roslyn returned no compilation.");
            assemblyName = compilation.AssemblyName;
            VisitNamespace(compilation.Assembly.GlobalNamespace, rows);
            string[] allCompilerErrors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToArray();
            compilerErrorCount = allCompilerErrors.Length;
            compilerErrors = allCompilerErrors.Take(100).ToArray();
        }
        catch (Exception exception)
        {
            loadFailure = exception.ToString();
        }

        string[] projectWorkspaceFailures = workspaceFailures.Skip(priorFailures).ToArray();
        var summary = new ProjectSummary(repoName, projectName, projectPath, assemblyName,
            documentCount, rows.Count, rows.Count(row => !row.Implicit && string.IsNullOrWhiteSpace(row.Xml)),
            projectWorkspaceFailures.Length, compilerErrorCount, loadFailure is not null);
        summaries.Add(summary);
        string relativeProjectPath = Path.GetRelativePath(repository, projectPath);
        string outputPath = Path.Combine(outputRoot, repoName, relativeProjectPath + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(new
        {
            summary,
            loadFailure,
            workspaceFailures = projectWorkspaceFailures,
            compilerErrors,
            symbols = rows.OrderBy(row => row.Id, StringComparer.Ordinal).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"{repoName}/{projectName}: {documentCount} files, {rows.Count} symbols, " +
            $"{summary.MissingXml} without XML, {summary.WorkspaceFailures} workspace, " +
            $"{summary.CompilerErrors} compiler, loadFailed={summary.LoadFailed}");
        Console.Out.Flush();
    }
}

await File.WriteAllTextAsync(Path.Combine(outputRoot, "summary.json"), JsonSerializer.Serialize(new
{
    generatedAtUtc = DateTimeOffset.UtcNow,
    repositoriesRoot,
    projectCount = summaries.Count,
    sourceDocuments = summaries.Sum(summary => summary.Documents),
    exposedSymbols = summaries.Sum(summary => summary.Symbols),
    missingXml = summaries.Sum(summary => summary.MissingXml),
    failedProjects = summaries.Count(summary => summary.LoadFailed || summary.WorkspaceFailures > 0 || summary.CompilerErrors > 0),
    projects = summaries
}, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"TOTAL: {summaries.Count} projects, {summaries.Sum(summary => summary.Symbols)} symbols, " +
    $"{summaries.Sum(summary => summary.MissingXml)} without XML, " +
    $"{summaries.Count(summary => summary.LoadFailed || summary.WorkspaceFailures > 0 || summary.CompilerErrors > 0)} failed projects");
return summaries.Any(summary => summary.LoadFailed || summary.WorkspaceFailures > 0 || summary.CompilerErrors > 0) ? 5 : 0;

static void VisitNamespace(INamespaceSymbol ns, ICollection<ApiRow> rows)
{
    foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
        VisitNamespace(child, rows);
    foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        VisitType(type, rows);
}

static void VisitType(INamedTypeSymbol type, ICollection<ApiRow> rows)
{
    if (!IsExposed(type))
        return;

    Add(type, rows);
    foreach (ISymbol member in type.GetMembers())
    {
        if (member is INamedTypeSymbol nested)
            VisitType(nested, rows);
        else if (IsExposed(member) && member is not IMethodSymbol
                 { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet
                     or MethodKind.EventAdd or MethodKind.EventRemove })
            Add(member, rows);
    }
}

static void Add(ISymbol symbol, ICollection<ApiRow> rows)
{
    Location? location = symbol.Locations.FirstOrDefault(location => location.IsInSource);
    if (location is null)
        return;

    var span = location.GetLineSpan();
    string id = symbol.GetDocumentationCommentId()
        ?? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    rows.Add(new ApiRow(id, symbol.Kind.ToString(), symbol.IsImplicitlyDeclared,
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
        span.Path, span.StartLinePosition.Line + 1,
        symbol.GetDocumentationCommentXml(expandIncludes: true) ?? string.Empty));
}

static bool IsExposed(ISymbol symbol)
{
    for (ISymbol? current = symbol; current is not null && current is not IAssemblySymbol;
         current = current.ContainingSymbol)
    {
        if (current is INamespaceSymbol or IModuleSymbol)
            continue;
        if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected
            or Accessibility.ProtectedOrInternal))
            return false;
        if (current.DeclaredAccessibility is Accessibility.Protected or Accessibility.ProtectedOrInternal &&
            current.ContainingType?.IsSealed == true)
            return false;
    }
    return true;
}

internal sealed record ApiRow(string Id, string Kind, bool Implicit, string Signature, string Path, int Line, string Xml);
internal sealed record ProjectSummary(string Repository, string Project, string Path, string? Assembly,
    int Documents, int Symbols, int MissingXml, int WorkspaceFailures, int CompilerErrors, bool LoadFailed);
