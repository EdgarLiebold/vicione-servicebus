using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ViciOne.ServiceBus.Analyzers.Internals;

static class OperationExtensions
{
    public static ITypeSymbol? GetSourceReceiverType(this IInvocationOperation invocation, Compilation compilation,
        CancellationToken cancellationToken)
    {
        if (invocation.Instance != null)
            return GetReceiverType(invocation.Instance.Syntax, compilation, cancellationToken);

        if (!invocation.TargetMethod.IsExtensionMethod || invocation.TargetMethod.Parameters.IsEmpty)
            return null;
        var firstArg = invocation.Arguments.FirstOrDefault();
        if (firstArg != null)
            return GetReceiverType(firstArg.Value.Syntax, compilation, cancellationToken);

        return invocation.TargetMethod.Parameters[0].IsParams ? invocation.TargetMethod.Parameters[0].Type : null;
    }

    static ITypeSymbol? GetReceiverType(SyntaxNode receiverSyntax, Compilation compilation, CancellationToken cancellationToken)
    {
        var model = compilation.GetSemanticModel(receiverSyntax.SyntaxTree);
        var typeInfo = model.GetTypeInfo(receiverSyntax, cancellationToken);
        return typeInfo.Type;
    }

    public static List<NameAndType> GetParameters(this IOperation operation, CancellationToken cancellationToken)
    {
        var result = new List<NameAndType>();
        var semanticModel = operation.SemanticModel;
        if (semanticModel == null)
            return result;

        var node = operation.Syntax;

        while (node != null)
        {
            switch (node)
            {
                case AccessorDeclarationSyntax accessor:
                    AddSetterValue(result, semanticModel, accessor, cancellationToken);
                    break;

                case PropertyDeclarationSyntax _:
                    return result;

                case AnonymousFunctionExpressionSyntax anonymousFunction:
                    if (semanticModel.GetOperation(anonymousFunction, cancellationToken) is IAnonymousFunctionOperation lambdaOperation)
                        AddParameters(result, lambdaOperation.Symbol.Parameters);
                    break;

                case IndexerDeclarationSyntax indexerDeclarationSyntax:
                    AddParameters(result, semanticModel.GetDeclaredSymbol(indexerDeclarationSyntax, cancellationToken)?.Parameters);
                    return result;

                case MethodDeclarationSyntax methodDeclaration:
                    AddParameters(result, semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken)?.Parameters);
                    return result;

                case LocalFunctionStatementSyntax localFunctionStatement:
                    AddParameters(result, semanticModel.GetDeclaredSymbol(localFunctionStatement, cancellationToken)?.Parameters);
                    break;

                case ConstructorDeclarationSyntax constructorDeclaration:
                    AddParameters(result, semanticModel.GetDeclaredSymbol(constructorDeclaration, cancellationToken)?.Parameters);
                    return result;
            }

            node = node.Parent;
        }

        return result;
    }

    static void AddSetterValue(List<NameAndType> result, SemanticModel semanticModel, AccessorDeclarationSyntax accessor,
        CancellationToken cancellationToken)
    {
        if (!accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
            return;

        var property = accessor.Ancestors().OfType<BasePropertyDeclarationSyntax>().FirstOrDefault();
        IPropertySymbol? symbol = property switch
        {
            PropertyDeclarationSyntax declaration => semanticModel.GetDeclaredSymbol(declaration, cancellationToken),
            IndexerDeclarationSyntax declaration => semanticModel.GetDeclaredSymbol(declaration, cancellationToken),
            _ => null,
        };
        if (symbol is not null)
            result.Add(new NameAndType("value", symbol.Type));
    }

    static void AddParameters(List<NameAndType> result, ImmutableArray<IParameterSymbol>? parameters)
    {
        if (parameters is { } values)
            result.AddRange(values.Select(parameter => new NameAndType(parameter.Name, parameter.Type)));
    }


    [StructLayout(LayoutKind.Auto)]
    internal readonly struct NameAndType(string name, ITypeSymbol typeSymbol)
    {
        public string Name { get; } = name;
        public ITypeSymbol TypeSymbol { get; } = typeSymbol;
    }
}
