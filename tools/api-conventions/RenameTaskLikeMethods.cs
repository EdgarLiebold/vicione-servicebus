#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=RenameAsync.packages.lock.json

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;

if (args.Length < 3 || args.Length > 4 || args[0] is not ("analyze" or "apply"))
{
    Console.Error.WriteLine(
        "Usage: dotnet run tools/api-conventions/RenameTaskLikeMethods.cs -- <analyze|apply> <solution.slnx> <report.json> [maximum-candidates]");
    return 2;
}

string mode = args[0];
string solutionPath = Path.GetFullPath(args[1]);
string reportPath = Path.GetFullPath(args[2]);
int maximumCandidates = args.Length == 4
    ? int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture)
    : int.MaxValue;
string repositoryRoot = FindRepositoryRoot(solutionPath);

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
{
    ["Configuration"] = "Release",
    ["ViciOneNativeTestTree"] = "true",
});

var workspaceDiagnostics = new List<string>();
workspace.RegisterWorkspaceFailedHandler(eventArgs =>
    workspaceDiagnostics.Add($"{eventArgs.Diagnostic.Kind}: {eventArgs.Diagnostic.Message}"));

Solution solution = await workspace.OpenSolutionAsync(solutionPath);
List<Candidate> initialCandidates = await FindCandidatesAsync(solution, repositoryRoot);
var changes = new List<RenameChange>();
var requirementMappingChanges = new List<RequirementMappingChange>();

if (mode == "apply")
{
    int requestedCandidates = Math.Min(initialCandidates.Count, maximumCandidates);
    int processedCandidates = 0;
    foreach (Candidate candidate in initialCandidates.Take(maximumCandidates))
    {
        IMethodSymbol? symbol = await ResolveCandidateAsync(solution, candidate);
        if (symbol is null)
        {
            changes.Add(new RenameChange(candidate, "unresolved"));
        }
        else if (symbol.Name.EndsWith("Async", StringComparison.Ordinal))
        {
            changes.Add(new RenameChange(candidate, "already-renamed"));
        }
        else if (!IsTaskLike(symbol.ReturnType))
        {
            changes.Add(new RenameChange(candidate, "no-longer-task-like"));
        }
        else
        {
            try
            {
                solution = await Renamer.RenameSymbolAsync(
                    solution,
                    symbol,
                    new SymbolRenameOptions(
                        RenameOverloads: false,
                        RenameInStrings: false,
                        RenameInComments: false,
                        RenameFile: false),
                    candidate.NewName);
                changes.Add(new RenameChange(candidate, "renamed"));
            }
            catch (Exception exception)
            {
                changes.Add(new RenameChange(
                    candidate,
                    $"error: {exception.GetType().Name}: {exception.Message}"));
            }
        }

        processedCandidates++;
        if (processedCandidates % 25 == 0 || processedCandidates == requestedCandidates)
            Console.WriteLine($"Processed candidates: {processedCandidates}/{requestedCandidates}");
    }

    if (!workspace.TryApplyChanges(solution))
        throw new InvalidOperationException("MSBuildWorkspace rejected the updated solution.");

    requirementMappingChanges = UpdateRequirementMappings(repositoryRoot, changes);
}

Solution finalSolution = mode == "apply" ? workspace.CurrentSolution : solution;
List<Candidate> remainingCandidates = mode == "apply"
    ? await FindCandidatesAsync(finalSolution, repositoryRoot)
    : initialCandidates;
List<AsyncSuffixCandidate> asyncSuffixCandidates = await FindAsyncSuffixCandidatesAsync(
    finalSolution,
    repositoryRoot);

Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await File.WriteAllTextAsync(
    reportPath,
    JsonSerializer.Serialize(
        new
        {
            mode,
            solution = solutionPath,
            initialCandidateCount = initialCandidates.Count,
            requestedCandidateCount = Math.Min(initialCandidates.Count, maximumCandidates),
            remainingCandidateCount = remainingCandidates.Count,
            asyncSuffixCandidateCount = asyncSuffixCandidates.Count,
            candidates = initialCandidates,
            changes,
            requirementMappingChanges,
            remaining = remainingCandidates,
            asyncSuffixCandidates,
            workspaceDiagnostics = workspaceDiagnostics.Distinct(StringComparer.Ordinal).Order().ToArray(),
        },
        new JsonSerializerOptions { WriteIndented = true }),
    CancellationToken.None);

Console.WriteLine($"Mode: {mode}");
Console.WriteLine($"Candidates: {initialCandidates.Count}");
Console.WriteLine($"Requested candidates: {Math.Min(initialCandidates.Count, maximumCandidates)}");
Console.WriteLine($"Remaining: {remainingCandidates.Count}");
Console.WriteLine($"Async-suffix semantic-review candidates: {asyncSuffixCandidates.Count}");
Console.WriteLine($"Report: {reportPath}");
return mode == "analyze" || remainingCandidates.Count == 0 ? 0 : 1;

static async Task<List<Candidate>> FindCandidatesAsync(Solution solution, string repositoryRoot)
{
    var candidates = new List<Candidate>();

    foreach (Project project in solution.Projects)
    {
        foreach (Document document in project.Documents.Where(document => IsRepositorySource(document, repositoryRoot)))
        {
            SyntaxNode? root = await document.GetSyntaxRootAsync();
            SemanticModel? semanticModel = await document.GetSemanticModelAsync();
            if (root is null || semanticModel is null || document.FilePath is null)
                continue;

            foreach (MethodDeclarationSyntax declaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                IMethodSymbol? method = semanticModel.GetDeclaredSymbol(declaration) as IMethodSymbol;
                if (method is null || !IsRenameCandidate(method))
                    continue;

                FileLinePositionSpan lineSpan = declaration.Identifier.GetLocation().GetLineSpan();
                candidates.Add(new Candidate(
                    "member",
                    DocumentationCommentId.CreateDeclarationId(method),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    declaration.Identifier.ValueText,
                    GetAsyncName(declaration.Identifier.ValueText),
                    GetRuntimeTypeName(method.ContainingType),
                    project.Name,
                    Path.GetFullPath(document.FilePath),
                    lineSpan.StartLinePosition.Line + 1,
                    method.ContainingType.TypeKind == TypeKind.Interface,
                    IsTestScenario(method)));
            }

            foreach (LocalFunctionStatementSyntax declaration in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            {
                IMethodSymbol? method = semanticModel.GetDeclaredSymbol(declaration) as IMethodSymbol;
                if (method is null || !IsRenameCandidate(method))
                    continue;

                FileLinePositionSpan lineSpan = declaration.Identifier.GetLocation().GetLineSpan();
                candidates.Add(new Candidate(
                    "local",
                    null,
                    method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    method.Name,
                    GetAsyncName(method.Name),
                    GetRuntimeTypeName(method.ContainingType),
                    project.Name,
                    Path.GetFullPath(document.FilePath),
                    lineSpan.StartLinePosition.Line + 1,
                    false,
                    false));
            }
        }
    }

    return candidates
        .OrderByDescending(candidate => candidate.IsInterfaceMember)
        .ThenBy(candidate => candidate.Project, StringComparer.Ordinal)
        .ThenBy(candidate => candidate.File, StringComparer.Ordinal)
        .ThenBy(candidate => candidate.Line)
        .ThenBy(candidate => candidate.DisplayName, StringComparer.Ordinal)
        .ToList();
}

static async Task<List<AsyncSuffixCandidate>> FindAsyncSuffixCandidatesAsync(
    Solution solution,
    string repositoryRoot)
{
    var candidates = new List<AsyncSuffixCandidate>();

    foreach (Project project in solution.Projects)
    {
        foreach (Document document in project.Documents.Where(document => IsRepositorySource(document, repositoryRoot)))
        {
            SyntaxNode? root = await document.GetSyntaxRootAsync();
            SemanticModel? semanticModel = await document.GetSemanticModelAsync();
            if (root is null || semanticModel is null || document.FilePath is null)
                continue;

            IEnumerable<SyntaxNode> declarations = root.DescendantNodes().Where(node =>
                node is MethodDeclarationSyntax or LocalFunctionStatementSyntax);
            foreach (SyntaxNode declaration in declarations)
            {
                IMethodSymbol? method = declaration switch
                {
                    MethodDeclarationSyntax member => semanticModel.GetDeclaredSymbol(member) as IMethodSymbol,
                    LocalFunctionStatementSyntax local => semanticModel.GetDeclaredSymbol(local) as IMethodSymbol,
                    _ => null,
                };
                if (method is null ||
                    !method.Name.EndsWith("Async", StringComparison.Ordinal) ||
                    IsTaskLike(method.ReturnType) ||
                    IsAsyncStream(method.ReturnType))
                {
                    continue;
                }

                FileLinePositionSpan lineSpan = declaration.GetLocation().GetLineSpan();
                candidates.Add(new AsyncSuffixCandidate(
                    method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    method.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    project.Name,
                    Path.GetFullPath(document.FilePath),
                    lineSpan.StartLinePosition.Line + 1));
            }
        }
    }

    return candidates
        .OrderBy(candidate => candidate.Project, StringComparer.Ordinal)
        .ThenBy(candidate => candidate.File, StringComparer.Ordinal)
        .ThenBy(candidate => candidate.Line)
        .ThenBy(candidate => candidate.DisplayName, StringComparer.Ordinal)
        .ToList();
}

static async Task<IMethodSymbol?> ResolveCandidateAsync(Solution solution, Candidate candidate)
{
    Project? project = solution.Projects.FirstOrDefault(project =>
        StringComparer.Ordinal.Equals(project.Name, candidate.Project));
    if (project is null)
        return null;

    if (candidate.DeclarationId is not null)
    {
        Compilation? compilation = await project.GetCompilationAsync();
        if (compilation is not null &&
            DocumentationCommentId.GetFirstSymbolForDeclarationId(candidate.DeclarationId, compilation) is IMethodSymbol method)
        {
            return method;
        }
    }

    Document? document = project.Documents.FirstOrDefault(document =>
        document.FilePath is not null &&
        StringComparer.Ordinal.Equals(Path.GetFullPath(document.FilePath), candidate.File));
    SyntaxNode? root = document is null ? null : await document.GetSyntaxRootAsync();
    SemanticModel? semanticModel = document is null ? null : await document.GetSemanticModelAsync();
    if (root is null || semanticModel is null)
        return null;

    IEnumerable<SyntaxNode> declarations = candidate.Kind == "local"
        ? root.DescendantNodes().OfType<LocalFunctionStatementSyntax>()
        : root.DescendantNodes().OfType<MethodDeclarationSyntax>();
    foreach (SyntaxNode declaration in declarations)
    {
        SyntaxToken identifier = declaration switch
        {
            MethodDeclarationSyntax member => member.Identifier,
            LocalFunctionStatementSyntax local => local.Identifier,
            _ => default,
        };
        string name = identifier.ValueText;
        if (identifier == default ||
            identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1 != candidate.Line ||
            name != candidate.OldName && name != candidate.NewName)
        {
            continue;
        }

        return declaration switch
        {
            MethodDeclarationSyntax member => semanticModel.GetDeclaredSymbol(member) as IMethodSymbol,
            LocalFunctionStatementSyntax local => semanticModel.GetDeclaredSymbol(local) as IMethodSymbol,
            _ => null,
        };
    }

    return null;
}

static bool IsRenameCandidate(IMethodSymbol method)
{
    return IsTaskLike(method.ReturnType) &&
        !method.Name.EndsWith("Async", StringComparison.Ordinal) &&
        method.Name != "Main" &&
        method.MethodKind != MethodKind.DelegateInvoke &&
        !ImplementsExternalContract(method);
}

static bool IsTestScenario(IMethodSymbol method)
{
    return method.GetAttributes().Any(attribute =>
        attribute.AttributeClass?.ToDisplayString() is "Xunit.FactAttribute" or "Xunit.TheoryAttribute");
}

static bool IsTaskLike(ITypeSymbol returnType)
{
    if (returnType is not INamedTypeSymbol namedType)
        return false;

    string metadataName = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    return metadataName is
        "global::System.Threading.Tasks.Task" or
        "global::System.Threading.Tasks.Task<TResult>" or
        "global::System.Threading.Tasks.ValueTask" or
        "global::System.Threading.Tasks.ValueTask<TResult>";
}

static bool IsAsyncStream(ITypeSymbol returnType)
{
    if (returnType is not INamedTypeSymbol namedType)
        return false;

    string metadataName = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    return metadataName is
        "global::System.Collections.Generic.IAsyncEnumerable<T>" or
        "global::Azure.AsyncPageable<T>";
}

static string GetAsyncName(string name)
{
    const string callbackSuffix = "AsyncCallback";
    return name.EndsWith(callbackSuffix, StringComparison.Ordinal)
        ? name[..^callbackSuffix.Length] + "CallbackAsync"
        : name + "Async";
}

static string GetRuntimeTypeName(INamedTypeSymbol type)
{
    var names = new Stack<string>();
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        names.Push(current.MetadataName);

    string typeName = string.Join("+", names);
    string namespaceName = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    return string.IsNullOrEmpty(namespaceName) ? typeName : namespaceName + "." + typeName;
}

static List<RequirementMappingChange> UpdateRequirementMappings(
    string repositoryRoot,
    IEnumerable<RenameChange> changes)
{
    RenameChange[] testRenames = changes
        .Where(change => change.Candidate.IsTestScenario && change.Status is "renamed" or "already-renamed")
        .GroupBy(change => (change.Candidate.ContainingType, change.Candidate.OldName))
        .Select(group => group.First())
        .ToArray();
    var mappingChanges = new List<RequirementMappingChange>();
    if (testRenames.Length == 0)
        return mappingChanges;

    foreach (string path in Directory.EnumerateFiles(repositoryRoot, "*Requirements.json", SearchOption.AllDirectories))
    {
        string relativePath = Path.GetRelativePath(repositoryRoot, path);
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(segment => segment is "bin" or "obj" or "review" or ".git"))
            continue;

        string content = File.ReadAllText(path);
        string updated = content;
        MatchCollection objectMatches = Regex.Matches(updated, @"\{[^{}]*\}", RegexOptions.Singleline);
        foreach (Match objectMatch in objectMatches.Cast<Match>().Reverse())
        {
            string objectText = objectMatch.Value;
            foreach (RenameChange rename in testRenames)
            {
                Candidate candidate = rename.Candidate;
                if (!Regex.IsMatch(
                        objectText,
                        $@"""testType""\s*:\s*""{Regex.Escape(candidate.ContainingType)}"""))
                {
                    continue;
                }

                string methodPattern = $@"(""testMethod""\s*:\s*""){Regex.Escape(candidate.OldName)}("")";
                string replacedObject = Regex.Replace(
                    objectText,
                    methodPattern,
                    $"$1{candidate.NewName}$2");
                if (replacedObject == objectText)
                    continue;

                updated = updated.Remove(objectMatch.Index, objectMatch.Length)
                    .Insert(objectMatch.Index, replacedObject);
                mappingChanges.Add(new RequirementMappingChange(
                    relativePath,
                    candidate.ContainingType,
                    candidate.OldName,
                    candidate.NewName));
                break;
            }
        }

        if (updated != content)
            File.WriteAllText(path, updated);
    }

    return mappingChanges;
}

static bool ImplementsExternalContract(IMethodSymbol method)
{
    if (method.OverriddenMethod is { } overriddenMethod &&
        !SymbolEqualityComparer.Default.Equals(overriddenMethod.ContainingAssembly, method.ContainingAssembly))
    {
        return true;
    }

    if (method.ExplicitInterfaceImplementations.Any(interfaceMethod =>
            !SymbolEqualityComparer.Default.Equals(interfaceMethod.ContainingAssembly, method.ContainingAssembly)))
    {
        return true;
    }

    string methodName = method.Name[(method.Name.LastIndexOf('.') + 1)..];
    foreach (INamedTypeSymbol interfaceType in method.ContainingType.AllInterfaces)
    {
        if (SymbolEqualityComparer.Default.Equals(interfaceType.ContainingAssembly, method.ContainingAssembly))
            continue;

        foreach (ISymbol interfaceMember in interfaceType.GetMembers(methodName))
        {
            if (SymbolEqualityComparer.Default.Equals(
                    method.ContainingType.FindImplementationForInterfaceMember(interfaceMember),
                    method))
            {
                return true;
            }
        }
    }

    return false;
}

static bool IsRepositorySource(Document document, string repositoryRoot)
{
    if (document.FilePath is null)
        return false;

    string path = Path.GetFullPath(document.FilePath);
    if (!path.StartsWith(repositoryRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        return false;

    string relativePath = Path.GetRelativePath(repositoryRoot, path);
    string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return !segments.Any(segment => segment is "bin" or "obj" or "review" or ".git");
}

static string FindRepositoryRoot(string solutionPath)
{
    DirectoryInfo? directory = new FileInfo(solutionPath).Directory;
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        directory = directory.Parent;

    return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
}

sealed record Candidate(
    string Kind,
    string? DeclarationId,
    string DisplayName,
    string OldName,
    string NewName,
    string ContainingType,
    string Project,
    string File,
    int Line,
    bool IsInterfaceMember,
    bool IsTestScenario);

sealed record RenameChange(Candidate Candidate, string Status);

sealed record AsyncSuffixCandidate(string DisplayName, string ReturnType, string Project, string File, int Line);

sealed record RequirementMappingChange(string File, string TestType, string OldName, string NewName);
