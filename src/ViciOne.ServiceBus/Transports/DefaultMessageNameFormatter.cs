using System;
using System.Collections.Concurrent;
using System.Text;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a default message name formatter implementation.
/// </summary>
public class DefaultMessageNameFormatter :
    IMessageNameFormatter
{
    readonly ConcurrentDictionary<Type, string> _cache;
    readonly string _genericArgumentSeparator;
    readonly string _genericTypeSeparator;
    readonly bool _includeNamespace;
    readonly string _namespaceSeparator;
    readonly string _nestedTypeSeparator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="genericArgumentSeparator">The generic argument separator value.</param>
    /// <param name="genericTypeSeparator">The generic type separator value.</param>
    /// <param name="namespaceSeparator">The namespace separator value.</param>
    /// <param name="nestedTypeSeparator">The nested type separator value.</param>
    public DefaultMessageNameFormatter(string genericArgumentSeparator, string genericTypeSeparator,
        string namespaceSeparator, string nestedTypeSeparator)
        : this(genericArgumentSeparator, genericTypeSeparator, namespaceSeparator, nestedTypeSeparator, true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="genericArgumentSeparator">The generic argument separator value.</param>
    /// <param name="genericTypeSeparator">The generic type separator value.</param>
    /// <param name="namespaceSeparator">The namespace separator value.</param>
    /// <param name="nestedTypeSeparator">The nested type separator value.</param>
    /// <param name="includeNamespace">The include namespace value.</param>
    public DefaultMessageNameFormatter(string genericArgumentSeparator, string genericTypeSeparator,
        string namespaceSeparator, string nestedTypeSeparator, bool includeNamespace)
    {
        _genericArgumentSeparator = genericArgumentSeparator;
        _genericTypeSeparator = genericTypeSeparator;
        _namespaceSeparator = namespaceSeparator;
        _nestedTypeSeparator = nestedTypeSeparator;
        _includeNamespace = includeNamespace;

        _cache = new ConcurrentDictionary<Type, string>();
    }

    /// <summary>
    /// Gets message name.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetMessageName(Type type)
    {
        return _cache.GetOrAdd(type, CreateMessageName);
    }

    string CreateMessageName(Type type)
    {
        if (type.IsGenericTypeDefinition)
            throw new ArgumentException("An open generic type cannot be used as a message name");

        var sb = new StringBuilder("");

        return GetMessageName(sb, type, null);
    }

    string GetMessageName(StringBuilder sb, Type type, string? scope)
    {
        if (type.IsGenericParameter)
            return "";

        var ns = type.Namespace;
        if (ns != null && _includeNamespace && !ns.Equals(scope))
        {
            sb.Append(ns);
            sb.Append(_namespaceSeparator);
        }

        if (type.IsNested)
        {
            GetMessageName(sb, type.DeclaringType
                ?? throw new InvalidOperationException($"Nested type '{type}' does not expose a declaring type."), ns);
            sb.Append(_nestedTypeSeparator);
        }

        if (type.IsGenericType)
        {
            var name = type.GetGenericTypeDefinition().Name;

            //remove `1
            var index = name.IndexOf('`');
            if (index > 0)
                name = name.Remove(index);

            sb.Append(name);
            sb.Append(_genericTypeSeparator);

            Type[] arguments = type.GetGenericArguments();
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                    sb.Append(_genericArgumentSeparator);

                GetMessageName(sb, arguments[i], ns);
            }

            sb.Append(_genericTypeSeparator);
        }
        else
            sb.Append(type.Name);

        return sb.ToString();
    }
}
