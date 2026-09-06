using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Configures the serializer used to produce Event Hubs events.</summary>
public static class EventHubSerializerConfigurationExtensions
{
    /// <summary>Uses the System.Text.Json message-envelope serializer.</summary>
    /// <param name="configurator">The Event Hubs rider configurator.</param>
    public static void UseJsonSerializer(this IEventHubFactoryConfigurator configurator)
    {
        configurator.AddSerializer(new SystemTextJsonMessageSerializerFactory());
    }

    /// <summary>Uses the System.Text.Json raw-message serializer.</summary>
    /// <param name="configurator">The Event Hubs rider configurator.</param>
    public static void UseRawJsonSerializer(this IEventHubFactoryConfigurator configurator)
    {
        configurator.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
    }
}
