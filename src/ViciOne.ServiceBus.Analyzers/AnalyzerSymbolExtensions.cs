using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>Classifies message-producing methods and Roslyn type shapes used by analyzer rules.</summary>
public static class AnalyzerSymbolExtensions
{
    static readonly IReadOnlyDictionary<string, int> _producerMethods = InitializeProducerMethods();
    static readonly HashSet<string> _producerAssemblies = new HashSet<string>(StringComparer.Ordinal)
    {
        "ViciOne.ServiceBus",
        "ViciOne.ServiceBus.Abstractions",
        "ViciOne.ServiceBus.Initializers",
        "ViciOne.ServiceBus.Sagas",
    };
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
            { "ViciOne.ServiceBus.Advanced.RequestExtensions.RequestAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.RespondAsyncExecuteExtensions.RespondAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SchedulePublishExtensions.SchedulePublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SendConsumeContextExecuteExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SendConsumeContextExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.SendExecuteExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.RelativeMessageSchedulerContextExtensions.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.AdvancedRelativeMessageSchedulerExtensions.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.AdvancedRelativeMessageSchedulerExtensions.SchedulePublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedMessageInitializerExtensions.SendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedMessageInitializerExtensions.PublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedRequestInitializerExtensions.Create", -1 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedRequestInitializerExtensions.GetResponseAsync", -1 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedScheduleInitializerExtensions.ScheduleSendAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.Initializers.AdvancedScheduleInitializerExtensions.SchedulePublishAsync", 0 },
            { "ViciOne.ServiceBus.Advanced.IAdvancedRequestClient.GetResponseAsync", -1 }
        };
    }

    /// <summary>Identifies message-producing Service Bus methods and locates their contract type.</summary>
    /// <param name="method">The invoked method.</param>
    /// <param name="index">The generic contract argument index, or <c>-1</c> when the receiver supplies the contract.</param>
    /// <returns><see langword="true" /> when <paramref name="method" /> is a recognized producer method.</returns>
    public static bool IsProducerMethod(this IMethodSymbol method, out int index)
    {
        if (_producerAssemblies.Contains(method.ContainingAssembly.Name))
            return _producerMethods.TryGetValue($"{method.ContainingNamespace}.{method.ContainingType.Name}.{method.Name}", out index);

        index = -1;
        return false;
    }

    /// <summary>Resolves a concrete message contract from a class, interface, or singly constrained type parameter.</summary>
    /// <param name="typeArgument">The producer's message type.</param>
    /// <param name="contractType">The class or interface that defines the message contract.</param>
    /// <returns><see langword="true" /> when a contract type can be resolved.</returns>
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

    /// <summary>Determines whether a symbol represents <see cref="ImmutableArray{T}" />.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="typeArgument">The immutable array element type.</param>
    /// <returns><see langword="true" /> for a closed <see cref="ImmutableArray{T}" /> type.</returns>
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

    /// <summary>Determines whether a symbol represents <see cref="ICollection{T}" />.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="typeArgument">The collection element type.</param>
    /// <returns><see langword="true" /> for a closed <see cref="ICollection{T}" /> type.</returns>
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

    /// <summary>Determines whether a symbol represents <see cref="IEnumerable{T}" />.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="typeArgument">The enumerable element type.</param>
    /// <returns><see langword="true" /> for a closed <see cref="IEnumerable{T}" /> type.</returns>
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

    /// <summary>Recognizes mutable and read-only generic list shapes.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="typeArgument">The list element type.</param>
    /// <returns><see langword="true" /> for <see cref="List{T}" />, <see cref="IList{T}" />, or <see cref="IReadOnlyList{T}" />.</returns>
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

    /// <summary>Gets serialized properties declared by a contract and its inherited interfaces.</summary>
    /// <param name="contractType">The message contract.</param>
    /// <returns>Readable public instance properties, de-duplicated by exact name.</returns>
    public static List<IPropertySymbol> GetContractProperties(this ITypeSymbol contractType)
    {
        var contractTypes = new List<ITypeSymbol> { contractType };

        contractTypes.AddRange(contractType.AllInterfaces);

        return contractTypes.SelectMany(i => i.GetMembers().OfType<IPropertySymbol>().Where(property =>
                property.DeclaredAccessibility == Accessibility.Public
                && !property.IsStatic
                && property.GetMethod != null
                && property.Parameters.IsEmpty))
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>Recognizes mutable and read-only generic dictionary shapes.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="keyType">The dictionary key type.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns><see langword="true" /> for a supported two-argument dictionary type.</returns>
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

    /// <summary>Extracts the value type from <see cref="Nullable{T}" />.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="typeArgument">The underlying value type.</param>
    /// <returns><see langword="true" /> when <paramref name="type" /> is nullable.</returns>
    public static bool IsNullable(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (type is INamedTypeSymbol nullableType &&
            nullableType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            nullableType.IsGenericType &&
            nullableType.TypeArguments.Length == 1)
        {
            typeArgument = nullableType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    /// <summary>Extracts the element type from an array symbol.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="elementType">The array element type.</param>
    /// <returns><see langword="true" /> when <paramref name="type" /> is an array.</returns>
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

    /// <summary>Extracts the value type represented by an initializer variable.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="inVarType">The initializer variable's value type.</param>
    /// <returns><see langword="true" /> when the type implements the initializer-variable contract.</returns>
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

    /// <summary>Determines whether a producer method returns <see cref="Task" /> or <see cref="Task{TResult}" />.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <returns><see langword="true" /> when its return type is a task.</returns>
    public static bool ReturnsTask(this IMethodSymbol method)
    {
        return method.ReturnType.Name == nameof(Task) && method.ReturnType.ContainingNamespace.ToString() == TaskNamespace;
    }

    /// <summary>Enumerates a type's interface identity followed by all inherited interfaces.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The interface itself when applicable and every interface it inherits.</returns>
    public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol type)
    {
        ImmutableArray<INamedTypeSymbol> allInterfaces = type.AllInterfaces;
        if (type is INamedTypeSymbol { TypeKind: TypeKind.Interface } namedType)
        {
            var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1) { namedType };
            result.AddRange(allInterfaces);
            return result;
        }

        return allInterfaces;
    }

    /// <summary>Enumerates a type followed by its base-type chain.</summary>
    /// <param name="type">The first type in the chain.</param>
    /// <returns>The type and each successive base type.</returns>
    public static IEnumerable<ITypeSymbol> GetAllTypes(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    /// <summary>Determines whether a Roslyn type kind can define a structural message contract.</summary>
    /// <param name="typeKind">The type kind to inspect.</param>
    /// <returns><see langword="true" /> for classes and interfaces.</returns>
    public static bool IsClassOrInterface(this TypeKind typeKind)
    {
        return typeKind == TypeKind.Interface || typeKind == TypeKind.Class;
    }

    /// <summary>Determines whether a type implements an exact interface symbol.</summary>
    /// <param name="symbol">The candidate implementation.</param>
    /// <param name="type">The interface to match.</param>
    /// <returns><see langword="true" /> when the interface is implemented.</returns>
    public static bool ImplementsInterface(this ITypeSymbol symbol, ITypeSymbol type)
    {
        return symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, type));
    }

    /// <summary>Determines whether a type is or derives from an exact type symbol.</summary>
    /// <param name="symbol">The candidate derived type.</param>
    /// <param name="type">The required base type.</param>
    /// <returns><see langword="true" /> when the type occurs in the base-type chain.</returns>
    public static bool InheritsFromType(this ITypeSymbol symbol, ITypeSymbol type)
    {
        return GetAllTypes(symbol).Any(x => SymbolEqualityComparer.Default.Equals(x, type));
    }

    /// <summary>Determines whether a type is, derives from, or implements an exact type symbol.</summary>
    /// <param name="type">The candidate type.</param>
    /// <param name="otherType">The required base type or interface.</param>
    /// <returns><see langword="true" /> when the required type is present.</returns>
    public static bool ImplementsType(this ITypeSymbol type, ITypeSymbol otherType)
    {
        IEnumerable<ITypeSymbol> types = GetAllTypes(type);
        IEnumerable<INamedTypeSymbol> interfaces = GetAllInterfaces(type);

        return types.Any(baseType => SymbolEqualityComparer.Default.Equals(baseType, otherType))
            || interfaces.Any(baseInterfaceType => SymbolEqualityComparer.Default.Equals(baseInterfaceType, otherType));
    }
}
