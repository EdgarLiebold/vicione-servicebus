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

if (args.Length != 3 || args[0] is not ("analyze" or "apply"))
{
    Console.Error.WriteLine(
        "Usage: dotnet run tools/api-conventions/MigratePublicDateTime.cs -- <analyze|apply> <solution.slnx> <report.json>");
    return 2;
}

string mode = args[0];
string solutionPath = Path.GetFullPath(args[1]);
string reportPath = Path.GetFullPath(args[2]);
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
var changes = new List<DateTimeSignatureChange>();

foreach (ProjectId projectId in solution.ProjectIds)
{
    Project project = solution.GetProject(projectId)!;
    if (project.FilePath is null ||
        !Path.GetFullPath(project.FilePath).StartsWith(sourceRoot, StringComparison.Ordinal))
    {
        continue;
    }

    foreach (DocumentId documentId in project.DocumentIds)
    {
        Document document = solution.GetDocument(documentId)!;
        SyntaxNode? root = await document.GetSyntaxRootAsync();
        SemanticModel? semanticModel = await document.GetSemanticModelAsync();
        if (root is null || semanticModel is null)
            continue;

        TypeSyntax[] candidates = root.DescendantNodes()
            .OfType<TypeSyntax>()
            .Where(typeSyntax => IsDateTime(typeSyntax, semanticModel))
            .Where(typeSyntax => !IsDateTime(typeSyntax.Parent as TypeSyntax, semanticModel))
            .Where(typeSyntax => IsPublicSignatureType(typeSyntax, semanticModel))
            .ToArray();
        if (candidates.Length == 0)
            continue;

        foreach (TypeSyntax candidate in candidates)
        {
            FileLinePositionSpan lineSpan = candidate.GetLocation().GetLineSpan();
            changes.Add(new DateTimeSignatureChange(
                project.Name,
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                PublicMemberName(candidate, semanticModel)));
        }

        if (mode == "apply")
        {
            SyntaxNode updatedRoot = root.ReplaceNodes(
                candidates,
                (original, _) => SyntaxFactory.IdentifierName("DateTimeOffset").WithTriviaFrom(original));
            solution = solution.WithDocumentSyntaxRoot(documentId, updatedRoot);
        }
    }
}

if (mode == "apply" && !workspace.TryApplyChanges(solution))
    throw new InvalidOperationException("MSBuildWorkspace rejected the updated solution.");

Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await File.WriteAllTextAsync(
    reportPath,
    JsonSerializer.Serialize(
        new
        {
            mode,
            solution = solutionPath,
            changeCount = changes.Count,
            changes = changes
                .OrderBy(change => change.Project, StringComparer.Ordinal)
                .ThenBy(change => change.File, StringComparer.Ordinal)
                .ThenBy(change => change.Line)
                .ToArray(),
            workspaceDiagnostics = workspaceDiagnostics.Distinct(StringComparer.Ordinal).Order().ToArray(),
        },
        new JsonSerializerOptions { WriteIndented = true }),
    CancellationToken.None);

Console.WriteLine($"Mode: {mode}");
Console.WriteLine($"Public DateTime signature nodes: {changes.Count}");
Console.WriteLine($"Report: {reportPath}");
return 0;

static bool IsDateTime(TypeSyntax? typeSyntax, SemanticModel semanticModel)
{
    return typeSyntax is not null && semanticModel.GetTypeInfo(typeSyntax).Type?.SpecialType == SpecialType.System_DateTime;
}

static bool IsPublicSignatureType(TypeSyntax typeSyntax, SemanticModel semanticModel)
{
    for (SyntaxNode? current = typeSyntax.Parent; current is not null; current = current.Parent)
    {
        switch (current)
        {
            case MethodDeclarationSyntax method:
                return IsWithin(typeSyntax, method.ReturnType) ||
                    method.ParameterList.Parameters.Any(parameter => parameter.Type is not null && IsWithin(typeSyntax, parameter.Type))
                    ? IsExternallyVisible(semanticModel.GetDeclaredSymbol(method))
                    : false;
            case ConstructorDeclarationSyntax constructor:
                return constructor.ParameterList.Parameters.Any(parameter => parameter.Type is not null && IsWithin(typeSyntax, parameter.Type))
                    ? IsExternallyVisible(semanticModel.GetDeclaredSymbol(constructor))
                    : false;
            case PropertyDeclarationSyntax property:
                return IsWithin(typeSyntax, property.Type) && IsExternallyVisible(semanticModel.GetDeclaredSymbol(property));
            case IndexerDeclarationSyntax indexer:
                return (IsWithin(typeSyntax, indexer.Type) ||
                        indexer.ParameterList.Parameters.Any(parameter => parameter.Type is not null && IsWithin(typeSyntax, parameter.Type))) &&
                    IsExternallyVisible(semanticModel.GetDeclaredSymbol(indexer));
            case FieldDeclarationSyntax field:
                return IsWithin(typeSyntax, field.Declaration.Type) &&
                    field.Declaration.Variables.Any(variable => IsExternallyVisible(semanticModel.GetDeclaredSymbol(variable)));
            case EventFieldDeclarationSyntax eventField:
                return IsWithin(typeSyntax, eventField.Declaration.Type) &&
                    eventField.Declaration.Variables.Any(variable => IsExternallyVisible(semanticModel.GetDeclaredSymbol(variable)));
            case EventDeclarationSyntax eventDeclaration:
                return IsWithin(typeSyntax, eventDeclaration.Type) && IsExternallyVisible(semanticModel.GetDeclaredSymbol(eventDeclaration));
            case DelegateDeclarationSyntax delegateDeclaration:
                return (IsWithin(typeSyntax, delegateDeclaration.ReturnType) ||
                        delegateDeclaration.ParameterList.Parameters.Any(parameter => parameter.Type is not null && IsWithin(typeSyntax, parameter.Type))) &&
                    IsExternallyVisible(semanticModel.GetDeclaredSymbol(delegateDeclaration));
            case RecordDeclarationSyntax recordDeclaration when recordDeclaration.ParameterList is not null:
                return recordDeclaration.ParameterList.Parameters.Any(parameter => parameter.Type is not null && IsWithin(typeSyntax, parameter.Type)) &&
                    IsExternallyVisible(semanticModel.GetDeclaredSymbol(recordDeclaration));
            case BaseTypeDeclarationSyntax:
                return false;
        }
    }

    return false;
}

static bool IsWithin(SyntaxNode candidate, SyntaxNode container)
{
    return container.Span.Contains(candidate.Span);
}

static bool IsExternallyVisible(ISymbol? symbol)
{
    if (symbol is null || symbol.DeclaredAccessibility != Accessibility.Public)
        return false;

    for (INamedTypeSymbol? type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
         type is not null;
         type = type.ContainingType)
    {
        if (type.DeclaredAccessibility != Accessibility.Public)
            return false;
    }

    return true;
}

static string PublicMemberName(TypeSyntax typeSyntax, SemanticModel semanticModel)
{
    SyntaxNode? declaration = typeSyntax.Ancestors().FirstOrDefault(node =>
        node is MemberDeclarationSyntax or RecordDeclarationSyntax);
    ISymbol? symbol = declaration switch
    {
        MethodDeclarationSyntax method => semanticModel.GetDeclaredSymbol(method),
        ConstructorDeclarationSyntax constructor => semanticModel.GetDeclaredSymbol(constructor),
        PropertyDeclarationSyntax property => semanticModel.GetDeclaredSymbol(property),
        IndexerDeclarationSyntax indexer => semanticModel.GetDeclaredSymbol(indexer),
        EventDeclarationSyntax eventDeclaration => semanticModel.GetDeclaredSymbol(eventDeclaration),
        DelegateDeclarationSyntax delegateDeclaration => semanticModel.GetDeclaredSymbol(delegateDeclaration),
        RecordDeclarationSyntax recordDeclaration => semanticModel.GetDeclaredSymbol(recordDeclaration),
        FieldDeclarationSyntax field => field.Declaration.Variables
            .Select(variable => semanticModel.GetDeclaredSymbol(variable))
            .FirstOrDefault(candidate => candidate is not null),
        EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables
            .Select(variable => semanticModel.GetDeclaredSymbol(variable))
            .FirstOrDefault(candidate => candidate is not null),
        _ => null,
    };
    return symbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? declaration?.ToString() ?? string.Empty;
}

static string FindRepositoryRoot(string solutionPath)
{
    DirectoryInfo? directory = new FileInfo(solutionPath).Directory;
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        directory = directory.Parent;

    return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
}

sealed record DateTimeSignatureChange(string Project, string File, int Line, string Member);
