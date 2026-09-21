using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Formats .NET message types as Amazon SQS and Amazon SNS entity-name segments.</summary>
/// <remarks>
/// Names whose identifiers contain a configured separator character use a reserved canonical
/// encoding to avoid separator-induced name collisions. Custom separators apply to names that
/// do not need this encoding. Excluding namespaces can still make different contracts share a name.
/// </remarks>
public class AmazonSqsMessageNameFormatter :
    IMessageNameFormatter
{
    readonly ConcurrentDictionary<Type, string> _cache;
    readonly string _genericArgumentSeparator;
    readonly string _genericTypeSeparator;
    readonly bool _includeNamespace;
    readonly string _namespaceSeparator;
    readonly string _nestedTypeSeparator;
    readonly string _reservedCharacters;

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
        if (_genericArgumentSeparator.Length == 0 || _genericTypeSeparator.Length == 0
            || _namespaceSeparator.Length == 0 || _nestedTypeSeparator.Length == 0)
            throw new ArgumentException("Message-name separators cannot be empty.");

        string[] activeSeparators = includeNamespace
            ? [_genericArgumentSeparator, _genericTypeSeparator, _namespaceSeparator, _nestedTypeSeparator]
            : [_genericArgumentSeparator, _genericTypeSeparator, _nestedTypeSeparator];
        if (activeSeparators.Distinct(StringComparer.Ordinal).Count() != activeSeparators.Length)
            throw new ArgumentException("Message-name separators must be distinct.");

        _reservedCharacters = _genericArgumentSeparator + _genericTypeSeparator + _namespaceSeparator + _nestedTypeSeparator;
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
        if (type.ContainsGenericParameters)
            throw new ArgumentException("An open generic type cannot be used as a message name", nameof(type));

        var sb = new StringBuilder("");
        bool canonical = RequiresCanonicalEncoding(type);
        // Keep legacy names when identifiers cannot be mistaken for a separator. The leading
        // hyphen reserves canonical names; _d, _n, and _u encode distinct boundaries.
        if (canonical)
            sb.Append('-');

        string name = GetMessageName(sb, type, null, canonical);
        if (!canonical || name.Length <= 256)
            return name;

        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)));
        return "--" + name.Substring(1, 256 - digest.Length - 3) + "-" + digest;
    }

    bool RequiresCanonicalEncoding(Type type)
    {
        if (type.IsGenericParameter)
            return false;

        if (ContainsReservedCharacter(type.Name) || ContainsReservedCharacter(type.Namespace))
            return true;

        if (type.DeclaringType is { } declaringType && RequiresCanonicalEncoding(declaringType))
            return true;

        return type.IsGenericType && type.GetGenericArguments().Any(RequiresCanonicalEncoding);
    }

    bool ContainsReservedCharacter(string? identifier)
    {
        return identifier?.Any(character => character is '_' or '-' || _reservedCharacters.Contains(character)) == true;
    }

    string GetMessageName(StringBuilder sb, Type type, string? scope, bool canonical)
    {
        if (type.IsGenericParameter)
            return "";

        string? ns = FormatNamespace(type.Namespace, canonical);
        if (ns != null && _includeNamespace && !ns.Equals(scope))
        {
            sb.Append(ns);
            sb.Append(canonical ? "-" : _namespaceSeparator);
        }

        if (type is { IsNested: true, DeclaringType: not null })
        {
            GetMessageName(sb, type.DeclaringType, ns, canonical);
            sb.Append(canonical ? "_n" : _nestedTypeSeparator);
        }

        AppendTypeName(sb, type, ns, canonical);

        return sb.ToString();
    }

    string? FormatNamespace(string? source, bool canonical)
    {
        if (source is null)
            return null;

        return canonical
            ? source.Replace("_", "_u").Replace("-", "_h").Replace(".", "_d")
            : source.Replace(".", _nestedTypeSeparator);
    }

    void AppendTypeName(StringBuilder sb, Type type, string? scope, bool canonical)
    {
        if (!type.IsGenericType)
        {
            sb.Append(canonical ? type.Name.Replace("_", "_u").Replace("-", "_h") : type.Name);
            return;
        }

        var name = type.GetGenericTypeDefinition().Name;
        // The CLR generic arity suffix is not part of an Amazon SQS entity name.
        var index = name.IndexOf('`');
        if (index > 0)
            name = name.Remove(index);

        sb.Append(canonical ? name.Replace("_", "_u").Replace("-", "_h") : name);
        sb.Append(canonical ? "--" : _genericTypeSeparator);

        Type[] arguments = type.GetGenericArguments();
        for (var i = 0; i < arguments.Length; i++)
        {
            if (i > 0)
                sb.Append(canonical ? "__" : _genericArgumentSeparator);

            GetMessageName(sb, arguments[i], scope, canonical);
        }

        sb.Append(canonical ? "--" : _genericTypeSeparator);
    }
}
