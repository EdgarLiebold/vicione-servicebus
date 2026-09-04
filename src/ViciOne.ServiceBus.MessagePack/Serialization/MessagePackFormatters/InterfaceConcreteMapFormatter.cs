using MessagePack;
using MessagePack.Formatters;

namespace ViciOne.ServiceBus.Serialization.MessagePackFormatters;

/// <summary>
/// Provides an interface concrete map formatter implementation.
/// </summary>
/// <typeparam name="TInterface">The t interface type.</typeparam>
/// <typeparam name="TImplementation">The t implementation type.</typeparam>
public class InterfaceConcreteMapFormatter<TInterface, TImplementation> :
    IMessagePackFormatter<TInterface>
    where TImplementation : TInterface
{
    /// <summary>
    /// Performs the serialize operation.
    /// </summary>
    /// <param name="writer">The writer value.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The options value.</param>
    public virtual void Serialize(ref MessagePackWriter writer, TInterface value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<TImplementation> innerFormatter = options.Resolver.GetFormatterWithVerify<TImplementation>();

        if (value is TImplementation implementation)
            innerFormatter.Serialize(ref writer, implementation, options);
        else
            innerFormatter.Serialize(ref writer, (TImplementation)value!, options);
    }

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="reader">The reader value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public virtual TInterface Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<TImplementation> innerFormatter = options.Resolver.GetFormatterWithVerify<TImplementation>();

        return innerFormatter.Deserialize(ref reader, options);
    }
}
