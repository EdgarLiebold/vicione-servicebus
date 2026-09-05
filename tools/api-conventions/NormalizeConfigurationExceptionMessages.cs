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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

if (args.Length != 3 || args[0] is not ("analyze" or "apply"))
{
    Console.Error.WriteLine(
        "Usage: dotnet run tools/api-conventions/NormalizeConfigurationExceptionMessages.cs -- <analyze|apply> <solution.slnx> <report.json>");
    return 2;
}

string mode = args[0];
string solutionPath = Path.GetFullPath(args[1]);
string reportPath = Path.GetFullPath(args[2]);
string repositoryRoot = FindRepositoryRoot(solutionPath);

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
{
    ["Configuration"] = "Release",
    ["ViciOneNativeTestTree"] = "true",
});

var diagnostics = new List<string>();
workspace.RegisterWorkspaceFailedHandler(eventArgs =>
    diagnostics.Add($"{eventArgs.Diagnostic.Kind}: {eventArgs.Diagnostic.Message}"));

Solution solution = await workspace.OpenSolutionAsync(solutionPath);
var changes = new List<Change>();
var unresolved = new List<string>();
var seenPaths = new HashSet<string>(StringComparer.Ordinal);
int creationCount = 0;
int compliantCount = 0;

foreach (Project project in solution.Projects.Where(project => IsProductProject(project, repositoryRoot)))
{
    Compilation compilation = await project.GetCompilationAsync()
        ?? throw new InvalidOperationException($"Unable to compile '{project.Name}'.");

    foreach (Document originalDocument in project.Documents.Where(static document => document.FilePath is not null))
    {
        string path = originalDocument.FilePath!;
        if (!seenPaths.Add(path))
            continue;

        Document document = solution.GetDocument(originalDocument.Id) ?? originalDocument;
        SyntaxNode root = await document.GetSyntaxRootAsync()
            ?? throw new InvalidOperationException($"Unable to parse '{path}'.");
        SemanticModel semanticModel = compilation.GetSemanticModel(root.SyntaxTree);
        var replacements = new Dictionary<ExpressionSyntax, ExpressionSyntax>();

        foreach (ObjectCreationExpressionSyntax creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            IMethodSymbol? constructor = semanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            if (constructor is null || !IsConfigurationException(constructor.ContainingType))
            {
                if (creation.Type.ToString().EndsWith("ConfigurationException", StringComparison.Ordinal)
                    && constructor is null)
                    unresolved.Add(Relative(repositoryRoot, path, creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
                continue;
            }

            creationCount++;

            IParameterSymbol? messageParameter = constructor.Parameters.SingleOrDefault(
                static parameter => parameter.Name == "message");
            if (messageParameter is null || creation.ArgumentList is null)
                continue;

            ArgumentSyntax? messageArgument = creation.ArgumentList.Arguments.FirstOrDefault(argument =>
                    argument.NameColon?.Name.Identifier.ValueText == "message")
                ?? (messageParameter.Ordinal < creation.ArgumentList.Arguments.Count
                    ? creation.ArgumentList.Arguments[messageParameter.Ordinal]
                    : null);
            if (messageArgument is null)
                continue;
            if (IsCompliant(messageArgument.Expression, semanticModel))
            {
                compliantCount++;
                continue;
            }

            string feature = FeatureName(path, creation);
            string replacementText =
                $"global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create({SymbolDisplay.FormatLiteral(feature, true)}, \"unknown\", {messageArgument.Expression.WithoutTrivia()}, \"Correct the named configuration before starting the host\")";
            ExpressionSyntax replacement = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(messageArgument.Expression);
            replacements.Add(messageArgument.Expression, replacement);
            changes.Add(new Change(
                Relative(repositoryRoot, path, creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1),
                feature,
                messageArgument.Expression.ToString()));
        }

        if (mode == "apply" && replacements.Count > 0)
        {
            SyntaxNode changedRoot = root.ReplaceNodes(
                replacements.Keys,
                (original, _) => replacements[(ExpressionSyntax)original]);
            solution = solution.WithDocumentSyntaxRoot(document.Id, changedRoot);
        }
    }
}

if (mode == "apply" && !workspace.TryApplyChanges(solution))
    throw new InvalidOperationException("MSBuildWorkspace rejected the configuration-message changes.");

Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new
{
    mode,
    creationCount,
    compliantCount,
    changeCount = changes.Count,
    unresolvedCount = unresolved.Count,
    changes,
    unresolved,
    workspaceDiagnostics = diagnostics,
}, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"ConfigurationException messages: {changes.Count} change(s), {unresolved.Count} unresolved creation(s).");
return unresolved.Count == 0 ? 0 : 1;

static bool IsProductProject(Project project, string repositoryRoot)
{
    if (project.FilePath is null)
        return false;
    string relative = Path.GetRelativePath(repositoryRoot, project.FilePath);
    return relative.StartsWith($"src{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        && !relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(static segment => segment is "obj" or "bin" or "artifacts" or "review");
}

static bool IsConfigurationException(INamedTypeSymbol type)
{
    INamedTypeSymbol? current = type;
    while (current is not null)
    {
        if (current.ToDisplayString() == "ViciOne.ServiceBus.ConfigurationException")
            return true;
        current = current.BaseType;
    }
    return false;
}

static bool IsCompliant(ExpressionSyntax expression, SemanticModel semanticModel)
{
    string source = expression.ToString();
    bool usesFactory = expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(invocation =>
        invocation.Expression.ToString().Contains("ConfigurationMessages.", StringComparison.Ordinal));
    if (usesFactory
        || source.Contains(" for bus '", StringComparison.Ordinal)
            && source.Contains("':", StringComparison.Ordinal))
        return true;

    Optional<object?> constant = semanticModel.GetConstantValue(expression);
    return constant.HasValue
        && constant.Value is string message
        && Regex.IsMatch(
            message,
            @"^[^\r\n]+ for bus '[^']+': .+\. .+\.$",
            RegexOptions.CultureInvariant);
}

static string FeatureName(string path, SyntaxNode node)
{
    string normalized = path.Replace('\\', '/');
    (string Token, string Feature)[] mappings =
    [
        ("RabbitMq", "RabbitMQ"),
        ("AzureServiceBus", "Azure Service Bus"),
        ("AmazonSqs", "Amazon SQS"),
        ("ActiveMq", "ActiveMQ"),
        ("SqlTransport", "SQL transport"),
        ("DurableSend", "Reliable messaging"),
        ("MessageData", "Message data"),
        ("MessageJournal", "Message journal"),
        ("JobService", "Job service"),
        ("Saga", "Saga"),
        ("Mediator", "Mediator"),
        ("Serialization", "Serialization"),
        ("ReceiveEndpoint", "Receive endpoint"),
        ("Testing", "Test harness"),
    ];
    foreach ((string token, string feature) in mappings)
    {
        if (normalized.Contains(token, StringComparison.Ordinal))
            return feature;
    }

    string typeName = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText
        ?? Path.GetFileNameWithoutExtension(path);
    typeName = Regex.Replace(
        typeName,
        "(?:ConfigurationExtensions|Configuration|Configurator|Specification|Options|Factory|Builder)$",
        string.Empty,
        RegexOptions.CultureInvariant);
    string words = Regex.Replace(typeName, "(?<=[a-z0-9])(?=[A-Z])", " ", RegexOptions.CultureInvariant).Trim();
    return string.IsNullOrWhiteSpace(words) ? "Configuration" : words;
}

static string FindRepositoryRoot(string path)
{
    DirectoryInfo? directory = new FileInfo(path).Directory;
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViciOne.ServiceBus.slnx")))
        directory = directory.Parent;
    return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
}

static string Relative(string root, string path, int line)
    => $"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{line}";

internal sealed record Change(string Location, string Feature, string OriginalExpression);
