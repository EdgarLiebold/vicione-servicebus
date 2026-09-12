using MessagePack;

namespace ViciOne.ServiceBus.MessagePack.Serialization.Formatters;

/// <summary>Holds compiled delegates that resolve and invoke one concrete MessagePack formatter.</summary>
/// <typeparam name="TContract">The interface contract accepted and returned by the delegates.</typeparam>
sealed class ConcreteFormatterInvoker<TContract>
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

    /// <summary>Creates a complete set of concrete-formatter operations.</summary>
    /// <param name="getFormatter">Resolves the concrete formatter from a resolver.</param>
    /// <param name="serialize">Writes a concrete value through the resolved formatter.</param>
    /// <param name="deserialize">Reads a concrete value through the resolved formatter.</param>
    public ConcreteFormatterInvoker(
        Func<IFormatterResolver, object> getFormatter,
        SerializeValue serialize,
        DeserializeValue deserialize)
    {
        GetFormatter = getFormatter ?? throw new ArgumentNullException(nameof(getFormatter));
        Serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
        Deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
    }

    /// <summary>Gets the compiled concrete-formatter lookup.</summary>
    public Func<IFormatterResolver, object> GetFormatter { get; }

    /// <summary>Gets the compiled serialization operation.</summary>
    public SerializeValue Serialize { get; }

    /// <summary>Gets the compiled deserialization operation.</summary>
    public DeserializeValue Deserialize { get; }
}
