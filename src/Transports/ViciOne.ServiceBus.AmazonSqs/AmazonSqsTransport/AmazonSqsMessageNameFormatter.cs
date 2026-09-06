using System;
using System.Collections.Concurrent;
using System.Text;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Formats .NET message types as Amazon SQS and Amazon SNS entity-name segments.</summary>
public class AmazonSqsMessageNameFormatter :
    IMessageNameFormatter
{
    readonly ConcurrentDictionary<Type, string> _cache;
    readonly string _genericArgumentSeparator;
    readonly string _genericTypeSeparator;
    readonly bool _includeNamespace;
    readonly string _namespaceSeparator;
    readonly string _nestedTypeSeparator;

    /// <summary>Creates a formatter that includes namespaces and uses optional custom separators.</summary>
    /// <param name="genericArgumentSeparator">The separator between generic arguments, or <c>__</c> by default.</param>
    /// <param name="genericTypeSeparator">The delimiter around generic arguments, or <c>--</c> by default.</param>
    /// <param name="namespaceSeparator">The separator between namespace and type, or <c>-</c> by default.</param>
    /// <param name="nestedTypeSeparator">The separator for namespace segments and nested types, or <c>_</c> by default.</param>
    public AmazonSqsMessageNameFormatter(string? genericArgumentSeparator = null, string? genericTypeSeparator = null,
        string? namespaceSeparator = null, string? nestedTypeSeparator = null)
        : this(true, genericArgumentSeparator, genericTypeSeparator, namespaceSeparator, nestedTypeSeparator)
    {
    }

    /// <summary>Creates a formatter with configurable namespace inclusion and separators.</summary>
    /// <param name="includeNamespace">Whether to include the declaring namespace.</param>
    /// <param name="genericArgumentSeparator">The separator between generic arguments, or <c>__</c> by default.</param>
    /// <param name="genericTypeSeparator">The delimiter around generic arguments, or <c>--</c> by default.</param>
    /// <param name="namespaceSeparator">The separator between namespace and type, or <c>-</c> by default.</param>
    /// <param name="nestedTypeSeparator">The separator for namespace segments and nested types, or <c>_</c> by default.</param>
    public AmazonSqsMessageNameFormatter(bool includeNamespace, string? genericArgumentSeparator = null,
        string? genericTypeSeparator = null, string? namespaceSeparator = null, string? nestedTypeSeparator = null)
    {
        _genericArgumentSeparator = genericArgumentSeparator ?? "__";
        _genericTypeSeparator = genericTypeSeparator ?? "--";
        _namespaceSeparator = namespaceSeparator ?? "-";
        _nestedTypeSeparator = nestedTypeSeparator ?? "_";
        _includeNamespace = includeNamespace;

        _cache = new ConcurrentDictionary<Type, string>();
    }

    /// <summary>Formats and caches a message type's AWS-compatible entity name.</summary>
    /// <param name="type">The closed message type to format.</param>
    /// <returns>The formatted message name.</returns>
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

        var ns = type.Namespace?.Replace(".", _nestedTypeSeparator);
        if (ns != null && _includeNamespace && !ns.Equals(scope))
        {
            sb.Append(ns);
            sb.Append(_namespaceSeparator);
        }

        if (type is { IsNested: true, DeclaringType: not null })
        {
            GetMessageName(sb, type.DeclaringType, ns);
            sb.Append(_nestedTypeSeparator);
        }

        if (type.IsGenericType)
        {
            var name = type.GetGenericTypeDefinition().Name;

            // The CLR generic arity suffix is not part of an Amazon SQS entity name.
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
