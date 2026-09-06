using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ViciOne.ServiceBus.Internals;

internal class TypeNameFormatter
{
    readonly ConditionalWeakTable<Type, CachedName> _cache;
    readonly string _genericArgumentSeparator;
    readonly string _genericClose;
    readonly string _genericOpen;
    readonly string _namespaceSeparator;
    readonly string _nestedTypeSeparator;

    public TypeNameFormatter()
        : this(",", "<", ">", ".", "+")
    {
    }

    public TypeNameFormatter(string genericArgumentSeparator, string genericOpen, string genericClose,
        string namespaceSeparator, string nestedTypeSeparator)
    {
        _genericArgumentSeparator = genericArgumentSeparator;
        _genericOpen = genericOpen;
        _genericClose = genericClose;
        _namespaceSeparator = namespaceSeparator;
        _nestedTypeSeparator = nestedTypeSeparator;

        _cache = new ConditionalWeakTable<Type, CachedName>();
    }

    public string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _cache.GetValue(type, value => new CachedName(FormatTypeName(value))).Value;
    }

    string FormatTypeName(Type type)
    {
        var sb = new StringBuilder("");

        return FormatTypeName(sb, type, null);
    }

    string FormatTypeName(StringBuilder sb, Type type, string? scope)
    {
        if (type.IsGenericParameter)
            return "";

        if (type.Namespace != null)
        {
            var ns = type.Namespace;
            if (!ns.Equals(scope))
            {
                sb.Append(ns);
                sb.Append(_namespaceSeparator);
            }
        }

        if (type.IsNested && type.DeclaringType != null)
        {
            FormatTypeName(sb, type.DeclaringType, type.Namespace);
            sb.Append(_nestedTypeSeparator);
        }

        if (type.IsGenericType)
        {
            var name = type.GetGenericTypeDefinition().Name;

            // The CLR generic arity suffix is not part of a formatted type name.
            var index = name.IndexOf('`');
            if (index > 0)
                name = name.Remove(index);

            sb.Append(name);
            sb.Append(_genericOpen);
            Type[] arguments = type.GetGenericArguments();
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                    sb.Append(_genericArgumentSeparator);

                FormatTypeName(sb, arguments[i], type.Namespace);
            }

            sb.Append(_genericClose);
        }
        else
            sb.Append(type.Name);

        return sb.ToString();
    }


    sealed record CachedName(string Value);
}
