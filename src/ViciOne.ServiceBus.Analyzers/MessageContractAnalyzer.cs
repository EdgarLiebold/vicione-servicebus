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

/// <summary>Validates message initializer values against their declared message contracts.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessageContractAnalyzer :
    DiagnosticAnalyzer
{
    static readonly ImmutableDictionary<string, Type> s_headerTypes =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["__SourceAddress"] = typeof(Uri),
            ["__DestinationAddress"] = typeof(Uri),
            ["__ResponseAddress"] = typeof(Uri),
            ["__FaultAddress"] = typeof(Uri),
            ["__RequestId"] = typeof(Guid),
            ["__MessageId"] = typeof(Guid),
            ["__ConversationId"] = typeof(Guid),
            ["__CorrelationId"] = typeof(Guid),
            ["__InitiatorId"] = typeof(Guid),
            ["__ScheduledMessageId"] = typeof(Guid),
            ["__TimeToLive"] = typeof(TimeSpan),
            ["__Durable"] = typeof(bool),
        }.ToImmutableDictionary(StringComparer.Ordinal);

    /// <summary>Identifies message values that are structurally incompatible with their contracts.</summary>
    public const string StructurallyCompatibleRuleId = "VOSB1002";
    /// <summary>Identifies message-contract properties omitted from initializer values.</summary>
    public const string MissingPropertiesRuleId = "VOSB1004";

    const string Category = "Usage";

    static readonly DiagnosticDescriptor StructurallyCompatibleRule = new(StructurallyCompatibleRuleId,
        "Message values are incompatible with the contract",
        "Message values do not map to contract '{0}'; incompatible properties: {1}",
        Category, DiagnosticSeverity.Error, true,
        "Every supplied message value must be structurally compatible with the corresponding contract property.");

    static readonly DiagnosticDescriptor MissingPropertiesRule = new(MissingPropertiesRuleId,
        "Message values omit contract properties",
        "Message values for contract '{0}' are missing properties: {1}",
        Category, DiagnosticSeverity.Info, true,
        "Message initializer values should include every serializable property declared by the message contract.");

    /// <summary>Gets the structural-compatibility and missing-property diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [StructurallyCompatibleRule, MissingPropertiesRule];

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
        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocationExpression.Expression, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!methodSymbol.TryGetProducerContractTypeArgument(out ITypeSymbol typeArgument))
            return;

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
            var structuralPath = new HashSet<TypePair>(TypePairComparer.Instance);
            if (!TypesAreStructurallyCompatible(typeConversion, messageContractType, anonymousType, structuralPath, string.Empty,
                    incompatibleProperties))
            {
                var diagnostic = Diagnostic.Create(StructurallyCompatibleRule, DiagnosticLocation(anonymousType, messageValue), immutableDictionary,
                    messageContractType.Name,
                    string.Join(", ", incompatibleProperties));
                context.ReportDiagnostic(diagnostic);
            }

            var missingProperties = new List<string>();
            var missingPath = new HashSet<TypePair>(TypePairComparer.Instance);
            if (HasMissingProperties(anonymousType, messageContractType, string.Empty, missingPath, missingProperties))
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
        ISet<TypePair> typePath, string path, ICollection<string> incompatibleProperties)
    {
        if (SymbolEqualityComparer.Default.Equals(inputType, contractType))
            return true;

        var pair = new TypePair(contractType, inputType);
        if (!typePath.Add(pair))
            return true;

        try
        {
            List<IPropertySymbol> contractProperties = contractType.GetSerializableProperties();
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
                else if (!PropertyTypesAreStructurallyCompatible(typeConversion, contractProperty, inputProperty, typePath, propertyPath,
                             incompatibleProperties))
                    result = false;
            }

            return result;
        }
        finally
        {
            typePath.Remove(pair);
        }
    }

    static bool PropertyTypesAreStructurallyCompatible(MessageTypeConversion typeConversion, IPropertySymbol contractProperty,
        IPropertySymbol inputProperty, ISet<TypePair> typePath,
        string path, ICollection<string> incompatibleProperties)
    {
        var contractPropertyType = contractProperty.Type;
        var inputPropertyType = inputProperty.Type;

        if (typeConversion.CanConvert(contractPropertyType, inputPropertyType))
            return true;

        var result = AnonymousTypeAndInterfaceAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, typePath,
                path, incompatibleProperties)
            ?? EnumerableTypesAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, typePath, path,
                incompatibleProperties)
            ?? DictionaryTypesAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, typePath, path,
                incompatibleProperties);
        if (result.HasValue)
            return result.Value;

        incompatibleProperties.Add(path);
        return false;
    }

    static bool? AnonymousTypeAndInterfaceAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractPropertyType,
        ITypeSymbol inputPropertyType, ISet<TypePair> typePath,
        string path, ICollection<string> incompatibleProperties)
    {
        if (inputPropertyType.IsAnonymousType)
        {
            if (contractPropertyType.TypeKind.IsClassOrInterface())
            {
                if (!TypesAreStructurallyCompatible(typeConversion, contractPropertyType, inputPropertyType, typePath, path,
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

    static bool? EnumerableTypesAreStructurallyCompatible(MessageTypeConversion typeConversion, ITypeSymbol contractPropertyType,
        ITypeSymbol inputPropertyType, ISet<TypePair> typePath, string path, ICollection<string> incompatibleProperties)
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
                if (!ElementTypesAreStructurallyCompatible(typeConversion, contractElementType, inputElementType, typePath, path,
                        incompatibleProperties))
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
        ITypeSymbol inputElementType, ISet<TypePair> typePath, string path, ICollection<string> incompatibleProperties)
    {
        if (typeConversion.CanConvert(contractElementType, inputElementType))
            return true;

        if (contractElementType.TypeKind.IsClassOrInterface())
        {
            if (!TypesAreStructurallyCompatible(typeConversion, contractElementType, inputElementType, typePath, path, incompatibleProperties))
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
        ITypeSymbol inputPropertyType, ISet<TypePair> typePath, string path, ICollection<string> incompatibleProperties)
    {
        if (contractPropertyType.IsDictionary(out var contractKeyType, out var contractValueType))
        {
            if (inputPropertyType.IsDictionary(out var inputKeyType, out var inputValueType))
            {
                if (!KeyValueTypesAreStructurallyCompatible(typeConversion, contractKeyType, contractValueType, inputKeyType, inputValueType,
                        typePath, path, incompatibleProperties))
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
        ITypeSymbol inputKeyType, ITypeSymbol inputValueType, ISet<TypePair> typePath, string path, ICollection<string> incompatibleProperties)
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
            if (!TypesAreStructurallyCompatible(typeConversion, contractValueType, inputValueType, typePath, path, incompatibleProperties))
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
        if (!messageProperty.Name.StartsWith("__", StringComparison.Ordinal))
            return false;

        if (messageProperty.Name.StartsWith("__Header_", StringComparison.Ordinal))
            return true;

        return s_headerTypes.TryGetValue(messageProperty.Name, out var expectedType)
            && typeConversion.CanConvert(expectedType, messageProperty.Type);
    }

    static bool HasMissingProperties(ITypeSymbol inputType, ITypeSymbol contractType,
        string path, ISet<TypePair> typePath, ICollection<string> missingProperties)
    {
        var pair = new TypePair(contractType, inputType);
        if (!typePath.Add(pair))
            return false;

        try
        {
            List<IPropertySymbol> contractProperties = contractType.GetSerializableProperties();
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
                else if (HasMissingProperties(inputProperty, contractProperty, propertyPath, typePath, missingProperties))
                    result = true;
            }

            return result;
        }
        finally
        {
            typePath.Remove(pair);
        }
    }

    static bool HasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, ISet<TypePair> typePath, ICollection<string> missingProperties)
    {
        var result = EnumerableTypeHasMissingProperties(inputProperty, contractProperty, path, typePath, missingProperties)
            ?? DictionaryTypeHasMissingProperties(inputProperty, contractProperty, path, typePath, missingProperties)
            ?? AnonymousTypeHasMissingProperties(inputProperty, contractProperty, path, typePath, missingProperties);

        return result ?? false;
    }

    static bool? EnumerableTypeHasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, ISet<TypePair> typePath, ICollection<string> missingProperties)
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
                && HasMissingProperties(inputElementType, contractElementType, path, typePath, missingProperties))
                return true;

            return false;
        }

        return null;
    }

    static bool? DictionaryTypeHasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, ISet<TypePair> typePath, ICollection<string> missingProperties)
    {
        if (!contractProperty.Type.IsDictionary(out _, out var contractValueType))
            return null;

        if (inputProperty.Type.IsDictionary(out _, out var inputValueType)
            && contractValueType.TypeKind.IsClassOrInterface()
            && HasMissingProperties(inputValueType, contractValueType, path, typePath, missingProperties))
            return true;

        return false;
    }

    static bool? AnonymousTypeHasMissingProperties(IPropertySymbol inputProperty, IPropertySymbol contractProperty,
        string path, ISet<TypePair> typePath, ICollection<string> missingProperties)
    {
        if (contractProperty.Type.TypeKind.IsClassOrInterface())
        {
            if (inputProperty.Type.IsAnonymousType
                && HasMissingProperties(inputProperty.Type, contractProperty.Type, path, typePath, missingProperties))
                return true;

            return false;
        }

        return null;
    }

    static List<IPropertySymbol> GetInputProperties(ITypeSymbol inputType)
    {
        return inputType.GetSerializableProperties();
    }

    static string Append(string path, string propertyName)
    {
        if (string.IsNullOrEmpty(path))
            return propertyName;

        if (path.EndsWith(".", StringComparison.Ordinal))
            return $"{path}{propertyName}";

        return $"{path}.{propertyName}";
    }

    readonly struct TypePair(ITypeSymbol contract, ITypeSymbol input)
    {
        public ITypeSymbol Contract { get; } = contract;
        public ITypeSymbol Input { get; } = input;
    }

    sealed class TypePairComparer : IEqualityComparer<TypePair>
    {
        public static TypePairComparer Instance { get; } = new TypePairComparer();

        public bool Equals(TypePair x, TypePair y)
        {
            return SymbolEqualityComparer.Default.Equals(x.Contract, y.Contract)
                && SymbolEqualityComparer.Default.Equals(x.Input, y.Input);
        }

        public int GetHashCode(TypePair obj)
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(obj.Contract) * 397)
                    ^ SymbolEqualityComparer.Default.GetHashCode(obj.Input);
            }
        }
    }
}
