using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.References;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;

/// <summary>Runs analyzers and code fixes against real, binding C# compilations.</summary>
/// <remarks>
/// The host is deliberately assertion-framework neutral. It returns observations; the executable
/// xUnit project owns every verdict. A fixture that does not compile throws before an analyzer is
/// invoked, preventing an invalid source from looking like a successful no-diagnostic case.
/// </remarks>
public static class RoslynTestHost
{
    private const string ProjectName = "AnalyzerFixture";

    /// <summary>Validates that one or more fixture sources form a binding compilation.</summary>
    /// <param name="sources">The sources used by the operation.</param>
    /// <param name="referenceRoots">The reference roots used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task ValidateCompilationAsync(
        IReadOnlyList<string> sources,
        IReadOnlyCollection<Assembly>? referenceRoots = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);

        using var workspace = new AdhocWorkspace();
        var project = CreateProject(workspace, sources, referenceRoots ?? []);
        _ = await GetBindingCompilationAsync(project, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Validates that one fixture source forms a binding compilation.</summary>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="referenceRoots">The reference roots used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task ValidateCompilationAsync(
        string source,
        IReadOnlyCollection<Assembly>? referenceRoots = null,
        CancellationToken cancellationToken = default) =>
        ValidateCompilationAsync([source], referenceRoots, cancellationToken);

    /// <summary>Runs one analyzer over one or more source documents.</summary>
    /// <param name="sources">The sources used by the operation.</param>
    /// <param name="analyzer">The analyzer used by the operation.</param>
    /// <param name="referenceRoots">The reference roots used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<IReadOnlyList<DiagnosticObservation>> AnalyzeAsync(
        IReadOnlyList<string> sources,
        DiagnosticAnalyzer analyzer,
        IReadOnlyCollection<Assembly>? referenceRoots = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(analyzer);

        using var workspace = new AdhocWorkspace();
        var project = CreateProject(workspace, sources, referenceRoots ?? []);
        var diagnostics = await AnalyzeProjectAsync(project, analyzer, cancellationToken).ConfigureAwait(false);

        return diagnostics.Select(DiagnosticObservation.From).ToArray();
    }

    /// <summary>Runs one analyzer over one source document.</summary>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="analyzer">The analyzer used by the operation.</param>
    /// <param name="referenceRoots">The reference roots used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<IReadOnlyList<DiagnosticObservation>> AnalyzeAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        IReadOnlyCollection<Assembly>? referenceRoots = null,
        CancellationToken cancellationToken = default) =>
        AnalyzeAsync([source], analyzer, referenceRoots, cancellationToken);

    /// <summary>
    /// Applies the first registered fix repeatedly until the selected analyzer has no diagnostic.
    /// </summary>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="analyzer">The analyzer used by the operation.</param>
    /// <param name="codeFixProvider">The code fix provider used by the operation.</param>
    /// <param name="referenceRoots">The reference roots used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<string> ApplyAllFixesAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFixProvider,
        IReadOnlyCollection<Assembly>? referenceRoots = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(codeFixProvider);

        using var workspace = new AdhocWorkspace();
        var project = CreateProject(workspace, [source], referenceRoots ?? []);
        var document = project.Documents.Single();
        var diagnostics = await AnalyzeProjectAsync(project, analyzer, cancellationToken).ConfigureAwait(false);
        var maximumApplications = diagnostics.Count;

        for (var application = 0; application < maximumApplications && diagnostics.Count > 0; application++)
        {
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(
                document,
                diagnostics[0],
                (action, _) => actions.Add(action),
                cancellationToken);

            await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            if (actions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{codeFixProvider.GetType().Name} registered no fix for {diagnostics[0].Id}.");
            }

            var operations = await actions[0].GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            var changes = operations.OfType<ApplyChangesOperation>().ToArray();

            if (changes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"The selected code action produced {changes.Length} ApplyChangesOperation instances; exactly one is required.");
            }

            document = changes[0].ChangedSolution.GetDocument(document.Id)
                ?? throw new InvalidOperationException("The code fix removed its source document.");

            await ThrowOnCompilationErrorsAsync(document.Project, cancellationToken).ConfigureAwait(false);
            diagnostics = await AnalyzeProjectAsync(document.Project, analyzer, cancellationToken).ConfigureAwait(false);
        }

        if (diagnostics.Count != 0)
        {
            throw new InvalidOperationException(
                $"{diagnostics.Count} analyzer diagnostic(s) remained after applying every available code fix.");
        }

        var simplified = await Simplifier.ReduceAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var root = await simplified.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The fixed source document has no syntax root.");
        // Format only the nodes the code fix marked. Reformatting the whole fixture would let an
        // unrelated whitespace rewrite hide whether the provider produced a minimal source delta.
        var formatted = Formatter.Format(
            root,
            Formatter.Annotation,
            simplified.Project.Solution.Workspace, cancellationToken: cancellationToken);

        return NormalizeLineEndings(formatted.GetText().ToString());
    }

    /// <summary>Normalizes text for exact cross-platform fixed-source comparisons.</summary>
    public static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

    private static Project CreateProject(
        AdhocWorkspace workspace,
        IReadOnlyList<string> sources,
        IReadOnlyCollection<Assembly> referenceRoots)
    {
        if (sources.Count == 0)
        {
            throw new ArgumentException("At least one source document is required.", nameof(sources));
        }

        var projectId = ProjectId.CreateNewId(ProjectName);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, ProjectName, ProjectName, LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp14))
            .WithProjectCompilationOptions(
                projectId,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(projectId, MetadataReferenceClosure.Create(referenceRoots));

        for (var index = 0; index < sources.Count; index++)
        {
            var fileName = $"Test{index}.cs";
            solution = solution.AddDocument(
                DocumentId.CreateNewId(projectId, fileName),
                fileName,
                SourceText.From(sources[index]),
                filePath: fileName);
        }

        return solution.GetProject(projectId)
            ?? throw new InvalidOperationException("The Roslyn fixture project could not be created.");
    }

    private static async Task<IReadOnlyList<Diagnostic>> AnalyzeProjectAsync(
        Project project,
        DiagnosticAnalyzer analyzer,
        CancellationToken cancellationToken)
    {
        var compilation = await GetBindingCompilationAsync(project, cancellationToken).ConfigureAwait(false);
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);
        var sourceTrees = compilation.SyntaxTrees.ToHashSet();

        return diagnostics
            .Where(diagnostic =>
                diagnostic.Location == Location.None ||
                diagnostic.Location.IsInMetadata ||
                diagnostic.Location.SourceTree is { } sourceTree && sourceTrees.Contains(sourceTree))
            .OrderBy(diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToArray();
    }

    private static async Task<Compilation> GetBindingCompilationAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The Roslyn fixture compilation could not be created.");
        var errors = compilation.GetDiagnostics(cancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (errors.Length != 0)
        {
            throw new InvalidOperationException(
                $"The analyzer fixture compilation reported {errors.Length} error(s); analyzer results would be meaningless. " +
                $"First errors: {string.Join(" | ", errors.Take(5))}");
        }

        return compilation;
    }

    private static async Task ThrowOnCompilationErrorsAsync(
        Project project,
        CancellationToken cancellationToken) =>
        _ = await GetBindingCompilationAsync(project, cancellationToken).ConfigureAwait(false);
}
