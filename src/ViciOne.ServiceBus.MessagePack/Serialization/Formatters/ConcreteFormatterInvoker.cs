using MessagePack;

namespace ViciOne.ServiceBus.MessagePack.Serialization.Formatters;

/// <summary>Holds compiled delegates that resolve and invoke one concrete MessagePack formatter.</summary>
/// <typeparam name="TContract">The interface contract accepted and returned by the delegates.</typeparam>
/// <param name="getFormatter">Resolves the concrete formatter from a resolver.</param>
/// <param name="serialize">Writes the concrete value.</param>
/// <param name="deserialize">Reads the concrete value.</param>
sealed class ConcreteFormatterInvoker<TContract>(
    Func<IFormatterResolver, object> getFormatter,
    ConcreteFormatterInvoker<TContract>.SerializeValue serialize,
    ConcreteFormatterInvoker<TContract>.DeserializeValue deserialize)
{
    internal delegate void SerializeValue(
        object formatter,
        ref MessagePackWriter writer,
        TContract value,
        MessagePackSerializerOptions options);

    internal delegate TContract DeserializeValue(
        object formatter,
        ref MessagePackReader reader,
        MessagePackSerializerOptions options);

    /// <summary>Gets the compiled concrete-formatter lookup.</summary>
    public Func<IFormatterResolver, object> GetFormatter { get; } = getFormatter;

    /// <summary>Gets the compiled serialization operation.</summary>
    public SerializeValue Serialize { get; } = serialize;

    /// <summary>Gets the compiled deserialization operation.</summary>
    public DeserializeValue Deserialize { get; } = deserialize;
}
