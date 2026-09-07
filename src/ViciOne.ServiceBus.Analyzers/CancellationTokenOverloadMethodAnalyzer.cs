using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ViciOne.ServiceBus.Analyzers.Internals;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>Reports cancellable calls that omit an available pipeline cancellation token.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CancellationTokenOverloadMethodAnalyzer :
    DiagnosticAnalyzer
{
    /// <summary>Identifies diagnostics for calls that can forward a pipeline cancellation token.</summary>
    public const string CancellationTokenOverloadMethodRuleId = "VOSB2001";

    // Diagnostic property keys. The code fix reads them from its own assembly, so they are part of
    // the contract between the two.
    /// <summary>Names the diagnostic property containing the cancellation-token parameter index.</summary>
    public const string ParameterIndex = "ParameterIndex";
    /// <summary>Names the diagnostic property containing the cancellation-token parameter name.</summary>
    public const string ParameterName = "ParameterName";
    /// <summary>Names the diagnostic property containing the available cancellation-token expressions.</summary>
    public const string CancellationTokens = "CancellationTokens";

    const string Category = "Reliability";

    static readonly DiagnosticDescriptor CancellationTokenOverloadMethodRule = new DiagnosticDescriptor(CancellationTokenOverloadMethodRuleId,
        "Context.CancellationToken could be used in method overload with CancellationToken",
        "Cancellation token from '{0}' can be used in cancellation token overload for '{1}' method",
        Category, DiagnosticSeverity.Info, true,
        "Context.CancellationToken can be passed in method with overload.");

    /// <summary>Gets the cancellation-forwarding diagnostic supported by this analyzer.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CancellationTokenOverloadMethodRule);

    /// <summary>Registers invocation analysis for each compilation.</summary>
    /// <param name="context">The analyzer registration context.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var cancellationTokenSymbol = GetBestTypeByMetadataName(context.Compilation, "System.Threading.CancellationToken");
        var pipeContextTypeSymbol = GetBestTypeByMetadataName(context.Compilation, "ViciOne.ServiceBus.Advanced.PipeContext");
        var cancellationTokenSourceSymbol = GetBestTypeByMetadataName(context.Compilation, "System.Threading.CancellationTokenSource");
        if (cancellationTokenSymbol == null || pipeContextTypeSymbol == null)
            return;

        var consumeContextTypeSymbol = GetBestTypeByMetadataName(context.Compilation, "ViciOne.ServiceBus.ConsumeContext");
        var outgoingMessagesTypeSymbol = GetBestTypeByMetadataName(context.Compilation, "ViciOne.ServiceBus.IOutgoingMessages");

        // ISymbol instances belong to one compilation, so the member cache shares that exact lifetime.
        var membersByType = new ConcurrentDictionary<ISymbol, IEnumerable<ISymbol>>(SymbolEqualityComparer.Default);

        context.RegisterOperationAction(analysisContext =>
        {
            var invocation = (IInvocationOperation)analysisContext.Operation;

            if (!(analysisContext.ContainingSymbol is IMethodSymbol))
                return;

            if (IsContextBoundServiceBusCall(invocation, analysisContext.Compilation, consumeContextTypeSymbol,
                    outgoingMessagesTypeSymbol, analysisContext.CancellationToken))
                return;

            if (!HasAnOverloadWithCancellationToken(invocation, cancellationTokenSymbol, cancellationTokenSourceSymbol, out var newParameterIndex,
                    out var newParameterName))
                return;

            var availableCancellationTokens = FindCancellationTokens(invocation, cancellationTokenSymbol, pipeContextTypeSymbol,
                analysisContext.CancellationToken, membersByType);

            if (!availableCancellationTokens.Any())
                return;

            var methodNameNode = GetInvocationMethodNameNode(analysisContext.Operation.Syntax) ?? analysisContext.Operation.Syntax;

            ImmutableDictionary<string, string?> properties = ImmutableDictionary.Create<string, string?>(StringComparer.Ordinal)
                .Add(ParameterIndex, newParameterIndex.ToString(CultureInfo.InvariantCulture))
                .Add(ParameterName, newParameterName)
                .Add(CancellationTokens, string.Join(",", availableCancellationTokens));

            var diagnostic = Diagnostic.Create(CancellationTokenOverloadMethodRule, invocation.Syntax.GetLocation(), properties,
                string.Join(",", availableCancellationTokens), methodNameNode);

            analysisContext.ReportDiagnostic(diagnostic);
        }, OperationKind.Invocation);
    }

    static bool IsContextBoundServiceBusCall(IInvocationOperation operation, Compilation compilation, ISymbol? consumeContextTypeSymbol,
        ISymbol? outgoingMessagesTypeSymbol, CancellationToken cancellationToken)
    {
        var receiverType = operation.GetReceiverType(compilation, true, cancellationToken);
        return receiverType != null
            && (consumeContextTypeSymbol != null && TryGetInterface(receiverType, consumeContextTypeSymbol, out _)
                || outgoingMessagesTypeSymbol != null && TryGetInterface(receiverType, outgoingMessagesTypeSymbol, out _));
    }

    static bool TryGetInterface(ITypeSymbol? symbol, ISymbol expectedSymbol, out ITypeSymbol? result)
    {
        result = null;
        if (symbol == null)
            return false;

        if (SymbolEqualityComparer.Default.Equals(symbol, expectedSymbol))
        {
            result = symbol;
            return true;
        }

        foreach (var s in symbol.Interfaces)
        {
            if (TryGetInterface(s, expectedSymbol, out result))
                return true;
        }

        return false;
    }

    static bool HasAnOverloadWithCancellationToken(IInvocationOperation operation, ITypeSymbol cancellationTokenSymbol,
        ISymbol? cancellationTokenSourceSymbol, out int parameterIndex, out string? parameterName)
    {
        parameterName = null;
        parameterIndex = -1;
        var method = operation.TargetMethod;
        if (method.Name == nameof(CancellationTokenSource.CreateLinkedTokenSource)
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, cancellationTokenSourceSymbol))
            return false;

        if (IsArgumentImplicitlyDeclared(operation, cancellationTokenSymbol, out parameterIndex, out parameterName))
            return true;

        var overload = FindOverloadWithAdditionalParameterOfType(operation.TargetMethod, cancellationTokenSymbol);
        if (overload == null)
            return false;

        for (var i = 0; i < overload.Parameters.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(overload.Parameters[i].Type, cancellationTokenSymbol))
                continue;
            parameterName ??= overload.Parameters[i].Name;
            parameterIndex = i;
            break;
        }

        return true;


        static bool IsArgumentImplicitlyDeclared(IInvocationOperation invocationOperation, ISymbol cancellationTokenSymbol, out int parameterIndex,
            out string? parameterName)
        {
            parameterIndex = -1;
            parameterName = null;

            static bool IsValid(IArgumentOperation arg, ISymbol cancellationTokenSymbol)
            {
                return arg is { IsImplicit: true, Parameter: { } } && SymbolEqualityComparer.Default.Equals(arg.Parameter.Type, cancellationTokenSymbol);
            }

            foreach (var arg in invocationOperation.Arguments.Where(x => IsValid(x, cancellationTokenSymbol)))
            {
                if (arg.Parameter == null)
                    continue;

                parameterIndex = invocationOperation.TargetMethod.Parameters.IndexOf(arg.Parameter);
                parameterName = arg.Parameter.Name;

                return true;
            }

            return false;
        }
    }

    static IMethodSymbol? FindOverloadWithAdditionalParameterOfType(IMethodSymbol methodSymbol, ITypeSymbol additionalParameterType)
    {
        methodSymbol = methodSymbol.OriginalDefinition;
        ImmutableArray<ISymbol> members = methodSymbol.ContainingType.GetMembers(methodSymbol.Name);

        return members.OfType<IMethodSymbol>()
            .FirstOrDefault(member => HasSameParametersPlus(methodSymbol, member, additionalParameterType));
    }

    static bool HasSameParametersPlus(IMethodSymbol method, IMethodSymbol candidate, ITypeSymbol additionalParameterType)
    {
        if (SymbolEqualityComparer.Default.Equals(method, candidate)
            || method.Arity != candidate.Arity
            || candidate.Parameters.Length != method.Parameters.Length + 1)
            return false;

        for (var addedIndex = 0; addedIndex < candidate.Parameters.Length; addedIndex++)
        {
            var addedParameter = candidate.Parameters[addedIndex];
            if (addedParameter.RefKind != RefKind.None
                || !SymbolEqualityComparer.Default.Equals(addedParameter.Type, additionalParameterType))
                continue;

            var matches = true;
            for (var sourceIndex = 0; sourceIndex < method.Parameters.Length; sourceIndex++)
            {
                var candidateIndex = sourceIndex < addedIndex ? sourceIndex : sourceIndex + 1;
                if (!ParametersMatch(method.Parameters[sourceIndex], candidate.Parameters[candidateIndex]))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return true;
        }

        return false;

        static bool ParametersMatch(IParameterSymbol source, IParameterSymbol candidateParameter)
        {
            return source.RefKind == candidateParameter.RefKind
                && source.IsParams == candidateParameter.IsParams
                && string.Equals(source.Name, candidateParameter.Name, StringComparison.Ordinal)
                && SymbolEqualityComparer.Default.Equals(source.Type, candidateParameter.Type);
        }
    }

    static SyntaxNode? GetInvocationMethodNameNode(SyntaxNode invocationNode)
    {
        if (!(invocationNode is InvocationExpressionSyntax invocationExpression))
            return null;

        if (invocationExpression.Expression is MemberBindingExpressionSyntax memberBindingExpression)
        {
            // A conditional-access dot belongs to the invocation expression; the diagnostic spans only the member name.
            return memberBindingExpression.Name;
        }

        return invocationExpression.Expression;
    }

    static ImmutableArray<INamedTypeSymbol> GetTypesByMetadataName(Compilation compilation, string typeMetadataName)
    {
        var result = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        var symbol = compilation.Assembly.GetTypeByMetadataName(typeMetadataName);
        if (symbol != null)
            result.Add(symbol);

        foreach (var reference in compilation.References)
        {
            if (!(compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol))
                continue;

            symbol = assemblySymbol.GetTypeByMetadataName(typeMetadataName);
            if (symbol != null)
                result.Add(symbol);
        }

        return result.ToImmutable();
    }

    static INamedTypeSymbol? GetBestTypeByMetadataName(Compilation compilation, string fullyQualifiedMetadataName)
    {
        INamedTypeSymbol? type = null;

        foreach (var currentType in GetTypesByMetadataName(compilation, fullyQualifiedMetadataName))
        {
            if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly))
                return currentType;

            switch (GetResultantVisibility(currentType))
            {
                case SymbolVisibility.Public:
                case SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                    break;

                default:
                    continue;
            }

            if (type is not null)
            {
                // An ambiguous metadata name cannot identify a safe analyzer target.
                return null;
            }

            type = currentType;
        }

        return type;
    }

    static SymbolVisibility GetResultantVisibility(ISymbol symbol)
    {
        var visibility = SymbolVisibility.Public;
        switch (symbol.Kind)
        {
            case SymbolKind.Alias:
                // Aliases are visible only in their declaring source file.
                return SymbolVisibility.Private;
            case SymbolKind.Parameter:
                // Parameters inherit the visibility of their containing symbol.
                return GetResultantVisibility(symbol.ContainingSymbol);
            case SymbolKind.TypeParameter:
                // Type parameters are not independently addressable outside their declaration.
                return SymbolVisibility.Private;
        }

        while (symbol != null && symbol.Kind != SymbolKind.Namespace)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return SymbolVisibility.Private;
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                    visibility = SymbolVisibility.Internal;
                    break;
            }

            symbol = symbol.ContainingSymbol;
        }

        return visibility;
    }

    static string[] FindCancellationTokens(IOperation operation, INamedTypeSymbol cancellationTokenSymbol, INamedTypeSymbol pipeContextSymbol,
        CancellationToken cancellationToken, ConcurrentDictionary<ISymbol, IEnumerable<ISymbol>> membersByType)
    {
        var isStatic = operation.IsStaticMember(cancellationToken);
        var paths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var availableSymbol in operation.GetParameters(cancellationToken))
        {
            foreach (var member in GetMembers(availableSymbol.TypeSymbol, cancellationTokenSymbol, pipeContextSymbol, membersByType))
            {
                if (!IsSymbolAccessibleFromOperation(member, operation))
                    continue;

                if (availableSymbol.Name == null && isStatic && !member.IsStatic)
                    continue;

                var fullPath = ComputeFullPath(availableSymbol.Name, member);
                paths.Add(fullPath);
            }
        }

        return paths.Count == 0
            ? []
            : paths.OrderBy(value => value.Count(c => c == '.')).ThenBy(value => value, StringComparer.Ordinal).ToArray();

        static string ComputeFullPath(string? prefix, ISymbol symbols)
        {
            if (prefix == null)
                return symbols.Name;

            if (string.IsNullOrEmpty(symbols.Name))
                return prefix;

            return prefix + "." + symbols.Name;
        }

        static bool IsSymbolAccessibleFromOperation(ISymbol symbol, IOperation operation)
        {
            return operation.SemanticModel?.IsAccessible(operation.Syntax.Span.Start, symbol) == true;
        }
    }

    static IEnumerable<ISymbol> GetMembers(ITypeSymbol symbol, ISymbol cancellationTokenSymbol, ISymbol pipeContextSymbol,
        ConcurrentDictionary<ISymbol, IEnumerable<ISymbol>> membersByType)
    {
        return membersByType.GetOrAdd(symbol, _ =>
        {
            // Special framework types cannot expose a nested service-bus cancellation token path.
            if (symbol.SpecialType != SpecialType.None)
                return [];

            var result = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var member in GetPipeContextMembers(symbol, pipeContextSymbol))
            {
                switch (member)
                {
                    case IPropertySymbol propertySymbol when SymbolEqualityComparer.Default.Equals(propertySymbol.Type, cancellationTokenSymbol):
                        result.Add(propertySymbol);
                        break;

                    case IFieldSymbol fieldSymbol when SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, cancellationTokenSymbol):
                        result.Add(fieldSymbol);
                        break;
                }
            }

            return result;
        });
    }

    static HashSet<ISymbol> GetPipeContextMembers(ITypeSymbol? symbol, ISymbol pipeContextSymbol)
    {
        if (symbol == null)
            return new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        static bool Filter(ISymbol symbol)
        {
            return symbol is { IsImplicitlyDeclared: false, Kind: SymbolKind.Property };
        }

        if (TryGetInterface(symbol, pipeContextSymbol, out var result) && result != null)
            return new HashSet<ISymbol>(result.GetMembers().Where(Filter), SymbolEqualityComparer.Default);

        return new HashSet<ISymbol>(SymbolEqualityComparer.Default);
    }


    enum SymbolVisibility
    {
        /// <summary>The symbol is visible to every referencing assembly.</summary>
        Public,
        /// <summary>The symbol is visible within its assembly or to a declared friend assembly.</summary>
        Internal,
        /// <summary>The symbol cannot be selected through a metadata-name lookup from another source context.</summary>
        Private,
    }
}
