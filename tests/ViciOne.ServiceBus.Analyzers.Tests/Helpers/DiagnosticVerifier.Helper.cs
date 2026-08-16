namespace ViciOne.ServiceBus.Analyzers.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;


    /// <summary>
    /// Class for turning strings into documents and getting the diagnostics on them
    /// All methods are static
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        /// <summary>
        /// The full shared framework reference set of the runtime the tests execute on.
        /// A hand-picked subset silently omitted facades such as System.Runtime and System.ComponentModel, which made every
        /// test snippet fail to bind with CS0012 and therefore made every analyzer report zero diagnostics.
        /// </summary>
        static readonly MetadataReference[] FrameworkReferences = CreateFrameworkReferences();

        static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        static readonly MetadataReference ViciOneServiceBusReference = MetadataReference.CreateFromFile(typeof(Bus).Assembly.Location);
        static readonly MetadataReference GreenPipesReference = MetadataReference.CreateFromFile(typeof(IProbeSite).Assembly.Location);
        static readonly MetadataReference NewIdReference = MetadataReference.CreateFromFile(typeof(NewId).Assembly.Location);

        static MetadataReference[] CreateFrameworkReferences()
        {
            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (string.IsNullOrEmpty(runtimeDirectory))
                throw new InvalidOperationException("The shared framework directory could not be resolved for the analyzer test compilation.");

            var references = new List<MetadataReference>();
            foreach (var path in Directory.EnumerateFiles(runtimeDirectory, "*.dll"))
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
                catch (BadImageFormatException)
                {
                    // Native libraries share the directory on some platforms and carry no metadata.
                }
            }

            if (references.Count == 0)
                throw new InvalidOperationException($"No framework references were resolved from '{runtimeDirectory}'.");

            return references.ToArray();
        }

        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string TestProjectName = "TestProject";

        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source classes are in</param>
        /// <param name="analyzer">The analyzer to be run on the sources</param>
        /// <returns>
        /// An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location
        /// </returns>
        static Diagnostic[] GetSortedDiagnostics(string[] sources, string language, DiagnosticAnalyzer analyzer)
        {
            return GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(sources, language));
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>
        /// An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location
        /// </returns>
        protected static Diagnostic[] GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
                projects.Add(document.Project);

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = project.GetCompilationAsync().Result;

                // A test snippet that does not bind produces zero analyzer diagnostics, which would silently look
                // like "the analyzer found nothing". Surface reference and compilation defects instead of hiding them.
                var compilationErrors = compilation.GetDiagnostics()
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .ToArray();
                if (compilationErrors.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"The analyzer test compilation reported {compilationErrors.Length} error(s); analyzer results would be meaningless. "
                        + "First errors: "
                        + string.Join(" | ", compilationErrors.Take(5).Select(diagnostic => diagnostic.ToString())));
                }

                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
                ImmutableArray<Diagnostic> diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                        diagnostics.Add(diag);
                    else
                    {
                        for (var i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = document.GetSyntaxTreeAsync().Result;
                            if (tree == diag.Location.SourceTree)
                                diagnostics.Add(diag);
                        }
                    }
                }
            }

            Diagnostic[] results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>
        /// A Tuple containing the Documents produced from the sources and their TextSpans if relevant
        /// </returns>
        static Document[] GetDocuments(string[] sources, string language, bool includeViciOneServiceBus = true)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
                throw new ArgumentException("Unsupported Language");

            var project = CreateProject(sources, language, includeViciOneServiceBus);
            Document[] documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
                throw new InvalidOperationException("Amount of sources did not match amount of Documents created");

            return documents;
        }

        /// <summary>
        /// Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp)
        {
            return CreateProject(new[] {source}, language).Documents.First();
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <param name="includeViciOneServiceBus">Whether the resulting Project has a dependency on ViciOne.ServiceBus or not</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        static Project CreateProject(string[] sources, string language = LanguageNames.CSharp, bool includeViciOneServiceBus = true)
        {
            var fileNamePrefix = DefaultFilePathPrefix;
            var fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;

            var projectId = ProjectId.CreateNewId(TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .AddMetadataReferences(projectId, FrameworkReferences)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference);

            // Analyzer fixtures are type declarations, not programs. The workspace default is a console application,
            // which fails every fixture with CS5001 before any analyzer runs.
            var compilationOptions = solution.GetProject(projectId).CompilationOptions;
            if (compilationOptions != null)
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            if (includeViciOneServiceBus)
            {
                solution = solution.AddMetadataReference(projectId, ViciOneServiceBusReference)
                    .AddMetadataReference(projectId, GreenPipesReference)
                    .AddMetadataReference(projectId, NewIdReference);
            }

            var count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            return solution.GetProject(projectId);
        }
    }
}
