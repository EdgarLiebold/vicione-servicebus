using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

public sealed partial class PublicDocumentationArchitectureTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> PartialTypeOwners = new(BuildPartialTypeOwners);

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLIC-DOCUMENTATION", "every-exposed-declaration-is-documented")]
    public void EveryExposedDeclaration_HasDocumentation()
    {
        string[] violations = ProductDeclarations()
            .Where(static declaration => Documentation(declaration.Node).Count == 0)
            .Select(Format)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(violations.Length == 0, FailureMessage("Exposed declarations without documentation", violations));
    }

    [Theory]
    [InlineData("public class Owner { private enum Kind { Value } }", false)]
    [InlineData("public enum Kind { Value }", true)]
    [InlineData("public class Owner { public enum Kind { Value } }", true)]
    [InlineData("internal class Owner { public enum Kind { Value } }", false)]
    [InlineData("public interface IOwner { enum Kind { Value } }", true)]
    [RequirementCoverage("REQ-VSB-PUBLIC-DOCUMENTATION", "enum-member-visibility-follows-containing-api")]
    public void EnumMemberVisibility_FollowsTheContainingEnumAndItsOwners(string source, bool expected)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(source,
            cancellationToken: TestContext.Current.CancellationToken).GetRoot(TestContext.Current.CancellationToken);
        EnumMemberDeclarationSyntax member = Assert.Single(root.DescendantNodes().OfType<EnumMemberDeclarationSyntax>());

        Assert.Equal(expected, IsExposedDeclaration(member));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLIC-DOCUMENTATION", "xml-elements-match-signatures-and-contain-text")]
    public void EveryExposedDeclaration_HasCompleteSignatureAccurateDocumentation()
    {
        var violations = new List<string>();

        foreach (SourceDeclaration declaration in ProductDeclarations())
        {
            IReadOnlyList<DocumentationCommentTriviaSyntax> documentation = Documentation(declaration.Node);
            if (documentation.Count == 0 || documentation.Any(HasInheritDoc))
                continue;

            IReadOnlyList<XmlElementSyntax> elements = documentation
                .SelectMany(static comment => comment.Content.OfType<XmlElementSyntax>())
                .ToArray();

            ValidateTextElement(declaration, elements, "summary", required: true, violations);
            ValidateTextElement(declaration, elements, "returns", ReturnsValue(declaration.Node), violations);
            ValidateNamedElements(declaration, elements, "typeparam", TypeParameterNames(declaration), violations);
            ValidateNamedElements(declaration, elements, "param", ParameterNames(declaration.Node), violations);
        }

        Assert.True(
            violations.Count == 0,
            FailureMessage("Public XML documentation does not match its declaration", violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLIC-DOCUMENTATION", "no-generated-placeholder-language")]
    public void EveryExposedDeclaration_UsesSpecificDocumentationInsteadOfGeneratedPlaceholders()
    {
        string[] violations = ProductDeclarations()
            .SelectMany(static declaration => Documentation(declaration.Node)
                .SelectMany(static comment => comment.Content.OfType<XmlElementSyntax>())
                .Select(element => (Declaration: declaration, Text: Normalize(element.Content.ToFullString()))))
            .Where(item => GeneratedPlaceholder().IsMatch(item.Text))
            .Select(item => $"{Format(item.Declaration)}: {item.Text}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            FailureMessage("Public XML documentation contains generated placeholder language", violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-COMMENTS", "all-documented-declarations-use-specific-language")]
    public void EveryDocumentedDeclaration_UsesSpecificDocumentationInsteadOfGeneratedPlaceholders()
    {
        string[] violations = DocumentedProductDeclarations()
            .SelectMany(static declaration => Documentation(declaration.Node)
                .SelectMany(static comment => comment.Content.OfType<XmlElementSyntax>())
                .Select(element => (Declaration: declaration, Text: Normalize(element.Content.ToFullString()))))
            .Where(item => GeneratedPlaceholder().IsMatch(item.Text))
            .Select(item => $"{Format(item.Declaration)}: {item.Text}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            FailureMessage("Product XML documentation contains generated placeholder language", violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-COMMENTS", "all-paired-xml-elements-contain-text")]
    public void EveryPairedDocumentationElement_ContainsText()
    {
        var violations = new List<string>();
        foreach (string path in ProductSourceFiles())
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                path: path,
                cancellationToken: TestContext.Current.CancellationToken);
            foreach (DocumentationCommentTriviaSyntax comment in tree.GetRoot(TestContext.Current.CancellationToken)
                         .DescendantTrivia(descendIntoTrivia: true)
                         .Where(static trivia => trivia.HasStructure)
                         .Select(static trivia => trivia.GetStructure())
                         .OfType<DocumentationCommentTriviaSyntax>())
            {
                foreach (XmlElementSyntax element in comment.DescendantNodes().OfType<XmlElementSyntax>())
                {
                    if (!string.IsNullOrWhiteSpace(Normalize(element.Content.ToFullString())))
                        continue;

                    int line = tree.GetLineSpan(element.Span, TestContext.Current.CancellationToken).StartLinePosition.Line + 1;
                    violations.Add($"{RepositoryLayout.RelativeToRoot(path)}:{line} (<{element.StartTag.Name.LocalName.ValueText}>)");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            FailureMessage("Paired XML documentation elements without descriptive text", violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-COMMENTS", "all-core-xml-elements-use-terminal-punctuation")]
    public void EveryDocumentedCoreElement_UsesTerminalPunctuation()
    {
        string[] violations = DocumentedProductDeclarations()
            .SelectMany(static declaration => Documentation(declaration.Node)
                .SelectMany(static comment => comment.Content.OfType<XmlElementSyntax>())
                .Where(static element => element.StartTag.Name.LocalName.ValueText is "summary" or "typeparam" or "param" or "returns")
                .Select(element => (Declaration: declaration, Element: element, Content: Normalize(element.Content.ToFullString()))))
            .Where(static item => !HasTerminalPunctuation(item.Content))
            .Select(item => $"{Format(item.Declaration)}: <{item.Element.StartTag.Name.LocalName.ValueText}> {item.Content}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            FailureMessage("Product XML documentation lacks terminal punctuation", violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLIC-DOCUMENTATION", "partial-type-documentation-has-one-canonical-owner")]
    public void PartialTypeDocumentation_IsOwnedByCanonicalFile()
    {
        PartialDeclaration[] declarations = ProductSourceFiles()
            .SelectMany(path =>
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(
                    File.ReadAllText(path),
                    path: path,
                    cancellationToken: TestContext.Current.CancellationToken);
                return tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Where(static type => type.Modifiers.Any(SyntaxKind.PartialKeyword))
                    .Where(IsExposedDeclaration)
                    .Select(type => new PartialDeclaration(
                        PartialTypeKey(type, path),
                        path,
                        tree.GetLineSpan(type.Span, TestContext.Current.CancellationToken).StartLinePosition.Line + 1,
                        type,
                        Documentation(type).Count));
            })
            .ToArray();
        string[] violations = declarations
            .Where(declaration => declaration.DocumentationCount != (IsCanonicalPartialDeclaration(declaration.Node, declaration.Path) ? 1 : 0))
            .Select(declaration => $"{RepositoryLayout.RelativeToRoot(declaration.Path)}:{declaration.LineNumber} "
                + $"({declaration.Node.Identifier.ValueText}; expected {(IsCanonicalPartialDeclaration(declaration.Node, declaration.Path) ? "one" : "no")} XML documentation block, found {declaration.DocumentationCount})")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            FailureMessage("Non-canonical partial declarations contain duplicate XML documentation", violations));
    }

    private static void ValidateTextElement(
        SourceDeclaration declaration,
        IReadOnlyList<XmlElementSyntax> elements,
        string elementName,
        bool required,
        ICollection<string> violations)
    {
        XmlElementSyntax[] matching = elements
            .Where(element => string.Equals(element.StartTag.Name.LocalName.ValueText, elementName, StringComparison.Ordinal))
            .ToArray();

        if (matching.Length != (required ? 1 : 0))
        {
            violations.Add($"{Format(declaration)}: expected {(required ? "one" : "no")} <{elementName}> element, found {matching.Length}");
            return;
        }

        if (required && string.IsNullOrWhiteSpace(Normalize(matching[0].Content.ToFullString())))
            violations.Add($"{Format(declaration)}: <{elementName}> is empty");
    }

    private static void ValidateNamedElements(
        SourceDeclaration declaration,
        IReadOnlyList<XmlElementSyntax> elements,
        string elementName,
        IReadOnlyList<string> expectedNames,
        ICollection<string> violations)
    {
        XmlElementSyntax[] matching = elements
            .Where(element => string.Equals(element.StartTag.Name.LocalName.ValueText, elementName, StringComparison.Ordinal))
            .ToArray();
        string[] actualNames = matching.Select(NameAttribute).ToArray();

        if (!expectedNames.SequenceEqual(actualNames, StringComparer.Ordinal))
        {
            violations.Add(
                $"{Format(declaration)}: <{elementName}> order [{string.Join(", ", actualNames)}] does not match signature [{string.Join(", ", expectedNames)}]");
        }

        foreach (XmlElementSyntax element in matching.Where(static element => string.IsNullOrWhiteSpace(Normalize(element.Content.ToFullString()))))
            violations.Add($"{Format(declaration)}: <{elementName} name=\"{NameAttribute(element)}\"> is empty");
    }

    private static string NameAttribute(XmlElementSyntax element) => element.StartTag.Attributes
        .OfType<XmlNameAttributeSyntax>()
        .Select(static attribute => attribute.Identifier.Identifier.ValueText)
        .SingleOrDefault() ?? "<missing>";

    private static bool HasInheritDoc(DocumentationCommentTriviaSyntax documentation) => documentation
        .DescendantNodes()
        .Any(static node => node switch
        {
            XmlEmptyElementSyntax empty => string.Equals(empty.Name.LocalName.ValueText, "inheritdoc", StringComparison.Ordinal),
            XmlElementSyntax element => string.Equals(element.StartTag.Name.LocalName.ValueText, "inheritdoc", StringComparison.Ordinal),
            _ => false,
        });

    private static IReadOnlyList<string> ParameterNames(SyntaxNode declaration) => declaration switch
    {
        BaseMethodDeclarationSyntax method => method.ParameterList.Parameters.Select(static parameter => parameter.Identifier.ValueText).ToArray(),
        DelegateDeclarationSyntax method => method.ParameterList.Parameters.Select(static parameter => parameter.Identifier.ValueText).ToArray(),
        IndexerDeclarationSyntax indexer => indexer.ParameterList.Parameters.Select(static parameter => parameter.Identifier.ValueText).ToArray(),
        RecordDeclarationSyntax record when record.ParameterList is not null => record.ParameterList.Parameters
            .Select(static parameter => parameter.Identifier.ValueText).ToArray(),
        ClassDeclarationSyntax @class when @class.ParameterList is not null => @class.ParameterList.Parameters
            .Select(static parameter => parameter.Identifier.ValueText).ToArray(),
        StructDeclarationSyntax @struct when @struct.ParameterList is not null => @struct.ParameterList.Parameters
            .Select(static parameter => parameter.Identifier.ValueText).ToArray(),
        _ => [],
    };

    private static IReadOnlyList<string> TypeParameterNames(SourceDeclaration declaration)
    {
        if (declaration.Node is TypeDeclarationSyntax partialType
            && partialType.Modifiers.Any(SyntaxKind.PartialKeyword)
            && !IsCanonicalPartialDeclaration(partialType, declaration.Path))
            return [];

        return declaration.Node switch
        {
            TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Select(static parameter => parameter.Identifier.ValueText).ToArray() ?? [],
            DelegateDeclarationSyntax method => method.TypeParameterList?.Parameters.Select(static parameter => parameter.Identifier.ValueText).ToArray() ?? [],
            MethodDeclarationSyntax method => method.TypeParameterList?.Parameters.Select(static parameter => parameter.Identifier.ValueText).ToArray() ?? [],
            _ => [],
        };
    }

    private static bool ReturnsValue(SyntaxNode declaration) => declaration switch
    {
        MethodDeclarationSyntax method => !string.Equals(method.ReturnType.ToString(), "void", StringComparison.Ordinal),
        OperatorDeclarationSyntax => true,
        ConversionOperatorDeclarationSyntax => true,
        DelegateDeclarationSyntax method => !string.Equals(method.ReturnType.ToString(), "void", StringComparison.Ordinal),
        _ => false,
    };

    private static IReadOnlyList<DocumentationCommentTriviaSyntax> Documentation(SyntaxNode declaration) => declaration
        .GetLeadingTrivia()
        .Where(static trivia => trivia.HasStructure)
        .Select(static trivia => trivia.GetStructure())
        .OfType<DocumentationCommentTriviaSyntax>()
        .ToArray();

    private static IEnumerable<SourceDeclaration> ProductDeclarations()
    {
        foreach (string path in ProductSourceFiles())
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                path: path,
                cancellationToken: TestContext.Current.CancellationToken);
            SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
            string relativePath = RepositoryLayout.RelativeToRoot(path);

            foreach (SyntaxNode node in root.DescendantNodes()
                         .Where(IsExposedDeclaration)
                         .Where(node => !IsNonCanonicalPartialDeclaration(node, path)))
                yield return new SourceDeclaration(
                    relativePath,
                    tree.GetLineSpan(node.Span, TestContext.Current.CancellationToken).StartLinePosition.Line + 1,
                    node);
        }
    }

    private static IEnumerable<SourceDeclaration> DocumentedProductDeclarations()
    {
        foreach (string path in ProductSourceFiles())
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                path: path,
                cancellationToken: TestContext.Current.CancellationToken);
            SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
            string relativePath = RepositoryLayout.RelativeToRoot(path);

            foreach (SyntaxNode node in root.DescendantNodes().Where(IsDocumentationDeclaration))
            {
                if (Documentation(node).Count == 0)
                    continue;

                yield return new SourceDeclaration(
                    relativePath,
                    tree.GetLineSpan(node.Span, TestContext.Current.CancellationToken).StartLinePosition.Line + 1,
                    node);
            }
        }
    }

    private static bool IsDocumentationDeclaration(SyntaxNode node) =>
        node is EnumMemberDeclarationSyntax
        || node is MemberDeclarationSyntax and not NamespaceDeclarationSyntax and not FileScopedNamespaceDeclarationSyntax;

    private static bool IsNonCanonicalPartialDeclaration(SyntaxNode declaration, string path) =>
        declaration is TypeDeclarationSyntax type
        && type.Modifiers.Any(SyntaxKind.PartialKeyword)
        && !IsCanonicalPartialDeclaration(type, path);

    private static bool IsCanonicalPartialDeclaration(TypeDeclarationSyntax declaration, string path)
    {
        string fullPath = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(RepositoryLayout.Root, path));
        return PartialTypeOwners.Value.TryGetValue(PartialTypeKey(declaration, fullPath), out string? owner)
            && string.Equals(owner, fullPath, StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<string, string> BuildPartialTypeOwners()
    {
        PartialDeclaration[] declarations = ProductSourceFiles()
            .SelectMany(path =>
            {
                string fullPath = Path.GetFullPath(path);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(
                    File.ReadAllText(fullPath),
                    path: fullPath,
                    cancellationToken: TestContext.Current.CancellationToken);
                return tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Where(static type => type.Modifiers.Any(SyntaxKind.PartialKeyword))
                    .Select(type => new PartialDeclaration(PartialTypeKey(type, fullPath), fullPath, 0, type, 0));
            })
            .ToArray();

        return declarations
            .GroupBy(static declaration => declaration.Key, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderByDescending(static declaration => string.Equals(
                        Path.GetFileNameWithoutExtension(declaration.Path),
                        declaration.Node.Identifier.ValueText,
                        StringComparison.Ordinal))
                    .ThenBy(static declaration => declaration.Path, StringComparer.Ordinal)
                    .First().Path,
                StringComparer.Ordinal);
    }

    private static string PartialTypeKey(TypeDeclarationSyntax declaration, string path)
    {
        string fullPath = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(RepositoryLayout.Root, path));
        string project = ProjectDirectory(fullPath);
        string namespaceName = string.Join(".", declaration.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .Select(static item => item.Name.ToString()));
        string containingTypes = string.Join("+", declaration.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .Reverse()
            .Select(static item => $"{item.Identifier.ValueText}`{item.TypeParameterList?.Parameters.Count ?? 0}"));
        string type = $"{declaration.Identifier.ValueText}`{declaration.TypeParameterList?.Parameters.Count ?? 0}";
        return $"{project}|{namespaceName}|{containingTypes}|{declaration.Kind()}|{type}";
    }

    private static string ProjectDirectory(string path)
    {
        for (DirectoryInfo? directory = Directory.GetParent(path); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
                return directory.FullName;
        }

        throw new InvalidOperationException($"No project could be resolved for '{path}'.");
    }

    private static bool IsExposedDeclaration(SyntaxNode node)
    {
        if (node is EnumMemberDeclarationSyntax)
            return node.Ancestors().OfType<EnumDeclarationSyntax>().FirstOrDefault() is { } enumeration && IsExposedDeclaration(enumeration);

        if (node is not MemberDeclarationSyntax member || node is NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax)
            return false;

        bool declaredVisible = member switch
        {
            BaseTypeDeclarationSyntax type => IsVisibleByModifiers(type.Modifiers, type.Parent),
            DelegateDeclarationSyntax method => IsVisibleByModifiers(method.Modifiers, method.Parent),
            BaseMethodDeclarationSyntax method => IsVisibleByModifiers(method.Modifiers, method.Parent),
            BasePropertyDeclarationSyntax property => IsVisibleByModifiers(property.Modifiers, property.Parent),
            BaseFieldDeclarationSyntax field => IsVisibleByModifiers(field.Modifiers, field.Parent),
            _ => false,
        };

        return declaredVisible && IsExternallyVisible(member);
    }

    private static bool IsExternallyVisible(SyntaxNode declaration) => declaration.Ancestors()
        .OfType<MemberDeclarationSyntax>()
        .Where(static ancestor => ancestor is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
        .All(ancestor => ancestor switch
        {
            BaseTypeDeclarationSyntax type => IsVisibleByModifiers(type.Modifiers, type.Parent),
            DelegateDeclarationSyntax method => IsVisibleByModifiers(method.Modifiers, method.Parent),
            _ => true,
        });

    private static bool IsVisibleByModifiers(SyntaxTokenList modifiers, SyntaxNode? parent)
    {
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
            return false;
        if (modifiers.Any(SyntaxKind.PublicKeyword) || modifiers.Any(SyntaxKind.ProtectedKeyword))
            return true;
        return parent is InterfaceDeclarationSyntax;
    }

    private static IEnumerable<string> ProductSourceFiles() => Directory
        .EnumerateFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.cs", SearchOption.AllDirectories)
        .Where(static path => !path.Contains(
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            RepositoryLayout.PathComparison))
        .Order(RepositoryLayout.PathComparer);

    private static string Format(SourceDeclaration declaration) =>
        $"{declaration.Path}:{declaration.LineNumber} ({declaration.Node.Kind()})";

    private static string FailureMessage(string title, IReadOnlyCollection<string> violations) =>
        $"{title}: {violations.Count} violation(s)." + Environment.NewLine
            + string.Join(Environment.NewLine, violations.Order(StringComparer.Ordinal).Take(100));

    private static string Normalize(string value)
    {
        string[] lines = value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(static line => DocumentationExterior().Replace(line, string.Empty).Trim())
            .ToArray();
        return Whitespace().Replace(string.Join(" ", lines), " ").Trim();
    }

    private static bool HasTerminalPunctuation(string content)
    {
        string plainText = Whitespace().Replace(XmlMarkup().Replace(content, " "), " ").Trim();
        return plainText.Length == 0 || plainText[^1] is '.' or '!' or '?' or ':' or ';';
    }

    [GeneratedRegex(@"^\s*///\s?", RegexOptions.CultureInvariant)]
    private static partial Regex DocumentationExterior();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex XmlMarkup();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();

    [GeneratedRegex(
        @"^(?:Provides support for .+\.|Provides (?:an?|the) .+ implementation\.|Defines the contract for .+\.|Represents (?:an?|the) .+ value\.|"
        + @"Defines the .+ value\.|(?:Performs|Executes) the .+ operation\.|Runs the .+ step\.|Handles .+ behavior\.|Coordinates .+ behavior\.|"
        + @"Provides operations for .+\.|Implements .+ for the service bus runtime\.|"
        + @"Gets(?: or sets)? the .+ associated with this instance\.|Creates the configured component\.|"
        + @"Represents the .+ service bus component\.|Provides the .+ capability\.|Provides .+ functionality\.|Implements [a-z0-9 -]+\.|"
        + @"The .+ supplied to the operation\.|The .+ processed by the operation\.|"
        + @"The result of .+\.|The .+ returned by the operation\.|A task that completes when .+ finishes\.|"
        + @"(?:Handles|Consumes) [a-z]\.|Determines whether the current value has [a-z]\.|"
        + @"The value processed by the member\.|Specifies the generic argument used for .+\.|"
        + @"The concrete implementation used for .+\.|The result of the operation\.|"
        + @"The operation context\.|Initializes a new instance of the containing type\.)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GeneratedPlaceholder();

    private sealed record SourceDeclaration(string Path, int LineNumber, SyntaxNode Node);
    private sealed record PartialDeclaration(string Key, string Path, int LineNumber, TypeDeclarationSyntax Node, int DocumentationCount);
}
