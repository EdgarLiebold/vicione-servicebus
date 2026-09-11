using System;
using System.Collections.Concurrent;
using System.Text;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats CLR message types as transport entity names using configurable separators.</summary>
public sealed class DefaultMessageNameFormatter :
    IMessageNameFormatter
{
    readonly ConcurrentDictionary<Type, string> _cache;
    readonly string _genericArgumentSeparator;
    readonly string _genericTypeSeparator;
    readonly bool _includeNamespace;
    readonly string _namespaceSeparator;
    readonly string _nestedTypeSeparator;

    /// <summary>Initializes a formatter that includes message namespaces.</summary>
    /// <param name="genericArgumentSeparator">The separator placed between generic arguments.</param>
    /// <param name="genericTypeSeparator">The delimiter placed before and after generic arguments.</param>
    /// <param name="namespaceSeparator">The separator placed between a namespace and its type name.</param>
    /// <param name="nestedTypeSeparator">The separator placed between declaring and nested type names.</param>
    public DefaultMessageNameFormatter(string genericArgumentSeparator, string genericTypeSeparator,
        string namespaceSeparator, string nestedTypeSeparator)
        : this(genericArgumentSeparator, genericTypeSeparator, namespaceSeparator, nestedTypeSeparator, true)
    {
    }

    /// <summary>Initializes a formatter with explicit namespace behavior.</summary>
    /// <param name="genericArgumentSeparator">The separator placed between generic arguments.</param>
    /// <param name="genericTypeSeparator">The delimiter placed before and after generic arguments.</param>
    /// <param name="namespaceSeparator">The separator placed between a namespace and its type name.</param>
    /// <param name="nestedTypeSeparator">The separator placed between declaring and nested type names.</param>
    /// <param name="includeNamespace"><see langword="true" /> to include namespaces in formatted names.</param>
    public DefaultMessageNameFormatter(string genericArgumentSeparator, string genericTypeSeparator,
        string namespaceSeparator, string nestedTypeSeparator, bool includeNamespace)
    {
        ArgumentNullException.ThrowIfNull(genericArgumentSeparator);
        ArgumentNullException.ThrowIfNull(genericTypeSeparator);
        ArgumentNullException.ThrowIfNull(namespaceSeparator);
        ArgumentNullException.ThrowIfNull(nestedTypeSeparator);

        _genericArgumentSeparator = genericArgumentSeparator;
        _genericTypeSeparator = genericTypeSeparator;
        _namespaceSeparator = namespaceSeparator;
        _nestedTypeSeparator = nestedTypeSeparator;
        _includeNamespace = includeNamespace;

        _cache = new ConcurrentDictionary<Type, string>();
    }

    /// <summary>Gets the transport entity name for a closed CLR type.</summary>
    /// <param name="type">The message type to format.</param>
    /// <returns>The cached transport entity name.</returns>
    public string GetMessageName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _cache.GetOrAdd(type, CreateMessageName);
    }

    string CreateMessageName(Type type)
    {
        if (type.ContainsGenericParameters)
            throw new ArgumentException("A type with open generic parameters cannot be used as a message name.", nameof(type));

        var sb = new StringBuilder();

        AppendMessageName(sb, type, null);
        return sb.ToString();
    }

    void AppendMessageName(StringBuilder builder, Type type, string? namespaceScope)
    {
        string? typeNamespace = type.Namespace;
        AppendNamespace(builder, typeNamespace, namespaceScope);

        if (type.IsNested)
        {
            Type declaringType = GetClosedDeclaringType(type);
            AppendMessageName(builder, declaringType, typeNamespace);
            builder.Append(_nestedTypeSeparator);
        }

        AppendTypeName(builder, type, typeNamespace);
    }

    void AppendNamespace(StringBuilder builder, string? typeNamespace, string? namespaceScope)
    {
        if (!_includeNamespace || typeNamespace is null || StringComparer.Ordinal.Equals(typeNamespace, namespaceScope))
            return;

        builder.Append(typeNamespace);
        builder.Append(_namespaceSeparator);
    }

    void AppendTypeName(StringBuilder builder, Type type, string? namespaceScope)
    {
        if (!type.IsGenericType)
        {
            builder.Append(type.Name);
            return;
        }

        string typeName = type.GetGenericTypeDefinition().Name;
        int arityIndex = typeName.IndexOf('`');
        builder.Append(arityIndex > 0 ? typeName[..arityIndex] : typeName);

        Type[] arguments = type.GetGenericArguments();
        int declaringArgumentCount = type.DeclaringType?.GetGenericArguments().Length ?? 0;
        if (declaringArgumentCount == arguments.Length)
            return;

        builder.Append(_genericTypeSeparator);
        AppendGenericArguments(builder, arguments, declaringArgumentCount, namespaceScope);
        builder.Append(_genericTypeSeparator);
    }

    void AppendGenericArguments(StringBuilder builder, Type[] arguments, int firstArgument, string? namespaceScope)
    {
        for (int index = firstArgument; index < arguments.Length; index++)
        {
            if (index > firstArgument)
                builder.Append(_genericArgumentSeparator);

            AppendMessageName(builder, arguments[index], namespaceScope);
        }
    }

    static Type GetClosedDeclaringType(Type nestedType)
    {
        Type declaringType = nestedType.DeclaringType
            ?? throw new InvalidOperationException($"Nested type '{nestedType}' does not expose a declaring type.");
        if (!declaringType.ContainsGenericParameters)
            return declaringType;

        int declaringArgumentCount = declaringType.GetGenericArguments().Length;
        Type[] nestedArguments = nestedType.GetGenericArguments();
        if (declaringArgumentCount > nestedArguments.Length)
            throw new InvalidOperationException($"Nested type '{nestedType}' does not expose its declaring type arguments.");

        return declaringType.MakeGenericType(nestedArguments[..declaringArgumentCount]);
    }
}
