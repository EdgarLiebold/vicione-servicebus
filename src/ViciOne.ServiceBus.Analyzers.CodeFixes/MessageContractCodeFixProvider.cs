using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using ViciOne.ServiceBus.Analyzers;
using ViciOne.ServiceBus.Analyzers.Internals;

namespace ViciOne.ServiceBus.Analyzers.CodeFixes;

/// <summary>Adds omitted members to anonymous message values from their declared contracts.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessageContractCodeFixProvider))]
[Shared]
public sealed class MessageContractCodeFixProvider :
    CodeFixProvider
{
    const string Title = "Add missing properties";

    /// <summary>Gets the missing-message-property diagnostic fixed by this provider.</summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => [MessageContractAnalyzer.MissingPropertiesRuleId];

    /// <summary>Gets the batch provider used to add missing properties across a solution.</summary>
    /// <returns>The standard batch fix-all provider.</returns>
    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <summary>Registers a fix for the anonymous object identified by the diagnostic.</summary>
    /// <param name="context">The code-fix registration context.</param>
    /// <returns>A task that completes after registration.</returns>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var anonymousObject = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<AnonymousObjectCreationExpressionSyntax>().FirstOrDefault();
        if (anonymousObject == null)
            return;

        if (!diagnostic.Properties.TryGetValue("messageContractType", out var fullType)
            || fullType == null
            || string.IsNullOrWhiteSpace(fullType))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => AddMissingPropertiesAsync(context.Document, anonymousObject, fullType, cancellationToken),
                Title),
            diagnostic);
    }

    static async Task<Document> AddMissingPropertiesAsync(Document document,
        AnonymousObjectCreationExpressionSyntax anonymousObject,
        string fullType,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || semanticModel == null)
            return document;

        var symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        var symbols = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Select(declaration => semanticModel.GetDeclaredSymbol(declaration, cancellationToken))
            .ToList();
        ITypeSymbol? contractType = symbols.FirstOrDefault(i => i?.ToDisplayString(symbolDisplayFormat) == fullType);
        contractType ??= semanticModel.Compilation.GetTypeByMetadataName(fullType);

        if (contractType != null)
        {
            var dictionary = new Dictionary<AnonymousObjectCreationExpressionSyntax, ITypeSymbol>();

            await FindAnonymousTypesWithMessageContractsInTreeAsync(
                    dictionary,
                    anonymousObject,
                    contractType,
                    semanticModel,
                    cancellationToken)
                .ConfigureAwait(false);

            var newRoot = AddMissingProperties(root, dictionary);

            var formattedRoot = Formatter.Format(newRoot, Formatter.Annotation, document.Project.Solution.Workspace,
                document.Project.Solution.Workspace.Options, cancellationToken: cancellationToken);

            return document.WithSyntaxRoot(formattedRoot);
        }

        return document;
    }

    static async Task FindAnonymousTypesWithMessageContractsInTreeAsync(
        IDictionary<AnonymousObjectCreationExpressionSyntax, ITypeSymbol> dictionary,
        AnonymousObjectCreationExpressionSyntax anonymousObject,
        ITypeSymbol contractType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        List<IPropertySymbol> contractProperties = contractType.GetSerializableProperties();

        foreach (var initializer in anonymousObject.Initializers)
        {
            var name = GetName(initializer);
            if (name == null)
                continue;

            var contractProperty = contractProperties.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (contractProperty != null)
            {
                await FindAnonymousTypesWithMessageContractsInTreeAsync(
                        dictionary,
                        initializer,
                        contractProperty,
                        semanticModel,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        dictionary.Add(anonymousObject, contractType);
    }

    static async Task FindAnonymousTypesWithMessageContractsInTreeAsync(
        IDictionary<AnonymousObjectCreationExpressionSyntax, ITypeSymbol> dictionary,
        AnonymousObjectMemberDeclaratorSyntax initializer,
        IPropertySymbol contractProperty,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (initializer.Expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreationExpressionSyntax)
        {
            if (contractProperty.Type.IsImmutableArray(out var contractElementType)
                || contractProperty.Type.IsList(out contractElementType)
                || contractProperty.Type.IsArray(out contractElementType)
                || contractProperty.Type.IsCollection(out contractElementType)
                || contractProperty.Type.IsEnumerable(out contractElementType))
            {
                await FindAnonymousTypesWithMessageContractsInTreeAsync(
                        dictionary,
                        implicitArrayCreationExpressionSyntax,
                        contractElementType,
                        semanticModel,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        else if (initializer.Expression is AnonymousObjectCreationExpressionSyntax anonymousObjectProperty)
        {
            await FindAnonymousTypesWithMessageContractsInTreeAsync(
                    dictionary,
                    anonymousObjectProperty,
                    contractProperty.Type,
                    semanticModel,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (initializer.Expression is InvocationExpressionSyntax invocationExpressionSyntax
                 && semanticModel.GetSymbolInfo(invocationExpressionSyntax, cancellationToken).Symbol is IMethodSymbol method
                 && method.ReturnType.IsList(out var methodReturnTypeArgument)
                 && methodReturnTypeArgument.IsAnonymousType)
        {
            if (contractProperty.Type.IsImmutableArray(out var contractElementType) ||
                contractProperty.Type.IsList(out contractElementType) ||
                contractProperty.Type.IsArray(out contractElementType) ||
                contractProperty.Type.IsCollection(out contractElementType) ||
                contractProperty.Type.IsEnumerable(out contractElementType))
            {
                await FindAnonymousTypesWithMessageContractsInTreeAsync(
                        dictionary,
                        methodReturnTypeArgument,
                        contractElementType,
                        semanticModel,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    static async Task FindAnonymousTypesWithMessageContractsInTreeAsync(
        IDictionary<AnonymousObjectCreationExpressionSyntax, ITypeSymbol> dictionary,
        ImplicitArrayCreationExpressionSyntax implicitArrayCreationExpressionSyntax,
        ITypeSymbol contractElementType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        SeparatedSyntaxList<ExpressionSyntax> expressions = implicitArrayCreationExpressionSyntax.Initializer.Expressions;
        foreach (var expression in expressions)
        {
            if (expression is AnonymousObjectCreationExpressionSyntax anonymousObjectArrayInitializer)
            {
                await FindAnonymousTypesWithMessageContractsInTreeAsync(dictionary, anonymousObjectArrayInitializer,
                    contractElementType, semanticModel, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    static async Task FindAnonymousTypesWithMessageContractsInTreeAsync(
        IDictionary<AnonymousObjectCreationExpressionSyntax, ITypeSymbol> dictionary,
        ITypeSymbol methodReturnTypeArgument,
        ITypeSymbol contractElementType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var syntaxReference = methodReturnTypeArgument.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference == null)
            return;

        var syntax = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
        if (syntax is AnonymousObjectCreationExpressionSyntax anonymousObjectTypeArgument)
        {
            await FindAnonymousTypesWithMessageContractsInTreeAsync(dictionary, anonymousObjectTypeArgument, contractElementType,
                semanticModel, cancellationToken).ConfigureAwait(false);
        }
    }

    static string? GetName(AnonymousObjectMemberDeclaratorSyntax initializer)
    {
        if (initializer.NameEquals != null)
            return initializer.NameEquals.Name.Identifier.ValueText;

        return initializer.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => null,
        };
    }

    static SyntaxNode AddMissingProperties(SyntaxNode root, IDictionary<AnonymousObjectCreationExpressionSyntax, ITypeSymbol> dictionary)
    {
        var newRoot = root.TrackNodes(dictionary.Keys);

        foreach (KeyValuePair<AnonymousObjectCreationExpressionSyntax, ITypeSymbol> keyValuePair in dictionary)
        {
            var anonymousObject = newRoot.GetCurrentNode(keyValuePair.Key);
            if (anonymousObject == null)
                continue;

            var contractType = keyValuePair.Value;
            newRoot = AddMissingProperties(newRoot, anonymousObject, contractType);
        }

        return newRoot;
    }

    static SyntaxNode AddMissingProperties(SyntaxNode root, AnonymousObjectCreationExpressionSyntax anonymousObject, ITypeSymbol contractType)
    {
        var newRoot = root;

        List<IPropertySymbol> contractProperties = contractType.GetSerializableProperties();

        var propertiesToAdd = new List<AnonymousObjectMemberDeclaratorSyntax>();
        foreach (var messageContractProperty in contractProperties)
        {
            var initializer = anonymousObject.Initializers
                .FirstOrDefault(i => string.Equals(GetName(i), messageContractProperty.Name, StringComparison.OrdinalIgnoreCase));
            if (initializer == null)
            {
                var path = Enumerable.Empty<ITypeSymbol>();
                var propertyToAdd = CreateProperty(messageContractProperty, path);
                propertiesToAdd.Add(propertyToAdd);
            }
        }

        if (propertiesToAdd.Any())
        {
            var newAnonymousObject = anonymousObject
                .AddInitializers([.. propertiesToAdd])
                .WithAdditionalAnnotations(Formatter.Annotation);
            newRoot = newRoot.ReplaceNode(anonymousObject, newAnonymousObject);
        }

        return newRoot;
    }

    static AnonymousObjectMemberDeclaratorSyntax[] CreateProperties(ITypeSymbol contractType, IEnumerable<ITypeSymbol> path)
    {
        List<IPropertySymbol> contractProperties = contractType.GetSerializableProperties();

        var propertiesToAdd = new List<AnonymousObjectMemberDeclaratorSyntax>();
        foreach (var contractProperty in contractProperties)
        {
            var propertyToAdd = CreateProperty(contractProperty, path);
            propertiesToAdd.Add(propertyToAdd);
        }

        return [.. propertiesToAdd];
    }

    static AnonymousObjectMemberDeclaratorSyntax CreateProperty(IPropertySymbol contractProperty, IEnumerable<ITypeSymbol> path)
    {
        ExpressionSyntax expression;

        if (contractProperty.Type.IsImmutableArray(out var contractElementType) ||
            contractProperty.Type.IsList(out contractElementType) ||
            contractProperty.Type.IsArray(out contractElementType) ||
            contractProperty.Type.IsCollection(out contractElementType) ||
            contractProperty.Type.IsEnumerable(out contractElementType))
        {
            if (path.Contains(contractElementType, SymbolEqualityComparer.Default))
                expression = CreateEmptyArray(contractElementType);
            else
                expression = CreateImplicitArray(contractElementType, path.Concat([contractElementType]));
        }
        else if (contractProperty.Type.TypeKind == TypeKind.Interface)
        {
            if (path.Contains(contractProperty.Type, SymbolEqualityComparer.Default))
                expression = CreateDefault(contractProperty.Type);
            else
                expression = CreateAnonymousObject(contractProperty.Type, path.Concat([contractProperty.Type]));
        }
        else
            expression = CreateDefault(contractProperty.Type);

        return SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.NameEquals(contractProperty.Name), expression)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    static ExpressionSyntax CreateEmptyArray(ITypeSymbol type)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Array"),
                    SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("Empty"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    CreateTypeSyntax(type))))))
            .NormalizeWhitespace();
    }

    static ImplicitArrayCreationExpressionSyntax CreateImplicitArray(ITypeSymbol type, IEnumerable<ITypeSymbol> path)
    {
        ExpressionSyntax node;
        if (type.TypeKind == TypeKind.Interface)
            node = CreateAnonymousObject(type, path);
        else
            node = CreateDefault(type);

        ExpressionSyntax[] nodes = [node];
        var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
            .WithExpressions(SyntaxFactory.SeparatedList(nodes));
        return SyntaxFactory.ImplicitArrayCreationExpression(initializer)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    static AnonymousObjectCreationExpressionSyntax CreateAnonymousObject(ITypeSymbol type, IEnumerable<ITypeSymbol> path)
    {
        AnonymousObjectMemberDeclaratorSyntax[] propertiesToAdd = CreateProperties(type, path);
        return SyntaxFactory.AnonymousObjectCreationExpression()
            .WithInitializers(SyntaxFactory.SeparatedList(propertiesToAdd))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    static DefaultExpressionSyntax CreateDefault(ITypeSymbol type)
    {
        return SyntaxFactory.DefaultExpression(CreateTypeSyntax(type));
    }

    static TypeSyntax CreateTypeSyntax(ITypeSymbol type)
    {
        return SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .WithAdditionalAnnotations(Simplifier.Annotation);
    }
}
