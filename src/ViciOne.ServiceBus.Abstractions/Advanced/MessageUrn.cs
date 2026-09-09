using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Builds and parses canonical URNs for message contract types.</summary>
public sealed class MessageUrn :
    Uri
{
    /// <summary>Identifies canonical message-contract URNs.</summary>
    public const string Prefix = "urn:message:";

    static readonly ConditionalWeakTable<Type, Cached> _cache = new();

    MessageUrn(string uriString)
        : base(uriString)
    {
    }

    /// <summary>Gets the cached canonical URN for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The canonical message URN.</returns>
    public static MessageUrn ForType<T>()
    {
        ValidateType(typeof(T));
        return MessageUrnCache<T>.Urn;
    }

    /// <summary>Gets the cached canonical URN text for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The canonical message URN text.</returns>
    public static string ForTypeString<T>()
    {
        ValidateType(typeof(T));
        return MessageUrnCache<T>.UrnString;
    }

    /// <summary>Gets the cached canonical URN for a runtime message contract.</summary>
    /// <param name="type">The closed runtime message contract type.</param>
    /// <returns>The canonical message URN.</returns>
    public static MessageUrn ForType(Type type)
    {
        ValidateType(type);

        return _cache.GetValue(type, ValueFactory).Urn;
    }

    /// <summary>Gets the cached canonical URN text for a runtime message contract.</summary>
    /// <param name="type">The closed runtime message contract type.</param>
    /// <returns>The canonical message URN text.</returns>
    public static string ForTypeString(Type type)
    {
        ValidateType(type);

        return _cache.GetValue(type, ValueFactory).UrnString;
    }

    static void ValidateType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.ContainsGenericParameters)
            throw new ArgumentException("A message contract cannot contain unbound generic parameters.", nameof(type));

        if (!type.IsClass && !type.IsInterface)
            throw new ArgumentException("A message contract must be a reference type.", nameof(type));

        if (typeof(Delegate).IsAssignableFrom(type))
            throw new ArgumentException("A delegate cannot be used as a message contract.", nameof(type));
    }

    static Cached ValueFactory(Type type)
    {
        return Activation.Activate(type, new Factory());
    }


    readonly struct Factory :
        IActivationType<Cached>
    {
        public Cached ActivateType<T>()
            where T : class
        {
            return new Cached<T>();
        }
    }


    /// <summary>Parses the canonical message name, namespace, and optional assembly scope from this URN.</summary>
    /// <param name="name">Receives the message-contract name when this is a message URN.</param>
    /// <param name="namespaceName">Receives the message-contract namespace when encoded by the URN.</param>
    /// <param name="assemblyName">Receives the assembly scope when encoded by the URN.</param>
    public void Deconstruct(out string? name, out string? namespaceName, out string? assemblyName)
    {
        name = null;
        namespaceName = null;
        assemblyName = null;

        if (Segments.Length > 0)
        {
            var names = Segments[0].Split(':');
            if (string.Equals(names[0], "message", StringComparison.OrdinalIgnoreCase))
            {
                if (names.Length == 2)
                    name = names[1];
                else if (names.Length == 3)
                {
                    name = names[2];
                    namespaceName = names[1];
                }
                else if (names.Length >= 4)
                {
                    name = names[2];
                    namespaceName = names[1];
                    assemblyName = names[3];
                }
            }
        }
    }

    static string GetUrnForType(Type type)
    {
        return GetMessageName(type, true);
    }

    static string GetMessageName(Type type, bool includeScope)
    {
        var messageName = GetMessageNameFromAttribute(type);

        return string.IsNullOrWhiteSpace(messageName)
            ? GetMessageNameFromType(new StringBuilder(Prefix), type, includeScope)
            : messageName!;
    }

    static string? GetMessageNameFromAttribute(Type? type)
    {
        if (type is { IsArray: true, HasElementType: true })
        {
            var elementType = type.GetElementType();
            var elementName = GetMessageNameFromAttribute(elementType);

            if (!string.IsNullOrWhiteSpace(elementName))
                return elementName + "[]";
        }

        return type?.GetCustomAttribute<MessageUrnAttribute>()?.Urn.ToString();
    }

    static string GetMessageNameFromType(StringBuilder sb, Type type, bool includeScope)
    {
        if (type.IsGenericParameter)
            return string.Empty;

        var ns = type.Namespace;
        if (includeScope && ns != null)
        {
            sb.Append(ns);

            sb.Append(':');
        }

        if (type is { IsNested: true, DeclaringType: { } })
        {
            GetMessageNameFromType(sb, type.DeclaringType, false);
            sb.Append('+');
        }

        if (type.IsGenericType)
        {
            var name = type.GetGenericTypeDefinition().Name;

            // The CLR generic arity suffix is not part of a message URN.
            var index = name.IndexOf('`');
            if (index > 0)
                name = name.Remove(index);
            sb.Append(name);
            sb.Append('[');

            Type[] arguments = type.GetGenericArguments();
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');

                sb.Append('[');
                GetMessageNameFromType(sb, arguments[i], true);
                sb.Append(']');
            }

            sb.Append(']');
        }
        else
            sb.Append(type.Name);

        return sb.ToString();
    }


    static class MessageUrnCache<T>
    {
        internal static readonly MessageUrn Urn;
        internal static readonly string UrnString;

        static MessageUrnCache()
        {
            Urn = new MessageUrn(GetUrnForType(typeof(T)));
            UrnString = Urn.ToString();
        }
    }


    interface Cached
    {
        MessageUrn Urn { get; }
        string UrnString { get; }
    }


    sealed class Cached<T> :
        Cached
    {
        public MessageUrn Urn => MessageUrnCache<T>.Urn;
        public string UrnString => MessageUrnCache<T>.UrnString;
    }
}
