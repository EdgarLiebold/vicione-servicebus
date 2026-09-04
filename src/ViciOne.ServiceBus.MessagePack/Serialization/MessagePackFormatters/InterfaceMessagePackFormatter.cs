using System;
using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization.MessagePackFormatters;

delegate void SerializeDelegate<TInterface>(object formatter, ref MessagePackWriter writer, TInterface value,
    MessagePackSerializerOptions options);


delegate TInterface DeserializeDelegate<out TInterface>(object formatter, ref MessagePackReader reader,
    MessagePackSerializerOptions options);


/// <summary>
/// Serializes an interface typed message through the formatter of its concrete type.
/// <para>
/// The invoker and the lookup of the concrete formatter are compiled once per concrete type and reached
/// through delegates, rather than per serialize and deserialize call; see
/// <see cref="ConcreteFormatterCache{TInterface}" /> for how the entries are bounded and why.
/// </para>
/// </summary>
public class InterfaceMessagePackFormatter<TInterface> :
    IMessagePackFormatter<TInterface>
{
    static readonly Type _declaredConcreteType = TypeMetadataCache.GetImplementationType(typeof(TInterface));

    // One cache per closed interface type rather than per formatter instance: a compiled invoker is
    // valid for the whole process, and the resolver is free to hand out more than one formatter. The
    // entries are bounded by the lifetime of their key, not by this being static.
    static readonly ConcreteFormatterCache<TInterface> _cache = new();

    /// <summary>
    /// How many entries this closed formatter has actually compiled.
    /// </summary>
    internal static int CompiledInvokerCount => _cache.CompiledCount;

    public void Serialize(ref MessagePackWriter writer, TInterface value, MessagePackSerializerOptions options)
    {
        // A value that is still typed as an interface has no formatter of its own; the type declared for
        // the contract is the one that can be written.
        var runtimeType = value?.GetType();
        var concreteType = runtimeType != null && !runtimeType.IsInterface
            ? runtimeType
            : _declaredConcreteType;

        var access = _cache.Get(concreteType);

        access.Serialize(access.GetFormatter(options.Resolver), ref writer, value, options);
    }

    public TInterface Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var access = _cache.Get(_declaredConcreteType);

        return access.Deserialize(access.GetFormatter(options.Resolver), ref reader, options);
    }
}
