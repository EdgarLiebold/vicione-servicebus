using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

public sealed partial class SourceHygieneArchitectureTests
{
    private const string FastExpressionCompilerVersion = "5.4.1";

    private static readonly IReadOnlySet<string> FastExpressionCompilerOwners = new HashSet<string>(StringComparer.Ordinal)
    {
        "src/ViciOne.ServiceBus.MessagePack/ViciOne.ServiceBus.MessagePack.csproj",
        "src/ViciOne.ServiceBus.Sagas/ViciOne.ServiceBus.Sagas.csproj",
        "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj",
    };

    private static readonly IReadOnlySet<string> ReviewedCompilerDirectives =
        new HashSet<string>(StringComparer.Ordinal);

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-DIRECTIVES", "only-feature-preserving-compiler-directives")]
    public void ProductSources_ContainOnlyReviewedFeaturePreservingCompilerDirectives()
    {
        string[] actual = ProductSyntaxTrees()
            .SelectMany(static source => source.Root.DescendantTrivia(descendIntoTrivia: true)
                .Select(static trivia => trivia.GetStructure())
                .OfType<DirectiveTriviaSyntax>()
                .Select(directive => $"{source.Path}|{directive.ToFullString().Trim()}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ReviewedCompilerDirectives.Order(StringComparer.Ordinal), actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-COMMENTS", "no-maintenance-markers-or-ide-suppressions")]
    public void ProductComments_ContainNoMaintenanceMarkersOrIdeSuppressions()
    {
        string[] violations = ProductComments()
            .Where(comment => MaintenanceMarker().IsMatch(comment.Text))
            .Select(Format)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Product comments contain maintenance markers or IDE-specific suppressions:"
                + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-COMMENTS", "no-historical-or-speculative-narrative")]
    public void ProductComments_DescribeOnlyCurrentCodeAndBehavior()
    {
        string[] violations = ProductComments()
            .Where(comment => HistoricalOrSpeculativeNarrative().IsMatch(comment.Text))
            .Select(Format)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Product comments contain historical, speculative, or implementation-process narrative:"
                + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-COMMENTS", "no-semantically-invalid-api-contracts")]
    public void ProductDocumentation_ContainsNoKnownSemanticallyInvalidContracts()
    {
        string[] violations = ProductComments()
            .Where(comment => SemanticallyInvalidApiContract().IsMatch(comment.Text))
            .Select(Format)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Product documentation contains a known semantically invalid contract:"
                + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-COMMENTS", "provider-documentation-uses-provider-native-vocabulary")]
    public void ProviderDocumentation_UsesProviderNativeVocabulary()
    {
        ProviderVocabularyRule[] rules =
        [
            new("src/Transports/ViciOne.ServiceBus.ActiveMq/", ActiveMqForeignVocabulary(), "ActiveMQ topic and selector vocabulary"),
            new("src/Transports/ViciOne.ServiceBus.AmazonSqs/", AmazonSqsForeignVocabulary(), "Amazon SNS/SQS topic, subscription, and queue vocabulary"),
            new("src/Transports/ViciOne.ServiceBus.AzureServiceBus/", AzureServiceBusForeignVocabulary(), "Azure topic, subscription, rule, and queue vocabulary"),
            new("src/Transports/ViciOne.ServiceBus.SqlTransport/", SqlTransportForeignVocabulary(), "SQL topic, subscription, routing-key, and queue vocabulary"),
        ];

        string[] violations = ProductComments()
            .Select(comment => (Comment: comment, Text: XmlMarkup().Replace(comment.Text, " ")))
            .SelectMany(item => rules
                .Where(rule => item.Comment.Path.StartsWith(rule.PathPrefix, StringComparison.Ordinal)
                    && rule.ForeignVocabulary.IsMatch(item.Text))
                .Select(rule => $"{Format(item.Comment)} (expected {rule.ExpectedVocabulary})"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Provider documentation contains foreign transport vocabulary:"
                + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEPENDENCY-OWNERSHIP", "expression-compiler-is-current-package-owned")]
    public void ExpressionCompiler_IsCurrentPackageOwnedAndNeverEmbedded()
    {
        string embeddedCompiler = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus",
            "Internals",
            "Reflection",
            "ExpressionCompiler.cs");
        Assert.False(File.Exists(embeddedCompiler), $"Embedded compiler source is forbidden: {embeddedCompiler}");

        XDocument centralPackages = XDocument.Load(Path.Combine(RepositoryLayout.Root, "Directory.Packages.props"));
        XElement packageVersion = Assert.Single(
            centralPackages.Descendants("PackageVersion"),
            element => string.Equals((string?)element.Attribute("Include"), "FastExpressionCompiler", StringComparison.Ordinal));
        Assert.Equal(FastExpressionCompilerVersion, (string?)packageVersion.Attribute("Version"));

        string[] actualOwners = Directory.EnumerateFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(project => XDocument.Load(project).Descendants("PackageReference")
                .Any(element => string.Equals((string?)element.Attribute("Include"), "FastExpressionCompiler", StringComparison.Ordinal)))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(FastExpressionCompilerOwners.Order(StringComparer.Ordinal), actualOwners);

        string[] missingImports = ProductSourceFiles()
            .Where(path => File.ReadAllText(path).Contains(".CompileFast", StringComparison.Ordinal))
            .Where(path => !File.ReadAllText(path).Contains("using FastExpressionCompiler;", StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(missingImports);
    }

    private static IEnumerable<SourceComment> ProductComments() => ProductSyntaxTrees()
        .SelectMany(static source => source.Root.DescendantTrivia(descendIntoTrivia: true)
            .Where(static trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .Select(trivia => new SourceComment(
                source.Path,
                trivia.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                trivia.ToFullString())));

    private static IEnumerable<SourceSyntax> ProductSyntaxTrees()
    {
        foreach (string path in ProductSourceFiles())
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot();
            yield return new SourceSyntax(RepositoryLayout.RelativeToRoot(path), root);
        }
    }

    private static IEnumerable<string> ProductSourceFiles()
    {
        string sourceRoot = Path.Combine(RepositoryLayout.Root, "src");
        return Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(
                         $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                         RepositoryLayout.PathComparison))
            .Order(RepositoryLayout.PathComparer);
    }

    private static string Format(SourceComment comment) =>
        $"{comment.Path}:{comment.LineNumber}: {comment.Text.Trim()}";

    [GeneratedRegex(@"\b(?:TODO|FIXME|HACK)\b|ReSharper", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MaintenanceMarker();

    [GeneratedRegex(
        @"this was disabled previously|not sure if|someday|haven't tested|later,\s*we'll|CreateAgent transfers|"
        + @"not currently used|make it ""cute""|obsoletes? the previous|used to sit here|summary that said so|"
        + @"a timer was disposed|auto-property would have been",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HistoricalOrSpeculativeNarrative();

    [GeneratedRegex(
        @"The task which is completed once the (?:Send|Publish) is acknowledged by the broker|"
        + @"The time at which the message should be delivered to the queue|"
        + @"AWS SQS minimum for ChangeMessageVisibility|per AWS SQS API constraints|"
        + @"mostly unused now|not used apparently|shutting it down for good|pushed from the broker|"
        + @"was has been|was already has been|same was Events|(?-i:\bTHe\b)|\boverriden\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SemanticallyInvalidApiContract();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex XmlMarkup();

    [GeneratedRegex(@"\b(?:exchange|routing key)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ActiveMqForeignVocabulary();

    [GeneratedRegex(@"\b(?:exchange|routing key|virtual host|broker restart)\b|pushed from the broker", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmazonSqsForeignVocabulary();

    [GeneratedRegex(@"\b(?:exchange|routing key|RabbitMQ|Amazon ?SQS|SQS)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AzureServiceBusForeignVocabulary();

    [GeneratedRegex(@"\b(?:exchange|RabbitMQ|Amazon ?SQS|SQS)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlTransportForeignVocabulary();

    private sealed record SourceComment(string Path, int LineNumber, string Text);

    private sealed record SourceSyntax(string Path, SyntaxNode Root);

    private sealed record ProviderVocabularyRule(string PathPrefix, Regex ForeignVocabulary, string ExpectedVocabulary);
}
