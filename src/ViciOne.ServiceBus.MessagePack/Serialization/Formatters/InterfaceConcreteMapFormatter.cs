using MessagePack;
using MessagePack.Formatters;

namespace ViciOne.ServiceBus.MessagePack.Serialization.Formatters;

/// <summary>Maps an interface contract to one declared concrete MessagePack representation.</summary>
/// <typeparam name="TInterface">The interface exposed by the message contract.</typeparam>
/// <typeparam name="TImplementation">The concrete representation used on the wire.</typeparam>
internal sealed class InterfaceConcreteMapFormatter<TInterface, TImplementation> :
    IMessagePackFormatter<TInterface>
    where TInterface : class
    where TImplementation : class, TInterface
{
    /// <summary>Serializes an interface value through the formatter registered for its declared implementation.</summary>
    /// <param name="writer">The MessagePack writer that receives the concrete value.</param>
    /// <param name="value">The value, which must be assignable to <typeparamref name="TImplementation"/>.</param>
    /// <param name="options">The serializer options whose resolver supplies the concrete formatter.</param>
    public void Serialize(ref MessagePackWriter writer, TInterface value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<TImplementation> innerFormatter = options.Resolver.GetFormatterWithVerify<TImplementation>();

        if (value is null)
        {
            innerFormatter.Serialize(ref writer, null!, options);
            return;
        }

        if (value is not TImplementation implementation)
        {
            throw new MessagePackSerializationException(
                $"The runtime value '{value.GetType()}' is not assignable to the declared MessagePack implementation '{typeof(TImplementation)}'.");
        }

        innerFormatter.Serialize(ref writer, implementation, options);
    }

    /// <summary>Deserializes the declared implementation and returns it through the interface contract.</summary>
    /// <param name="reader">The MessagePack reader positioned at the concrete value.</param>
    /// <param name="options">The serializer options whose resolver supplies the concrete formatter.</param>
    /// <returns>The deserialized concrete instance exposed as <typeparamref name="TInterface"/>.</returns>
    public TInterface Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<TImplementation> innerFormatter = options.Resolver.GetFormatterWithVerify<TImplementation>();

        return innerFormatter.Deserialize(ref reader, options);
    }
}
