using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides a message urn implementation.
/// </summary>
public class MessageUrn :
    Uri
{
    /// <summary>
    /// Defines the prefix value.
    /// </summary>
    public const string Prefix = "urn:message:";

    static readonly ConcurrentDictionary<Type, Cached> _cache = new ConcurrentDictionary<Type, Cached>();

    MessageUrn(string uriString)
        : base(uriString)
    {
    }

    /// <summary>
    /// Performs the for type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static MessageUrn ForType<T>()
    {
        return MessageUrnCache<T>.Urn;
    }

    /// <summary>
    /// Performs the for type string operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static string ForTypeString<T>()
    {
        return MessageUrnCache<T>.UrnString;
    }

    /// <summary>
    /// Performs the for type operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static MessageUrn ForType(Type type)
    {
        ValidateType(type);

        return _cache.GetOrAdd(type, ValueFactory).Urn;
    }

    /// <summary>
    /// Performs the for type string operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static string ForTypeString(Type type)
    {
        ValidateType(type);

        return _cache.GetOrAdd(type, ValueFactory).UrnString;
    }

    static void ValidateType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.ContainsGenericParameters)
            throw new ArgumentException("A message type may not contain generic parameters", nameof(type));
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


    /// <summary>
    /// Deconstructs this value into its components.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="ns">The ns value.</param>
    /// <param name="assemblyName">The assembly name value.</param>
    public void Deconstruct(out string? name, out string? ns, out string? assemblyName)
    {
        name = null;
        ns = null;
        assemblyName = null;

        if (Segments.Length > 0)
        {
            var names = Segments[0].Split(':');
            if (string.Compare(names[0], "message", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (names.Length == 2)
                    name = names[1];
                else if (names.Length == 3)
                {
                    name = names[2];
                    ns = names[1];
                }
                else if (names.Length >= 4)
                {
                    name = names[2];
                    ns = names[1];
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

            //remove `1
            var index = name.IndexOf('`');
            if (index > 0)
                name = name.Remove(index);
            //

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


    class Cached<T> :
        Cached
    {
        public MessageUrn Urn => MessageUrnCache<T>.Urn;
        public string UrnString => MessageUrnCache<T>.UrnString;
    }
}
