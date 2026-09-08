using System;
using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessagePack.Serialization.Formatters;

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
/// <typeparam name="TInterface">The interface message contract being serialized.</typeparam>
internal sealed class InterfaceMessagePackFormatter<TInterface> :
    IMessagePackFormatter<TInterface>
{
    static readonly Type _declaredConcreteType = TypeMetadataCache.GetImplementationType(typeof(TInterface));

    // One cache per closed interface type rather than per formatter instance: a compiled invoker is
    // valid for the whole process, and the resolver is free to hand out more than one formatter. The
    // entries are bounded by the lifetime of their key, not by this being static.
    static readonly ConcreteFormatterCache<TInterface> _cache = new();

    /// <summary>Gets the number of concrete-type invokers compiled for this closed interface contract.</summary>
    internal static int CompiledInvokerCount => _cache.CompiledCount;

    /// <summary>Serializes an interface message using its runtime concrete type when available.</summary>
    /// <param name="writer">The MessagePack writer that receives the concrete message.</param>
    /// <param name="value">The interface value to serialize.</param>
    /// <param name="options">The serializer options whose resolver supplies the concrete formatter.</param>
    public void Serialize(ref MessagePackWriter writer, TInterface value, MessagePackSerializerOptions options)
    {
        // A value that is still typed as an interface has no formatter of its own; the type declared for
        // the contract is the one that can be written.
        var runtimeType = value?.GetType();
        var concreteType = runtimeType != null && !runtimeType.IsInterface
            ? runtimeType
            : _declaredConcreteType;

        var access = _cache.Get(concreteType);

        access.Serialize(access.GetFormatter(options.Resolver), ref writer, value!, options);
    }

    /// <summary>Deserializes the implementation type declared for the interface contract.</summary>
    /// <param name="reader">The MessagePack reader positioned at the concrete message.</param>
    /// <param name="options">The serializer options whose resolver supplies the concrete formatter.</param>
    /// <returns>The deserialized concrete message exposed as <typeparamref name="TInterface"/>.</returns>
    public TInterface Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var access = _cache.Get(_declaredConcreteType);

        return access.Deserialize(access.GetFormatter(options.Resolver), ref reader, options);
    }
}
