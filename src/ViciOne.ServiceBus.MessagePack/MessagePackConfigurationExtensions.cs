using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessagePack;

/// <summary>Registers MessagePack envelope serialization with bus and receive-endpoint configurators.</summary>
public static class MessagePackConfigurationExtensions
{
    /// <summary>Registers MessagePack for both serialization and deserialization on a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="isDefault">
    /// <see langword="true" /> to select MessagePack for outgoing messages and incoming messages
    /// without a content type; <see langword="false" /> to register it without changing either default.
    /// </param>
    public static void UseMessagePackSerializer(this IReceiveEndpointConfigurator configurator, bool isDefault = true)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        var factory = new MessagePackSerializerFactory();

        configurator.AddSerializer(factory, isDefault);
        configurator.AddDeserializer(factory, isDefault);
    }

    /// <summary>Registers MessagePack for both serialization and deserialization on the bus.</summary>
    /// <param name="configurator">The bus to configure.</param>
    /// <param name="isDefault">
    /// <see langword="true" /> to select MessagePack for outgoing messages and incoming messages
    /// without a content type; <see langword="false" /> to register it without changing either default.
    /// </param>
    public static void UseMessagePackSerializer(this IBusFactoryConfigurator configurator, bool isDefault = true)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        var factory = new MessagePackSerializerFactory();

        configurator.AddSerializer(factory, isDefault);
        configurator.AddDeserializer(factory, isDefault);
    }

    /// <summary>Registers MessagePack deserialization without adding an outgoing serializer.</summary>
    /// <param name="configurator">The bus to configure.</param>
    /// <param name="isDefault">
    /// <see langword="true" /> to use MessagePack when an incoming message has no content type;
    /// otherwise, <see langword="false" />.
    /// </param>
    public static void UseMessagePackDeserializer(this IBusFactoryConfigurator configurator, bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.AddDeserializer(new MessagePackSerializerFactory(), isDefault);
    }
}
