using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>
/// Provides a common expressions implementation.
/// </summary>
public static class CommonExpressions
{
    static readonly IReadOnlyDictionary<string, int> _producerMethods = InitializeProducerMethods();
    const string TaskNamespace = "System.Threading.Tasks";

    static IReadOnlyDictionary<string, int> InitializeProducerMethods()
    {
        return new Dictionary<string, int>
        {
            { "ViciOne.ServiceBus.Sagas.BehaviorContext.InitAsync", 0 },
            { "ViciOne.ServiceBus.ConsumeContext.RespondAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.ConsumeContextSelfSchedulerExtensions.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.EndpointConventionExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.ForwardExtensions.ForwardAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.IClientFactory.CreateRequest", 0 },
            { "ViciOne.ServiceBus.IMessageScheduler.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.IMessageScheduler.SchedulePublishAsync", 0 },
            { "ViciOne.ServiceBus.IPublishEndpoint.PublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.IRecurringMessageScheduler.ScheduleRecurringSendAsync", 0 },
            { "ViciOne.ServiceBus.IRequestClient.Create", -1 },
            { "ViciOne.ServiceBus.IRequestClient.GetResponseAsync", -1 },
            { "ViciOne.ServiceBus.ISendEndpoint.SendAsync", 0 },
            { "ViciOne.ServiceBus.Initializers.MessageInitializerCache.InitializeAsync", -1 },
            { "ViciOne.ServiceBus.Initializers.MessageInitializerCache.InitializeMessageAsync", -1 },
            { "ViciOne.ServiceBus.Advanced.PublishExecuteExtensions.PublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.PublishEndpointRecurringSchedulerExtensions.ScheduleRecurringSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.RequestExtensions.RequestAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.RespondAsyncExecuteExtensions.RespondAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SchedulePublishExtensions.SchedulePublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SendConsumeContextExecuteExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SendConsumeContextExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SendExecuteExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SendEndpointRecurringSchedulerExtensions.ScheduleRecurringSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.TimeSpanContextScheduleExtensions.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.TimeSpanScheduleExtensions.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.TimeSpanSchedulePublishExtensions.SchedulePublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedMessageInitializerExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedMessageInitializerExtensions.PublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedRequestInitializerExtensions.Create", -1 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedRequestInitializerExtensions.GetResponseAsync", -1 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedScheduleInitializerExtensions.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedScheduleInitializerExtensions.SchedulePublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.IAdvancedRequestClient.GetResponseAsync", -1 }
        };
    }

    /// <summary>
    /// Determines whether producer method.
    /// </summary>
    /// <param name="method">The method value.</param>
    /// <param name="index">The index value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsProducerMethod(this IMethodSymbol method, out int index)
    {
        return _producerMethods.TryGetValue($"{method.ContainingNamespace}.{method.ContainingType.Name}.{method.Name}", out index);
    }

    /// <summary>
    /// Determines whether activator.
    /// </summary>
    /// <param name="argumentSyntax">The argument syntax value.</param>
    /// <param name="semanticModel">The semantic model value.</param>
    /// <param name="typeArgument">The type argument value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsActivator(this ArgumentSyntax? argumentSyntax, SemanticModel semanticModel,
        [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (argumentSyntax != null
            && argumentSyntax.Parent is ArgumentListSyntax argumentListSyntax
            && argumentListSyntax.Parent is InvocationExpressionSyntax invocationExpressionSyntax
            && invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax
            && semanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol is IMethodSymbol method
            && IsProducerMethod(method, out var index)
            && method.Parameters[0].Type.SpecialType == SpecialType.System_Object)
        {
            if (index == 0 && method.TypeArguments.Length == 1)
            {
                typeArgument = method.TypeArguments[0];
                return true;
            }

            if (index == -1 && method.ContainingType.IsGenericType && method.ContainingType.TypeArguments.Length == 1)
            {
                typeArgument = method.ContainingType.TypeArguments[0];
                return true;
            }
        }

        typeArgument = null;
        return false;
    }

    /// <summary>
    /// Determines whether the current value has message contract.
    /// </summary>
    /// <param name="typeArgument">The type argument value.</param>
    /// <param name="contractType">The contract type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool HasMessageContract(this ITypeSymbol typeArgument, [NotNullWhen(true)] out ITypeSymbol? contractType)
    {
        if (typeArgument.TypeKind.IsClassOrInterface())
        {
            contractType = typeArgument;
            return true;
        }

        if (typeArgument.TypeKind == TypeKind.TypeParameter &&
            typeArgument is ITypeParameterSymbol typeParameter &&
            typeParameter.ConstraintTypes.Length == 1 &&
            typeParameter.ConstraintTypes[0].TypeKind.IsClassOrInterface())
        {
            contractType = typeParameter.ConstraintTypes[0];
            return true;
        }

        contractType = null;
        return false;
    }

    /// <summary>
    /// Determines whether immutable array.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="typeArgument">The type argument value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsImmutableArray(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (type.TypeKind == TypeKind.Struct &&
            type.Name == "ImmutableArray" &&
            type.ContainingNamespace.ToString() == "System.Collections.Immutable" &&
            type is INamedTypeSymbol immutableArrayType &&
            immutableArrayType.IsGenericType &&
            immutableArrayType.TypeArguments.Length == 1)
        {
            typeArgument = immutableArrayType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    /// <summary>
    /// Determines whether collection.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="typeArgument">The type argument value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsCollection(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (type.TypeKind == TypeKind.Interface &&
            type.Name == "ICollection" &&
            type.ContainingNamespace.ToString() == "System.Collections.Generic" &&
            type is INamedTypeSymbol collectionType &&
            collectionType.IsGenericType &&
            collectionType.TypeArguments.Length == 1)
        {
            typeArgument = collectionType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    /// <summary>
    /// Determines whether enumerable.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="typeArgument">The type argument value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsEnumerable(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (type.TypeKind == TypeKind.Interface &&
            type.Name == "IEnumerable" &&
            type.ContainingNamespace.ToString() == "System.Collections.Generic" &&
            type is INamedTypeSymbol collectionType &&
            collectionType.IsGenericType &&
            collectionType.TypeArguments.Length == 1)
        {
            typeArgument = collectionType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    /// <summary>
    /// Determines whether list.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="typeArgument">The type argument value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsList(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if ((type.TypeKind == TypeKind.Class && type.Name == "List"
                || type.TypeKind.IsClassOrInterface() && type.Name == "IReadOnlyList"
                || type.TypeKind.IsClassOrInterface() && type.Name == "IList")
            && type.ContainingNamespace.ToString() == "System.Collections.Generic"
            && type is INamedTypeSymbol listType
            && listType.IsGenericType
            && listType.TypeArguments.Length == 1)
        {
            typeArgument = listType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    /// <summary>
    /// Gets contract properties.
    /// </summary>
    /// <param name="contractType">The contract type value.</param>
    /// <returns>The result of the operation.</returns>
    public static List<IPropertySymbol> GetContractProperties(this ITypeSymbol contractType)
    {
        var contractTypes = new List<ITypeSymbol> { contractType };

        contractTypes.AddRange(contractType.AllInterfaces);

        return contractTypes.SelectMany(i => i.GetMembers().OfType<IPropertySymbol>().Where(x => x.DeclaredAccessibility == Accessibility.Public))
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>
    /// Determines whether dictionary.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="keyType">The key type value.</param>
    /// <param name="valueType">The value type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsDictionary(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? keyType,
        [NotNullWhen(true)] out ITypeSymbol? valueType)
    {
        if ((type.TypeKind == TypeKind.Class && type.Name == "Dictionary"
                || type.TypeKind.IsClassOrInterface() && type.Name == "IReadOnlyDictionary"
                || type.TypeKind.IsClassOrInterface() && type.Name == "IDictionary")
            && type.ContainingNamespace.ToString() == "System.Collections.Generic"
            && type is INamedTypeSymbol dictionaryType
            && dictionaryType.IsGenericType
            && dictionaryType.TypeArguments.Length == 2)
        {
            keyType = dictionaryType.TypeArguments[0];
            valueType = dictionaryType.TypeArguments[1];
            return true;
        }

        keyType = null;
        valueType = null;
        return false;
    }

    /// <summary>
    /// Determines whether nullable.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="typeArgument">The type argument value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsNullable(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (type.TypeKind == TypeKind.Struct &&
            type.Name == "Nullable" &&
            type.ContainingNamespace.Name == "System" &&
            type is INamedTypeSymbol nullableType &&
            nullableType.IsGenericType &&
            nullableType.TypeArguments.Length == 1)
        {
            typeArgument = nullableType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    /// <summary>
    /// Determines whether array.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="elementType">The element type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsArray(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? elementType)
    {
        if (type.TypeKind == TypeKind.Array &&
            type is IArrayTypeSymbol arrayTypeSymbol)
        {
            elementType = arrayTypeSymbol.ElementType;
            return true;
        }

        elementType = null;
        return false;
    }

    /// <summary>
    /// Determines whether in var.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="inVarType">The in var type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsInVar(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? inVarType)
    {
        if (type.TypeKind == TypeKind.Class
            && type.ContainingNamespace.Name == "Variables"
            && type.ContainingNamespace.ContainingNamespace.Name == "Initializers"
            && type.ContainingNamespace.ToString() == "ViciOne.ServiceBus.Initializers.Variables")
        {
            var inVar = type.Interfaces.FirstOrDefault(i => i.Name == "IInitializerVariable");
            if (inVar != null &&
                inVar.IsGenericType &&
                inVar.TypeArguments.Length == 1)
            {
                inVarType = inVar.TypeArguments[0];
                return true;
            }
        }

        inVarType = null;
        return false;
    }

    /// <summary>
    /// Performs the returns task operation.
    /// </summary>
    /// <param name="method">The method value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool ReturnsTask(this IMethodSymbol method)
    {
        return method.ReturnType.Name == nameof(Task) && method.ReturnType.ContainingNamespace.ToString() == TaskNamespace;
    }

    /// <summary>
    /// Gets all interfaces.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol type)
    {
        ImmutableArray<INamedTypeSymbol> allInterfaces = type.AllInterfaces;
        if (type is INamedTypeSymbol namedType && namedType.TypeKind.IsClassOrInterface() && !allInterfaces.Contains(namedType))
        {
            var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1) { namedType };
            result.AddRange(allInterfaces);
            return result;
        }

        return allInterfaces;
    }

    /// <summary>
    /// Return the type, and any base types
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<ITypeSymbol> GetAllTypes(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    /// <summary>
    /// Determines whether class or interface.
    /// </summary>
    /// <param name="typeKind">The type kind value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsClassOrInterface(this TypeKind typeKind)
    {
        return typeKind == TypeKind.Interface || typeKind == TypeKind.Class;
    }

    /// <summary>
    /// Performs the implements interface operation.
    /// </summary>
    /// <param name="symbol">The symbol value.</param>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool ImplementsInterface(this ITypeSymbol symbol, ITypeSymbol type)
    {
        return symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, type));
    }

    /// <summary>
    /// Performs the inherits from type operation.
    /// </summary>
    /// <param name="symbol">The symbol value.</param>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool InheritsFromType(this ITypeSymbol symbol, ITypeSymbol type)
    {
        return GetAllTypes(symbol).Any(x => SymbolEqualityComparer.Default.Equals(x, type));
    }

    /// <summary>
    /// Performs the implements type operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="otherType">The other type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool ImplementsType(this ITypeSymbol type, ITypeSymbol otherType)
    {
        IEnumerable<ITypeSymbol> types = GetAllTypes(type);
        IEnumerable<INamedTypeSymbol> interfaces = GetAllInterfaces(type);

        return types.Any(baseType => SymbolEqualityComparer.Default.Equals(baseType, otherType))
            || interfaces.Any(baseInterfaceType => SymbolEqualityComparer.Default.Equals(baseInterfaceType, otherType));
    }
}
