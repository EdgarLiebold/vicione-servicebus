namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Creates a message deserializer for a serialization registry.</summary>
/// <returns>A new message deserializer instance.</returns>
public delegate IMessageDeserializer DeserializerFactory();
