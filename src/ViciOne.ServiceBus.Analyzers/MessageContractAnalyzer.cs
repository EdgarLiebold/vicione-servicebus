using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ViciOne.ServiceBus.Analyzers.Internals;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>Validates anonymous message values against their declared message contracts.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessageContractAnalyzer :
    DiagnosticAnalyzer
{
    /// <summary>Identifies structurally incompatible anonymous message values.</summary>
    public const string StructurallyCompatibleRuleId = "VOSB1002";
    /// <summary>Identifies message-contract properties omitted from anonymous values.</summary>
    public const string MissingPropertiesRuleId = "VOSB1004";

    const string Category = "Usage";

    static readonly DiagnosticDescriptor StructurallyCompatibleRule = new DiagnosticDescriptor(StructurallyCompatibleRuleId,
        "Anonymous type does not map to message contract",
        "Anonymous type does not map to message contract '{0}'. The following properties of the anonymous type are incompatible: {1}.",
        Category, DiagnosticSeverity.Error, true,
        "Anonymous type should map to message contract.");

    static readonly DiagnosticDescriptor MissingPropertiesRule = new DiagnosticDescriptor(MissingPropertiesRuleId,
        "Anonymous type is missing properties that are in the message contract",
        "Anonymous type is missing properties that are in the message contract '{0}'. The following properties are missing: {1}.",
        Category, DiagnosticSeverity.Info, true,
        "Anonymous type misses properties that are in the message contract.");

    /// <summary>Gets the structural-compatibility and missing-property diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(StructurallyCompatibleRule, MissingPropertiesRule);

    /// <summary>Registers analysis for invocations of recognized message producers.</summary>
    /// <param name="context">The analyzer registration context.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeProducerInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeProducerInvocation(SyntaxNodeAnalysisContext context)
    {
        ITypeSymbol typeArgument;

        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocationExpression.Expression, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!methodSymbol.IsProducerMethod(out var producerIndex))
            return;

        switch (producerIndex)
        {
            case -1:
                {
                    if (!(methodSymbol.ReceiverType is INamedTypeSymbol { IsGenericType: true } parentNamedType) || !parentNamedType.TypeArguments.Any())
                        return;

                    typeArgument = parentNamedType.TypeArguments.First();
                    break;
                }
            case 0:
                {
                    if (!methodSymbol.IsGenericMethod || !methodSymbol.TypeArguments.Any())
                        return;

                    typeArgument = methodSymbol.TypeArguments.First();
                    break;
                }
            default:
                return;
        }

        var messageArgument = invocationExpression.ArgumentList.Arguments.FirstOrDefault(argument =>
            context.SemanticModel.GetOperation(argument, context.CancellationToken) is IArgumentOperation
            {
                Parameter.Type.SpecialType: SpecialType.System_Object,
            });
        if (messageArgument == null)
            return;

        SyntaxNode messageValue = messageArgument.Expression;

        var typeConversion = new MessageTypeConversion(context.SemanticModel);

        if (typeArgument.HasMessageContract(out var messageContractType))
        {
            var anonymousType = context.SemanticModel.GetTypeInfo(messageValue, context.CancellationToken).Type;

            if (anonymousType == null || anonymousType.SpecialType == SpecialType.System_Object)
                return;

            var symbolDisplayFormat =
                new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            ImmutableDictionary<string, string?> immutableDictionary =
                new Dictionary<string, string?> { { "messageContractType", messageContractType.ToDisplayString(symbolDisplayFormat) } }
                    .ToImmutableDictionary();

            var incompatibleProperties = new List<string>();
            if (!TypesAreStructurallyCompatible(typeConversion, messageContractType, anonymousType, string.Empty, incompatibleProperties))
            {
                var diagnostic = Diagnostic.Create(StructurallyCompatibleRule, DiagnosticLocation(anonymousType, messageValue), immutableDictionary,
                    messageContractType.Name,
                    string.Join(", ", incompatibleProperties));
                context.ReportDiagnostic(diagnostic);
            }

            var missingProperties = new List<string>();
            IEnumerable<ITypeSymbol> symbolPath = Enumerable.Empty<ITypeSymbol>();
            if (HasMissingProperties(anonymousType, messageContractType, string.Empty, symbolPath, missingProperties))
            {
                var diagnostic = Diagnostic.Create(MissingPropertiesRule, DiagnosticLocation(anonymousType, messageValue), immutableDictionary,
                    messageContractType.Name, string.Join(", ", missingProperties));
                context.ReportDiagnostic(diagnostic);
            }
        }

        static Location DiagnosticLocation(ITypeSymbol messageType, SyntaxNode messageValue)
        {
            return messageType.Locations.FirstOrDefault(location => location.IsInSource) ?? messageValue.GetLocation();
        }
    }

    static bool TypesAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractType, ITypeSymbol inputType,
        string path, ICollection<string> incompatibleProperties)
    {
        if (SymbolEqualityComparer.Default.Equals(inputType, contractType))
            return true;

        List<IPropertySymbol> contractProperties = contractType.GetContractProperties();
        List<IPropertySymbol> inputProperties = GetInputProperties(inputType);
        var result = true;

        foreach (var inputProperty in inputProperties)
        {
            var contractProperty = contractProperties.FirstOrDefault(m => m.Name.Equals(inputProperty.Name, StringComparison.OrdinalIgnoreCase));

            var propertyPath = Append(path, inputProperty.Name);

            if (contractProperty == null)
            {
                if (!IsHeaderProperty(typeConversion, inputProperty))
                {
                    incompatibleProperties.Add(propertyPath);
                    result = false;
                }
            }
            else if (!PropertyTypesAreStructurallyCompatible(typeConversion, contractProperty, inputProperty, propertyPath, incompatibleProperties))
                result = false;
        }

        return result;
    }

    static bool PropertyTypesAreStructurallyCompatible(MessageTypeConversion typeConversion, IPropertySymbol contractProperty,
        IPropertySymbol inputProperty,
        string path, ICollection<string> incompatibleProperties)
    {
        var contractPropertyType = contractProperty.Type;
        var inputPropertyType = inputProperty.Type;

        if (typeConversion.CanConvert(contractPropertyType, inputPropertyType))
            return true;

        var result = AnonymousTypeAndInterfaceAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, path,
                incompatibleProperties)
            ?? EnumerableTypesAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, path, incompatibleProperties)
            ?? DictionaryTypesAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, path, incompatibleProperties);
        if (result.HasValue)
            return result.Value;

        incompatibleProperties.Add(path);
        return false;
    }

    static bool? AnonymousTypeAndInterfaceAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractPropertyType,
        ITypeSymbol inputPropertyType,
        string path, ICollection<string> incompatibleProperties)
    {
        if (inputPropertyType.IsAnonymousType)
        {
            if (contractPropertyType.TypeKind.IsClassOrInterface())
            {
                if (!TypesAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, path, incompatibleProperties))
                    return false;
            }
            else
            {
                incompatibleProperties.Add(path);
                return false;
            }

            return true;
        }

        return null;
    }

    static bool? EnumerableTypesAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractPropertyType,
        ITypeSymbol inputPropertyType, string path, ICollection<string> incompatibleProperties)
    {
        if (contractPropertyType.IsImmutableArray(out var contractElementType)
            || contractPropertyType.IsList(out contractElementType)
            || contractPropertyType.IsArray(out contractElementType)
            || contractPropertyType.IsCollection(out contractElementType)
            || contractPropertyType.IsEnumerable(out contractElementType))
        {
            if (inputPropertyType.IsImmutableArray(out var inputElementType)
                || inputPropertyType.IsList(out inputElementType)
                || inputPropertyType.IsArray(out inputElementType)
                || inputPropertyType.IsEnumerable(out inputElementType)
                || inputPropertyType.IsCollection(out inputElementType))
            {
                if (!ElementTypesAreStructurallyCompatible(typeConversion, contractElementType, inputElementType, path, incompatibleProperties))
                    return false;
            }
            // A convertible scalar initializes a one-element message-contract collection.
            else if (!typeConversion.CanConvert(contractElementType, inputPropertyType))
            {
                incompatibleProperties.Add(path);
                return false;
            }

            return true;
        }

        return null;
    }

    static bool ElementTypesAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractElementType,
        ITypeSymbol inputElementType, string path, ICollection<string> incompatibleProperties)
    {
        if (typeConversion.CanConvert(contractElementType, inputElementType))
            return true;

        if (contractElementType.TypeKind.IsClassOrInterface())
        {
            if (!TypesAreStructurallyCompatible(typeConversion, contractElementType, inputElementType, path, incompatibleProperties))
                return false;
        }
        else
        {
            incompatibleProperties.Add(path);
            return false;
        }

        return true;
    }

    static bool? DictionaryTypesAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractPropertyType,
        ITypeSymbol inputPropertyType, string path, ICollection<string> incompatibleProperties)
    {
        if (contractPropertyType.IsDictionary(out var contractKeyType, out var contractValueType))
        {
            if (inputPropertyType.IsDictionary(out var inputKeyType, out var inputValueType))
            {
                if (!KeyValueTypesAreStructurallyCompatible(typeConversion, contractKeyType, contractValueType, inputKeyType, inputValueType, path,
                        incompatibleProperties))
                    return false;
            }
            else
            {
                incompatibleProperties.Add(path);
                return false;
            }

            return true;
        }

        return null;
    }

    static bool KeyValueTypesAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractKeyType, ITypeSymbol contractValueType,
        ITypeSymbol inputKeyType, ITypeSymbol inputValueType, string path, ICollection<string> incompatibleProperties)
    {
        if (!typeConversion.CanConvert(contractKeyType, inputKeyType))
        {
            incompatibleProperties.Add(path);
            return false;
        }

        if (typeConversion.CanConvert(contractValueType, inputValueType))
            return true;

        if (contractValueType.TypeKind.IsClassOrInterface())
        {
            if (!TypesAreStructurallyCompatible(typeConversion, contractValueType, inputValueType, path, incompatibleProperties))
                return false;
        }
        else
        {
            incompatibleProperties.Add(path);
            return false;
        }

        return true;
    }

    static bool IsHeaderProperty(MessageTypeConversion typeConversion, IPropertySymbol messageProperty)
    {
        if (!messageProperty.Name.StartsWith("__"))
            return false;

        if (messageProperty.Name.StartsWith("__Header_"))
            return true;

        return messageProperty.Name switch
        {
            "__SourceAddress" => typeConversion.CanConvert(typeof(Uri), messageProperty.Type),
            "__DestinationAddress" => typeConversion.CanConvert(typeof(Uri), messageProperty.Type),
            "__ResponseAddress" => typeConversion.CanConvert(typeof(Uri), messageProperty.Type),
            "__FaultAddress" => typeConversion.CanConvert(typeof(Uri), messageProperty.Type),
            "__RequestId" => typeConversion.CanConvert(typeof(Guid), messageProperty.Type),
            "__MessageId" => typeConversion.CanConvert(typeof(Guid), messageProperty.Type),
            "__ConversationId" => typeConversion.CanConvert(typeof(Guid), messageProperty.Type),
            "__CorrelationId" => typeConversion.CanConvert(typeof(Guid), messageProperty.Type),
            "__InitiatorId" => typeConversion.CanConvert(typeof(Guid), messageProperty.Type),
            "__ScheduledMessageId" => typeConversion.CanConvert(typeof(Guid), messageProperty.Type),
            "__TimeToLive" => typeConversion.CanConvert(typeof(TimeSpan), messageProperty.Type),
            "__Durable" => typeConversion.CanConvert(typeof(bool), messageProperty.Type),
            _ => false
        };
    }

    static bool HasMissingProperties(ITypeSymbol inputType, ITypeSymbol contractType,
        string path, IEnumerable<ITypeSymbol> symbolPath, ICollection<string> missingProperties)
    {
        List<IPropertySymbol> contractProperties = contractType.GetContractProperties();
        List<IPropertySymbol> inputProperties = GetInputProperties(inputType);
        var result = false;

        foreach (var contractProperty in contractProperties)
        {
            var inputProperty = inputProperties.FirstOrDefault(m => m.Name.Equals(contractProperty.Name, StringComparison.OrdinalIgnoreCase));

            var propertyPath = Append(path, contractProperty.Name);

            if (inputProperty == null)
            {
                missingProperties.Add(propertyPath);
                result = true;
            }
            else if (HasMissingProperties(inputProperty, contractProperty, propertyPath, symbolPath, missingProperties))
                result = true;
        }

        return result;
    }

    static bool HasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, IEnumerable<ITypeSymbol> symbolPath, ICollection<string> missingProperties)
    {
        var result = EnumerableTypeHasMissingProperties(inputProperty, contractProperty, path, symbolPath, missingProperties)
            ?? DictionaryTypeHasMissingProperties(inputProperty, contractProperty, path, symbolPath, missingProperties)
            ?? AnonymousTypeHasMissingProperties(inputProperty, contractProperty, path, symbolPath, missingProperties);

        return result ?? false;
    }

    static bool? EnumerableTypeHasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, IEnumerable<ITypeSymbol> symbolPath, ICollection<string> missingProperties)
    {
        if (contractProperty.Type.IsImmutableArray(out var contractElementType)
            || contractProperty.Type.IsList(out contractElementType)
            || contractProperty.Type.IsArray(out contractElementType)
            || contractProperty.Type.IsCollection(out contractElementType)
            || contractProperty.Type.IsEnumerable(out contractElementType))
        {
            if ((inputProperty.Type.IsImmutableArray(out var inputElementType)
                    || inputProperty.Type.IsList(out inputElementType)
                    || inputProperty.Type.IsArray(out inputElementType)
                    || inputProperty.Type.IsCollection(out inputElementType)
                    || inputProperty.Type.IsEnumerable(out inputElementType))
                && contractElementType.TypeKind.IsClassOrInterface()
                && !symbolPath.Contains(contractElementType, SymbolEqualityComparer.Default)
                && HasMissingProperties(inputElementType, contractElementType, path, symbolPath.Concat(new[] { contractElementType }), missingProperties))
                return true;

            return false;
        }

        return null;
    }

    static bool? DictionaryTypeHasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, IEnumerable<ITypeSymbol> symbolPath, ICollection<string> missingProperties)
    {
        if (!contractProperty.Type.IsDictionary(out _, out var contractValueType))
            return null;

        if (inputProperty.Type.IsDictionary(out _, out var inputValueType)
            && contractValueType.TypeKind.IsClassOrInterface()
            && !symbolPath.Contains(contractValueType, SymbolEqualityComparer.Default)
            && HasMissingProperties(inputValueType, contractValueType, path,
                symbolPath.Concat(new[] { contractValueType }), missingProperties))
            return true;

        return false;
    }

    static bool? AnonymousTypeHasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, IEnumerable<ITypeSymbol> symbolPath, ICollection<string> missingProperties)
    {
        if (contractProperty.Type.TypeKind.IsClassOrInterface())
        {
            if (inputProperty.Type.IsAnonymousType
                && !symbolPath.Contains(contractProperty.Type, SymbolEqualityComparer.Default)
                && HasMissingProperties(inputProperty.Type, contractProperty.Type, path, symbolPath.Concat(new[] { contractProperty.Type }),
                    missingProperties))
                return true;

            return false;
        }

        return null;
    }

    static List<IPropertySymbol> GetInputProperties(ITypeSymbol inputType)
    {
        return inputType.GetMembers().OfType<IPropertySymbol>().ToList();
    }

    static string Append(string path, string propertyName)
    {
        if (string.IsNullOrEmpty(path))
            return propertyName;

        if (path.EndsWith(".", StringComparison.Ordinal))
            return $"{path}{propertyName}";

        return $"{path}.{propertyName}";
    }
}
