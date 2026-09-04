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
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;

if (args.Length < 3 || args.Length > 4 || args[0] is not ("analyze" or "apply"))
{
    Console.Error.WriteLine(
        "Usage: dotnet run tools/api-conventions/RenameAsync.cs -- <analyze|apply> <solution.slnx> <report.json> [maximum-rename-roots]");
    return 2;
}

string mode = args[0];
string solutionPath = Path.GetFullPath(args[1]);
string reportPath = Path.GetFullPath(args[2]);
int maximumRenameRoots = args.Length == 4
    ? int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture)
    : int.MaxValue;
string repositoryRoot = FindRepositoryRoot(solutionPath);
string sourceRoot = Path.Combine(repositoryRoot, "src") + Path.DirectorySeparatorChar;

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
var apiCollisionChanges = new List<ApiCollisionChange>();
if (mode == "apply")
    (solution, apiCollisionChanges) = await ResolveKnownApiCollisionsAsync(solution);

List<Candidate> initialCandidates = await FindCandidatesAsync(solution, sourceRoot);
List<Candidate> renameRoots = SelectRenameRoots(initialCandidates);
var applied = new List<RenameResult>();

if (mode == "apply")
{
    int processedRoots = 0;
    foreach (Candidate candidate in renameRoots.Take(maximumRenameRoots))
    {
        IMethodSymbol? symbol = await ResolveCandidateAsync(solution, candidate);
        if (symbol is null || symbol.Name.EndsWith("Async", StringComparison.Ordinal) || !IsAsyncMethod(symbol))
            applied.Add(new RenameResult(candidate.DeclarationId, candidate.DisplayName, candidate.Name, candidate.Name + "Async", "already-renamed"));
        else
        {
            string newName = symbol.Name + "Async";
            string? collision = FindCollision(symbol, newName);
            if (collision is not null)
                applied.Add(new RenameResult(candidate.DeclarationId, candidate.DisplayName, candidate.Name, newName, collision));
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
                        newName);
                    applied.Add(new RenameResult(candidate.DeclarationId, candidate.DisplayName, candidate.Name, newName, "renamed"));
                }
                catch (Exception exception)
                {
                    applied.Add(new RenameResult(candidate.DeclarationId, candidate.DisplayName, candidate.Name, newName,
                        $"error: {exception.GetType().Name}: {exception.Message}"));
                }
            }
        }

        processedRoots++;
        if (processedRoots % 25 == 0 || processedRoots == Math.Min(renameRoots.Count, maximumRenameRoots))
            Console.WriteLine($"Processed rename roots: {processedRoots}/{Math.Min(renameRoots.Count, maximumRenameRoots)}");
    }

}

List<CancellationTokenCandidate> cancellationTokenCandidates =
    await FindCancellationTokenCandidatesAsync(solution, sourceRoot);
var cancellationTokenChanges = new List<CancellationTokenChange>();
var cancellationTokenDocumentationChanges = new List<CancellationTokenDocumentationChange>();
var cancellationTokenImplementationChanges = new List<CancellationTokenImplementationChange>();
var cancellationTokenPropagationChanges = new List<CancellationTokenPropagationChange>();
var cancellationTokenUsageChanges = new List<CancellationTokenUsageChange>();
if (mode == "apply")
{
    (solution, cancellationTokenChanges) =
        await AddCancellationTokensAsync(solution, cancellationTokenCandidates);
    (solution, cancellationTokenImplementationChanges) =
        await AlignCancellationTokenImplementationsAsync(solution);
    (solution, cancellationTokenPropagationChanges) =
        await PropagateCancellationTokensAsync(solution);
    (solution, cancellationTokenUsageChanges) =
        await EnsureCancellationTokensAreUsedAsync(solution, sourceRoot);
    (solution, cancellationTokenDocumentationChanges) =
        await AddCancellationTokenDocumentationAsync(solution, sourceRoot);

    if (!workspace.TryApplyChanges(solution))
        throw new InvalidOperationException("MSBuildWorkspace rejected the updated solution.");
}

List<Candidate> remainingCandidates = mode == "apply"
    ? await FindCandidatesAsync(workspace.CurrentSolution, sourceRoot)
    : initialCandidates;
List<CancellationTokenCandidate> remainingCancellationTokenCandidates = mode == "apply"
    ? await FindCancellationTokenCandidatesAsync(workspace.CurrentSolution, sourceRoot)
    : cancellationTokenCandidates;
List<MisleadingAsyncSuffix> misleadingAsyncSuffixes =
    await FindMisleadingAsyncSuffixesAsync(mode == "apply" ? workspace.CurrentSolution : solution, repositoryRoot);

Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await File.WriteAllTextAsync(
    reportPath,
    JsonSerializer.Serialize(
        new
        {
            mode,
            solution = solutionPath,
            productProjects = solution.Projects.Count(project => IsProductProject(project, sourceRoot)),
            initialCandidateCount = initialCandidates.Count,
            apiCollisionChanges,
            initialRenameRootCount = renameRoots.Count,
            requestedRenameRoots = Math.Min(renameRoots.Count, maximumRenameRoots),
            remainingCandidateCount = remainingCandidates.Count,
            cancellationTokenCandidateCount = cancellationTokenCandidates.Count,
            remainingCancellationTokenCandidateCount = remainingCancellationTokenCandidates.Count,
            misleadingAsyncSuffixCount = misleadingAsyncSuffixes.Count,
            candidates = initialCandidates,
            applied,
            remaining = remainingCandidates,
            cancellationTokenCandidates,
            cancellationTokenChanges,
            cancellationTokenImplementationChanges,
            cancellationTokenPropagationChanges,
            cancellationTokenUsageChanges,
            cancellationTokenDocumentationChanges,
            remainingCancellationTokenCandidates,
            misleadingAsyncSuffixes,
            workspaceDiagnostics = workspaceDiagnostics.Distinct(StringComparer.Ordinal).Order().ToArray(),
        },
        new JsonSerializerOptions { WriteIndented = true }),
    CancellationToken.None);

Console.WriteLine($"Mode: {mode}");
Console.WriteLine($"Candidates: {initialCandidates.Count}");
Console.WriteLine($"Rename roots: {renameRoots.Count}");
Console.WriteLine($"Requested roots: {Math.Min(renameRoots.Count, maximumRenameRoots)}");
Console.WriteLine($"Remaining: {remainingCandidates.Count}");
Console.WriteLine($"Cancellation-token candidates: {cancellationTokenCandidates.Count}");
Console.WriteLine($"Remaining cancellation-token candidates: {remainingCancellationTokenCandidates.Count}");
Console.WriteLine($"Synchronous methods with an Async suffix: {misleadingAsyncSuffixes.Count}");
Console.WriteLine($"Report: {reportPath}");
return (remainingCandidates.Count == 0 && remainingCancellationTokenCandidates.Count == 0) || mode == "analyze" ? 0 : 1;

static async Task<List<Candidate>> FindCandidatesAsync(Solution solution, string sourceRoot)
{
    var candidates = new List<Candidate>();

    foreach (Project project in solution.Projects.Where(project => IsProductProject(project, sourceRoot)))
    {
        Compilation? compilation = await project.GetCompilationAsync();
        if (compilation is null)
            continue;

        foreach (INamedTypeSymbol type in AllNamespaceTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind == TypeKind.Delegate)
                continue;

            foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation) ||
                    method.Name.EndsWith("Async", StringComparison.Ordinal) ||
                    !method.Locations.Any(location => location.IsInSource) ||
                    !IsAsyncMethod(method) ||
                    ImplementsExternalContract(method))
                {
                    continue;
                }

                Location location = method.Locations.First(location => location.IsInSource);
                FileLinePositionSpan lineSpan = location.GetLineSpan();
                candidates.Add(new Candidate(
                    DocumentationCommentId.CreateDeclarationId(method)
                        ?? throw new InvalidOperationException($"No declaration ID for {method.ToDisplayString()}"),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    method.Name,
                    method.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    project.Name,
                    lineSpan.Path,
                    lineSpan.StartLinePosition.Line + 1,
                    method.IsOverride,
                    method.ExplicitInterfaceImplementations.Length > 0,
                    method.ContainingType.TypeKind == TypeKind.Interface));
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

static async Task<List<MisleadingAsyncSuffix>> FindMisleadingAsyncSuffixesAsync(
    Solution solution,
    string repositoryRoot)
{
    var findings = new List<MisleadingAsyncSuffix>();

    foreach (Project project in solution.Projects)
    {
        Compilation? compilation = await project.GetCompilationAsync();
        if (compilation is null)
            continue;

        foreach (INamedTypeSymbol type in AllNamespaceTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind == TypeKind.Delegate)
                continue;

            foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation) ||
                    !method.Name.EndsWith("Async", StringComparison.Ordinal) ||
                    IsAsyncMethod(method) ||
                    !method.Locations.Any(location => location.IsInSource))
                {
                    continue;
                }

                Location location = method.Locations.First(location => location.IsInSource);
                FileLinePositionSpan lineSpan = location.GetLineSpan();
                if (!Path.GetFullPath(lineSpan.Path).StartsWith(repositoryRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    continue;

                findings.Add(new MisleadingAsyncSuffix(
                    method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    method.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    project.Name,
                    lineSpan.Path,
                    lineSpan.StartLinePosition.Line + 1));
            }
        }
    }

    return findings
        .OrderBy(finding => finding.Project, StringComparer.Ordinal)
        .ThenBy(finding => finding.File, StringComparer.Ordinal)
        .ThenBy(finding => finding.Line)
        .ThenBy(finding => finding.DisplayName, StringComparer.Ordinal)
        .ToList();
}

static async Task<(Solution Solution, List<ApiCollisionChange> Changes)> ResolveKnownApiCollisionsAsync(Solution solution)
{
    var changes = new List<ApiCollisionChange>();
    Project? testingProject = solution.Projects.FirstOrDefault(project =>
        project.Name == "ViciOne.ServiceBus.Testing");
    Compilation? compilation = testingProject is null ? null : await testingProject.GetCompilationAsync();
    INamedTypeSymbol? extensions = compilation?.GetTypeByMetadataName(
        "ViciOne.ServiceBus.Testing.AsyncElementListExtensions");
    IMethodSymbol? firstAsync = extensions?.GetMembers("FirstAsync")
        .OfType<IMethodSymbol>()
        .SingleOrDefault(method =>
            method.Parameters.Length == 2 &&
            method.Parameters[0].Type.OriginalDefinition.ToDisplayString() ==
            "System.Collections.Generic.IAsyncEnumerable<T>");

    if (firstAsync is not null)
    {
        solution = await Renamer.RenameSymbolAsync(
            solution,
            firstAsync,
            new SymbolRenameOptions(
                RenameOverloads: false,
                RenameInStrings: false,
                RenameInComments: false,
                RenameFile: false),
            "FirstObservedAsync");
        changes.Add(new ApiCollisionChange(
            firstAsync.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            "FirstObservedAsync",
            "Avoids the .NET 10 System.Linq.AsyncEnumerable.FirstAsync extension collision."));
    }

    return (solution, changes);
}

static async Task<List<CancellationTokenCandidate>> FindCancellationTokenCandidatesAsync(
    Solution solution,
    string sourceRoot)
{
    var candidates = new List<CancellationTokenCandidate>();

    foreach (Project project in solution.Projects)
    {
        bool isProductProject = IsProductProject(project, sourceRoot);
        Compilation? compilation = await project.GetCompilationAsync();
        if (compilation is null)
            continue;

        foreach (INamedTypeSymbol type in AllNamespaceTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind == TypeKind.Delegate)
                continue;

            foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
            {
                bool publicApiCandidate =
                    isProductProject &&
                    IsExternallyVisible(type) &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    !IsCallbackMethod(method) &&
                    !IsContextBoundCancellationMethod(method) &&
                    !ImplementsExternalContract(method);
                bool sourceContractImplementationCandidate =
                    ImplementsSourceContractRequiringCancellationToken(method);
                if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation) ||
                    !method.Locations.Any(location => location.IsInSource) ||
                    !IsAsyncMethod(method) ||
                    HasCancellationToken(method) ||
                    (!publicApiCandidate && !sourceContractImplementationCandidate))
                {
                    continue;
                }

                Location location = method.Locations.First(location => location.IsInSource);
                FileLinePositionSpan lineSpan = location.GetLineSpan();
                candidates.Add(new CancellationTokenCandidate(
                    DocumentationCommentId.CreateDeclarationId(method)
                        ?? throw new InvalidOperationException($"No declaration ID for {method.ToDisplayString()}"),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    project.Name,
                    lineSpan.Path,
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

static async Task<(Solution Solution, List<CancellationTokenChange> Changes)> AddCancellationTokensAsync(
    Solution solution,
    IReadOnlyCollection<CancellationTokenCandidate> candidates)
{
    var candidateIdsByProject = candidates
        .GroupBy(candidate => candidate.Project, StringComparer.Ordinal)
        .ToDictionary(
            group => group.Key,
            group => group.Select(candidate => candidate.DeclarationId).ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
    var changes = new List<CancellationTokenChange>();

    foreach (ProjectId projectId in solution.ProjectIds)
    {
        Project project = solution.GetProject(projectId)!;
        if (!candidateIdsByProject.TryGetValue(project.Name, out HashSet<string>? candidateIds))
            continue;

        foreach (DocumentId documentId in project.DocumentIds)
        {
            Document document = solution.GetDocument(documentId)!;
            SyntaxNode? root = await document.GetSyntaxRootAsync();
            SemanticModel? semanticModel = await document.GetSemanticModelAsync();
            if (root is null || semanticModel is null)
                continue;

            var rewriter = new CancellationTokenParameterRewriter(semanticModel, candidateIds, changes);
            SyntaxNode updatedRoot = rewriter.Visit(root);
            if (!ReferenceEquals(root, updatedRoot))
                solution = solution.WithDocumentSyntaxRoot(documentId, updatedRoot);
        }
    }

    return (solution, changes);
}

static async Task<(Solution Solution, List<CancellationTokenDocumentationChange> Changes)>
    AddCancellationTokenDocumentationAsync(Solution solution, string sourceRoot)
{
    var changes = new List<CancellationTokenDocumentationChange>();

    foreach (ProjectId projectId in solution.ProjectIds)
    {
        Project project = solution.GetProject(projectId)!;
        foreach (DocumentId documentId in project.DocumentIds)
        {
            Document document = solution.GetDocument(documentId)!;
            SyntaxNode? root = await document.GetSyntaxRootAsync();
            if (root is null)
                continue;

            var rewriter = new CancellationTokenDocumentationRewriter(changes);
            SyntaxNode updatedRoot = rewriter.Visit(root);
            if (!ReferenceEquals(root, updatedRoot))
                solution = solution.WithDocumentSyntaxRoot(documentId, updatedRoot);
        }
    }

    return (solution, changes);
}

static async Task<(Solution Solution, List<CancellationTokenImplementationChange> Changes)>
    AlignCancellationTokenImplementationsAsync(Solution solution)
{
    var changes = new List<CancellationTokenImplementationChange>();

    foreach (ProjectId projectId in solution.ProjectIds)
    {
        Project project = solution.GetProject(projectId)!;
        foreach (DocumentId documentId in project.DocumentIds)
        {
            Document document = solution.GetDocument(documentId)!;
            SyntaxNode? root = await document.GetSyntaxRootAsync();
            if (root is null)
                continue;

            var rewriter = new CancellationTokenImplementationRewriter(changes);
            SyntaxNode updatedRoot = rewriter.Visit(root);
            if (!ReferenceEquals(root, updatedRoot))
                solution = solution.WithDocumentSyntaxRoot(documentId, updatedRoot);
        }
    }

    return (solution, changes);
}

static async Task<(Solution Solution, List<CancellationTokenPropagationChange> Changes)>
    PropagateCancellationTokensAsync(Solution solution)
{
    var changes = new List<CancellationTokenPropagationChange>();

    foreach (ProjectId projectId in solution.ProjectIds)
    {
        Project project = solution.GetProject(projectId)!;
        foreach (DocumentId documentId in project.DocumentIds)
        {
            Document document = solution.GetDocument(documentId)!;
            SyntaxNode? root = await document.GetSyntaxRootAsync();
            SemanticModel? semanticModel = await document.GetSemanticModelAsync();
            if (root is null || semanticModel is null)
                continue;

            var rewriter = new CancellationTokenPropagationRewriter(semanticModel, changes);
            SyntaxNode updatedRoot = rewriter.Visit(root);
            if (!ReferenceEquals(root, updatedRoot))
                solution = solution.WithDocumentSyntaxRoot(documentId, updatedRoot);
        }
    }

    return (solution, changes);
}

static async Task<(Solution Solution, List<CancellationTokenUsageChange> Changes)>
    EnsureCancellationTokensAreUsedAsync(Solution solution, string sourceRoot)
{
    var changes = new List<CancellationTokenUsageChange>();

    foreach (ProjectId projectId in solution.ProjectIds)
    {
        Project project = solution.GetProject(projectId)!;
        foreach (DocumentId documentId in project.DocumentIds)
        {
            Document document = solution.GetDocument(documentId)!;
            if (document.FilePath is null ||
                !Path.GetFullPath(document.FilePath).StartsWith(sourceRoot, StringComparison.Ordinal))
            {
                continue;
            }

            SyntaxNode? root = await document.GetSyntaxRootAsync();
            SemanticModel? semanticModel = await document.GetSemanticModelAsync();
            if (root is null || semanticModel is null)
                continue;

            var rewriter = new CancellationTokenUsageRewriter(semanticModel, changes);
            SyntaxNode updatedRoot = rewriter.Visit(root);
            if (!ReferenceEquals(root, updatedRoot))
                solution = solution.WithDocumentSyntaxRoot(documentId, updatedRoot);
        }
    }

    return (solution, changes);
}

static List<Candidate> SelectRenameRoots(IEnumerable<Candidate> candidates)
{
    return candidates
        .GroupBy(candidate => candidate.DeclarationId, StringComparer.Ordinal)
        .Select(group => group
            .OrderByDescending(candidate => candidate.IsInterfaceMember)
            .ThenBy(candidate => candidate.Line)
            .First())
        .OrderByDescending(candidate => candidate.IsInterfaceMember)
        .ThenBy(candidate => candidate.Project, StringComparer.Ordinal)
        .ThenBy(candidate => candidate.File, StringComparer.Ordinal)
        .ThenBy(candidate => candidate.Line)
        .ToList();
}

static async Task<IMethodSymbol?> ResolveCandidateAsync(Solution solution, Candidate candidate)
{
    Project? project = solution.Projects.FirstOrDefault(project =>
        StringComparer.Ordinal.Equals(project.Name, candidate.Project));
    Compilation? compilation = project is null ? null : await project.GetCompilationAsync();
    if (compilation is not null &&
        DocumentationCommentId.GetFirstSymbolForDeclarationId(candidate.DeclarationId, compilation) is IMethodSymbol method)
        return method;

    return null;
}

static string? FindCollision(IMethodSymbol symbol, string newName)
{
    foreach (IMethodSymbol existing in symbol.ContainingType.GetMembers(newName).OfType<IMethodSymbol>())
    {
        if (existing.Arity != symbol.Arity || existing.Parameters.Length != symbol.Parameters.Length)
            continue;

        bool sameParameters = existing.Parameters.Zip(symbol.Parameters).All(pair =>
            SymbolEqualityComparer.Default.Equals(pair.First.Type, pair.Second.Type) &&
            pair.First.RefKind == pair.Second.RefKind);
        if (sameParameters)
            return $"collision: {existing.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}";
    }

    return null;
}

static bool IsAsyncMethod(IMethodSymbol method)
{
    INamedTypeSymbol? returnType = method.ReturnType as INamedTypeSymbol;
    string metadataName = returnType?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
    return metadataName is "global::System.Threading.Tasks.Task" or
        "global::System.Threading.Tasks.Task<TResult>" or
        "global::System.Threading.Tasks.ValueTask" or
        "global::System.Threading.Tasks.ValueTask<TResult>";
}

static bool HasCancellationToken(IMethodSymbol method)
{
    return method.Parameters.Any(parameter =>
        parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
        "global::System.Threading.CancellationToken");
}

static bool IsCallbackMethod(IMethodSymbol method)
{
    if (method.ContainingType.TypeKind == TypeKind.Interface)
        return IsCallbackContract(method.ContainingType);

    foreach (INamedTypeSymbol interfaceType in method.ContainingType.AllInterfaces.Where(IsCallbackContract))
    {
        foreach (ISymbol interfaceMember in interfaceType.GetMembers(method.Name))
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

static bool IsCallbackContract(INamedTypeSymbol type)
{
    return GetMetadataName(type.OriginalDefinition) is
        "ViciOne.ServiceBus.IConsumer`1" or
        "ViciOne.ServiceBus.IFilter`1" or
        "ViciOne.ServiceBus.IPipe`1" or
        "ViciOne.ServiceBus.IProbeSite" or
        "ViciOne.ServiceBus.IExecuteActivity`1" or
        "ViciOne.ServiceBus.ICompensateActivity`1" or
        "ViciOne.ServiceBus.IActivity`2" or
        "ViciOne.ServiceBus.IJobConsumer`1" or
        "ViciOne.ServiceBus.IActivityObserver" or
        "ViciOne.ServiceBus.ISendObserver" or
        "ViciOne.ServiceBus.IPublishObserver" or
        "ViciOne.ServiceBus.IConsumeObserver" or
        "ViciOne.ServiceBus.IConsumeMessageObserver`1" or
        "ViciOne.ServiceBus.IReceiveObserver" or
        "ViciOne.ServiceBus.IReceiveEndpointObserver" or
        "ViciOne.ServiceBus.IReceiveTransportObserver" or
        "ViciOne.ServiceBus.IFilterObserver" or
        "ViciOne.ServiceBus.IFilterObserver`1" or
        "ViciOne.ServiceBus.IRetryObserver" or
        "ViciOne.ServiceBus.IBusObserver" or
        "ViciOne.ServiceBus.IConsumerFactory`1" or
        "ViciOne.ServiceBus.ISagaFactory`2" or
        "ViciOne.ServiceBus.ISagaPolicy`2" or
        "ViciOne.ServiceBus.ISagaRepository`1" or
        "ViciOne.ServiceBus.Saga.ISagaRepositoryContextFactory`1" or
        "ViciOne.ServiceBus.Saga.ISagaConsumeContextFactory`1" or
        "ViciOne.ServiceBus.Saga.ISagaConsumeContextFactory`2" or
        "ViciOne.ServiceBus.IStateMachineActivity" or
        "ViciOne.ServiceBus.IStateMachineActivity`1" or
        "ViciOne.ServiceBus.IStateMachineActivity`2" or
        "ViciOne.ServiceBus.IBehavior`1" or
        "ViciOne.ServiceBus.IBehavior`2" or
        "ViciOne.ServiceBus.IEventObserver`1" or
        "ViciOne.ServiceBus.IStateObserver`1";
}

static bool IsContextBoundCancellationMethod(IMethodSymbol method)
{
    string containingType = GetMetadataName(method.ContainingType.OriginalDefinition);
    if (containingType is "ViciOne.ServiceBus.ConsumeContext" or "ViciOne.ServiceBus.ConsumeContext`1")
    {
        return method.Name is "RespondAsync" or "ForwardAsync";
    }

    foreach (INamedTypeSymbol interfaceType in method.ContainingType.AllInterfaces)
    {
        string interfaceName = GetMetadataName(interfaceType.OriginalDefinition);
        if (interfaceName is not ("ViciOne.ServiceBus.ConsumeContext" or "ViciOne.ServiceBus.ConsumeContext`1"))
            continue;

        foreach (ISymbol interfaceMember in interfaceType.GetMembers(method.Name))
        {
            if (SymbolEqualityComparer.Default.Equals(
                    method.ContainingType.FindImplementationForInterfaceMember(interfaceMember),
                    method))
            {
                return method.Name is "RespondAsync" or "ForwardAsync";
            }
        }
    }

    if (!method.IsExtensionMethod || method.Parameters.Length == 0)
        return false;

    string receiverType = GetMetadataName(method.Parameters[0].Type.OriginalDefinition as INamedTypeSymbol);
    return receiverType is "ViciOne.ServiceBus.ConsumeContext" or "ViciOne.ServiceBus.ConsumeContext`1" &&
        method.Name is "RespondAsync" or "ForwardAsync";
}

static bool ImplementsSourceContractRequiringCancellationToken(IMethodSymbol method)
{
    string methodName = method.Name[(method.Name.LastIndexOf('.') + 1)..];
    foreach (INamedTypeSymbol interfaceType in method.ContainingType.AllInterfaces)
    {
        if (IsCallbackContract(interfaceType))
            continue;

        foreach (IMethodSymbol interfaceMethod in interfaceType.GetMembers(methodName).OfType<IMethodSymbol>())
        {
            if (IsProductContract(interfaceMethod) &&
                IsContractShapeWithAddedCancellationToken(method, interfaceMethod))
            {
                return true;
            }
        }
    }

    for (INamedTypeSymbol? baseType = method.ContainingType.BaseType;
         baseType is not null;
         baseType = baseType.BaseType)
    {
        foreach (IMethodSymbol baseMethod in baseType.GetMembers(methodName).OfType<IMethodSymbol>())
        {
            if (IsProductContract(baseMethod) &&
                IsContractShapeWithAddedCancellationToken(method, baseMethod))
            {
                return true;
            }
        }
    }

    return false;
}

static bool IsProductContract(IMethodSymbol method)
{
    return method.Locations.Any(location => location.IsInSource) ||
        method.ContainingAssembly.Name.StartsWith("ViciOne.ServiceBus", StringComparison.Ordinal);
}

static bool IsContractShapeWithAddedCancellationToken(IMethodSymbol implementation, IMethodSymbol contract)
{
    if (implementation.Arity != contract.Arity ||
        contract.Parameters.Length != implementation.Parameters.Length + 1 ||
        !HasCancellationToken(contract) ||
        contract.Parameters[^1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
        "global::System.Threading.CancellationToken")
    {
        return false;
    }

    return implementation.Parameters.Zip(contract.Parameters).All(pair =>
        pair.First.RefKind == pair.Second.RefKind &&
        SymbolEqualityComparer.Default.Equals(pair.First.Type, pair.Second.Type));
}

static string GetMetadataName(INamedTypeSymbol? type)
{
    if (type is null)
        return string.Empty;

    var names = new Stack<string>();
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        names.Push(current.MetadataName);

    string typeName = string.Join("+", names);
    string namespaceName = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    return string.IsNullOrEmpty(namespaceName) ? typeName : namespaceName + "." + typeName;
}

static bool ImplementsExternalContract(IMethodSymbol method)
{
    if (method.OverriddenMethod is { } overriddenMethod &&
        !SymbolEqualityComparer.Default.Equals(overriddenMethod.ContainingAssembly, method.ContainingAssembly))
    {
        return true;
    }

    if (method.ExplicitInterfaceImplementations.Any(interfaceMethod =>
            !SymbolEqualityComparer.Default.Equals(
                interfaceMethod.ContainingAssembly,
                method.ContainingAssembly)))
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

static IEnumerable<INamedTypeSymbol> AllNamespaceTypes(INamespaceSymbol root)
{
    foreach (INamespaceOrTypeSymbol member in root.GetMembers())
    {
        if (member is INamespaceSymbol childNamespace)
        {
            foreach (INamedTypeSymbol type in AllNamespaceTypes(childNamespace))
                yield return type;
        }
        else if (member is INamedTypeSymbol type)
        {
            foreach (INamedTypeSymbol nested in AllNestedTypes(type))
                yield return nested;
        }
    }
}

static IEnumerable<INamedTypeSymbol> AllNestedTypes(INamedTypeSymbol root)
{
    yield return root;
    foreach (INamedTypeSymbol nested in root.GetTypeMembers())
    {
        foreach (INamedTypeSymbol type in AllNestedTypes(nested))
            yield return type;
    }
}

static bool IsExternallyVisible(INamedTypeSymbol type)
{
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
        if (current.DeclaredAccessibility != Accessibility.Public)
            return false;
    }

    return true;
}

static bool IsProductProject(Project project, string sourceRoot)
{
    return project.FilePath is not null &&
        Path.GetFullPath(project.FilePath).StartsWith(sourceRoot, StringComparison.Ordinal);
}

static string FindRepositoryRoot(string solutionPath)
{
    DirectoryInfo? directory = new FileInfo(solutionPath).Directory;
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        directory = directory.Parent;

    return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
}

sealed record Candidate(
    string DeclarationId,
    string DisplayName,
    string Name,
    string ContainingType,
    string Project,
    string File,
    int Line,
    bool IsOverride,
    bool IsExplicitInterfaceImplementation,
    bool IsInterfaceMember);

sealed record ApiCollisionChange(string Symbol, string NewName, string Reason);

sealed record RenameResult(string DeclarationId, string DisplayName, string OldName, string NewName, string Status);

sealed record MisleadingAsyncSuffix(string DisplayName, string ReturnType, string Project, string File, int Line);

sealed record CancellationTokenCandidate(string DeclarationId, string DisplayName, string Project, string File, int Line);

sealed record CancellationTokenChange(string DeclarationId, string DisplayName, string File, int Line);

sealed record CancellationTokenDocumentationChange(string File, int Line);

sealed record CancellationTokenImplementationChange(string File, int Line, string Kind);

sealed record CancellationTokenPropagationChange(string File, int Line, string Target);

sealed record CancellationTokenUsageChange(string File, int Line, string Method, string GuardKind);

sealed class CancellationTokenParameterRewriter(
    SemanticModel semanticModel,
    IReadOnlySet<string> candidateIds,
    ICollection<CancellationTokenChange> changes) : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        IMethodSymbol? method = semanticModel.GetDeclaredSymbol(node);
        string? declarationId = method is null ? null : DocumentationCommentId.CreateDeclarationId(method);
        if (method is null || declarationId is null || !candidateIds.Contains(declarationId))
            return base.VisitMethodDeclaration(node);

        if (node.ParameterList.Parameters.Any(parameter => parameter.Identifier.ValueText == "cancellationToken"))
            return base.VisitMethodDeclaration(node);

        FileLinePositionSpan lineSpan = method.Locations.First(location => location.IsInSource).GetLineSpan();
        changes.Add(new CancellationTokenChange(
            declarationId,
            method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1));

        ParameterSyntax cancellationToken = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
            .WithType(SyntaxFactory.IdentifierName("CancellationToken")
                .WithTrailingTrivia(SyntaxFactory.Space))
            .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression))
                .WithEqualsToken(SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(SyntaxFactory.Space),
                    SyntaxKind.EqualsToken,
                    SyntaxFactory.TriviaList(SyntaxFactory.Space))));
        MethodDeclarationSyntax updated = node.WithParameterList(
            node.ParameterList.AddParameters(cancellationToken));
        return base.VisitMethodDeclaration(updated);
    }
}

sealed class CancellationTokenImplementationRewriter(
    ICollection<CancellationTokenImplementationChange> changes) : CSharpSyntaxRewriter
{
    static readonly HashSet<string> SourceContractsWithCancellation = new(StringComparer.Ordinal)
    {
        "IPropertyConverter",
        "IPropertyProvider",
        "ISendContextPipe",
        "State",
        "StateMachine",
    };

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        MethodDeclarationSyntax updated = node;
        ParameterSyntax? cancellationToken = node.ParameterList.Parameters.FirstOrDefault(parameter =>
            parameter.Identifier.ValueText == "cancellationToken" &&
            parameter.Type?.ToString().EndsWith("CancellationToken", StringComparison.Ordinal) == true);

        bool implementationCannotDeclareOptionalDefault =
            node.ExplicitInterfaceSpecifier is not null ||
            node.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
            !node.Identifier.ValueText.EndsWith("Async", StringComparison.Ordinal);
        if (cancellationToken?.Default is not null && implementationCannotDeclareOptionalDefault)
        {
            updated = updated.ReplaceNode(cancellationToken, cancellationToken.WithDefault(null));
            Record(node, "removed-implementation-default");
        }
        if (cancellationToken is null && RequiresSourceContractCancellationToken(node))
        {
            updated = updated.WithParameterList(updated.ParameterList.AddParameters(CreateRequiredCancellationToken()));
            Record(node, "added-required-implementation-parameter");
        }

        return base.VisitMethodDeclaration(updated);
    }

    static bool RequiresSourceContractCancellationToken(MethodDeclarationSyntax node)
    {
        if (!node.Identifier.ValueText.EndsWith("Async", StringComparison.Ordinal))
            return false;

        if (node.ExplicitInterfaceSpecifier is not null)
        {
            string interfaceName = node.ExplicitInterfaceSpecifier.Name switch
            {
                GenericNameSyntax genericName => genericName.Identifier.ValueText,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
                AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
                IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                _ => node.ExplicitInterfaceSpecifier.Name.ToString(),
            };
            return SourceContractsWithCancellation.Contains(interfaceName);
        }

        return node.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
            node.Identifier.ValueText == "NotifyFaultedAsync";
    }

    static ParameterSyntax CreateRequiredCancellationToken()
    {
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
            .WithType(SyntaxFactory.IdentifierName("CancellationToken")
                .WithTrailingTrivia(SyntaxFactory.Space));
    }

    void Record(MethodDeclarationSyntax node, string kind)
    {
        FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
        changes.Add(new CancellationTokenImplementationChange(
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1,
            kind));
    }
}

sealed class CancellationTokenPropagationRewriter(
    SemanticModel semanticModel,
    ICollection<CancellationTokenPropagationChange> changes) : CSharpSyntaxRewriter
{
    int _cancellationTokenScopeDepth;

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        bool declaresCancellationToken = node.ParameterList.Parameters.Any(IsCancellationTokenParameter);
        if (declaresCancellationToken)
            _cancellationTokenScopeDepth++;

        SyntaxNode? updated = base.VisitMethodDeclaration(node);

        if (declaresCancellationToken)
            _cancellationTokenScopeDepth--;

        return updated;
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        InvocationExpressionSyntax visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
        if (_cancellationTokenScopeDepth == 0 ||
            node.ArgumentList.Arguments.Any(argument =>
                argument.NameColon?.Name.Identifier.ValueText == "cancellationToken" ||
                argument.Expression is IdentifierNameSyntax { Identifier.ValueText: "cancellationToken" }))
        {
            return visited;
        }

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);
        IMethodSymbol? target = (symbolInfo.Symbol as IMethodSymbol) ??
            symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(method => CanAppendCancellationToken(method, node));
        if (target is null || !CanAppendCancellationToken(target, node))
            return visited;

        ArgumentSyntax argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
            .WithNameColon(SyntaxFactory.NameColon("cancellationToken"));
        FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
        changes.Add(new CancellationTokenPropagationChange(
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1,
            target.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));

        return visited.WithArgumentList(visited.ArgumentList.AddArguments(argument));
    }

    static bool CanAppendCancellationToken(IMethodSymbol method, InvocationExpressionSyntax invocation)
    {
        IMethodSymbol effectiveMethod = method.ReducedFrom is null ? method : method;
        if (effectiveMethod.Parameters.Length == 0 ||
            !IsCancellationToken(effectiveMethod.Parameters[^1].Type) ||
            invocation.ArgumentList.Arguments.Count >= effectiveMethod.Parameters.Length)
        {
            return false;
        }

        int requiredParametersBeforeToken = effectiveMethod.Parameters
            .Take(effectiveMethod.Parameters.Length - 1)
            .Count(parameter => !parameter.IsOptional && !parameter.IsParams);
        return invocation.ArgumentList.Arguments.Count >= requiredParametersBeforeToken;
    }

    static bool IsCancellationTokenParameter(ParameterSyntax parameter)
    {
        return parameter.Identifier.ValueText == "cancellationToken" &&
            parameter.Type?.ToString().EndsWith("CancellationToken", StringComparison.Ordinal) == true;
    }

    static bool IsCancellationToken(ITypeSymbol type)
    {
        return type.Name == nameof(CancellationToken) &&
            type.ContainingNamespace.ToDisplayString() == "System.Threading";
    }
}

sealed class CancellationTokenUsageRewriter(
    SemanticModel semanticModel,
    ICollection<CancellationTokenUsageChange> changes) : CSharpSyntaxRewriter
{
    private static readonly SymbolDisplayFormat FullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!node.Modifiers.Any(SyntaxKind.PublicKeyword) ||
            node.Body is null && node.ExpressionBody is null)
        {
            return base.VisitMethodDeclaration(node);
        }

        ParameterSyntax? parameter = node.ParameterList.Parameters.FirstOrDefault(IsCancellationTokenParameter);
        if (parameter is null || UsesCancellationToken(node, parameter.Identifier.ValueText))
            return base.VisitMethodDeclaration(node);

        IMethodSymbol? method = semanticModel.GetDeclaredSymbol(node);
        if (method is null)
            return base.VisitMethodDeclaration(node);

        StatementSyntax guard = CreateGuard(method);
        MethodDeclarationSyntax updated = node.Body is not null
            ? node.WithBody(node.Body.WithStatements(node.Body.Statements.Insert(0, guard)))
            : ConvertExpressionBody(node, method, guard);

        FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
        changes.Add(new CancellationTokenUsageChange(
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1,
            method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            method.IsAsync ? "async-cancellation-check" : GetGuardKind(method.ReturnType)));
        return base.VisitMethodDeclaration(updated);
    }

    private static MethodDeclarationSyntax ConvertExpressionBody(
        MethodDeclarationSyntax node,
        IMethodSymbol method,
        StatementSyntax guard)
    {
        ExpressionSyntax expression = node.ExpressionBody!.Expression;
        StatementSyntax result = expression switch
        {
            ThrowExpressionSyntax thrown => SyntaxFactory.ThrowStatement(thrown.Expression),
            _ when method.ReturnsVoid => SyntaxFactory.ExpressionStatement(expression),
            _ => SyntaxFactory.ReturnStatement(expression).WithReturnKeyword(
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(),
                    SyntaxKind.ReturnKeyword,
                    SyntaxFactory.TriviaList(SyntaxFactory.Space))),
        };

        return node
            .WithExpressionBody(null)
            .WithSemicolonToken(default)
            .WithBody(SyntaxFactory.Block(guard, result));
    }

    private static StatementSyntax CreateGuard(IMethodSymbol method)
    {
        if (method.IsAsync)
            return SyntaxFactory.ParseStatement("cancellationToken.ThrowIfCancellationRequested();");

        string returnType = GetTaskLikeReturnType(method.ReturnType, out ITypeSymbol? resultType);
        if (returnType.Length == 0)
            return SyntaxFactory.ParseStatement("cancellationToken.ThrowIfCancellationRequested();");

        string factory = resultType is null
            ? $"global::System.Threading.Tasks.{returnType}.FromCanceled(cancellationToken)"
            : $"global::System.Threading.Tasks.{returnType}.FromCanceled<{resultType.ToDisplayString(FullyQualifiedNullableFormat)}>(cancellationToken)";
        return SyntaxFactory.ParseStatement(
            $"if (cancellationToken.IsCancellationRequested) return {factory};\n");
    }

    private static string GetGuardKind(ITypeSymbol returnType) =>
        GetTaskLikeReturnType(returnType, out _) is { Length: > 0 } taskLike
            ? $"{taskLike.ToLowerInvariant()}-canceled-result"
            : "synchronous-cancellation-check";

    private static string GetTaskLikeReturnType(ITypeSymbol returnType, out ITypeSymbol? resultType)
    {
        resultType = null;
        if (returnType is not INamedTypeSymbol namedType)
            return string.Empty;

        string original = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (original == "global::System.Threading.Tasks.Task")
            return "Task";
        if (original == "global::System.Threading.Tasks.ValueTask")
            return "ValueTask";
        if (original == "global::System.Threading.Tasks.Task<TResult>")
        {
            resultType = namedType.TypeArguments[0];
            return "Task";
        }
        if (original == "global::System.Threading.Tasks.ValueTask<TResult>")
        {
            resultType = namedType.TypeArguments[0];
            return "ValueTask";
        }

        return string.Empty;
    }

    private static bool UsesCancellationToken(MethodDeclarationSyntax method, string parameterName)
    {
        SyntaxNode implementation = (SyntaxNode?)method.Body ?? method.ExpressionBody!;
        return implementation.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => identifier.Identifier.ValueText == parameterName && !IsNameOfArgument(identifier));
    }

    private static bool IsCancellationTokenParameter(ParameterSyntax parameter) =>
        parameter.Identifier.ValueText == "cancellationToken" &&
        parameter.Type?.ToString().EndsWith("CancellationToken", StringComparison.Ordinal) == true;

    private static bool IsNameOfArgument(IdentifierNameSyntax identifier) =>
        identifier.Ancestors()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });
}

sealed class CancellationTokenDocumentationRewriter(
    ICollection<CancellationTokenDocumentationChange> changes) : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!node.ParameterList.Parameters.Any(parameter =>
                parameter.Identifier.ValueText == "cancellationToken" &&
                parameter.Type?.ToString().EndsWith("CancellationToken", StringComparison.Ordinal) == true))
        {
            return base.VisitMethodDeclaration(node);
        }

        SyntaxToken firstToken = node.GetFirstToken(includeZeroWidth: true);
        SyntaxTriviaList leadingTrivia = firstToken.LeadingTrivia;
        int documentationIndex = -1;
        for (int index = 0; index < leadingTrivia.Count; index++)
        {
            if (leadingTrivia[index].GetStructure() is DocumentationCommentTriviaSyntax)
                documentationIndex = index;
        }

        if (documentationIndex < 0)
        {
            return base.VisitMethodDeclaration(node);
        }

        string existingDocumentation = leadingTrivia[documentationIndex].ToFullString();
        string[] missingParameters = node.ParameterList.Parameters
            .Select(parameter => parameter.Identifier.ValueText)
            .Where(parameterName => !existingDocumentation.Contains(
                $"<param name=\"{parameterName}\"",
                StringComparison.Ordinal))
            .ToArray();
        if (missingParameters.Length == 0)
            return base.VisitMethodDeclaration(node);

        FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
        string indentation = new(' ', lineSpan.StartLinePosition.Character);
        SyntaxTriviaList parameterDocumentation = SyntaxFactory.ParseLeadingTrivia(
            string.Concat(missingParameters.Select(parameterName =>
                indentation +
                $"/// <param name=\"{parameterName}\">{DescribeParameter(parameterName)}</param>" +
                Environment.NewLine)));
        SyntaxTriviaList updatedTrivia = leadingTrivia.InsertRange(
            documentationIndex + 1,
            parameterDocumentation);
        changes.Add(new CancellationTokenDocumentationChange(
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1));

        MethodDeclarationSyntax updated = node.ReplaceToken(
            firstToken,
            firstToken.WithLeadingTrivia(updatedTrivia));
        return base.VisitMethodDeclaration(updated);
    }

    static string DescribeParameter(string parameterName)
    {
        return parameterName switch
        {
            "cancellationToken" => "The token used to cancel the operation.",
            "message" => "The message processed by the operation.",
            "context" => "The context for the operation.",
            "timeout" => "The maximum time allowed for the operation.",
            "timeProvider" => "The time source used by the operation.",
            "busControl" => "The bus control instance.",
            "busControls" => "The bus control instances.",
            "expectedStatus" => "The health status to wait for.",
            _ => $"The {SplitIdentifier(parameterName)} used by the operation.",
        };
    }

    static string SplitIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "value";

        var result = new System.Text.StringBuilder(value.Length + 4);
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]))
                result.Append(' ');
            result.Append(char.ToLowerInvariant(character));
        }

        return result.ToString();
    }
}
