using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides extension methods for event hub serializer configuration.
/// </summary>
public static class EventHubSerializerConfigurationExtensions
{
    /// <summary>
    /// Serialize messages using the JSON serializer
    /// </summary>
    /// <param name="configurator"></param>
    public static void UseJsonSerializer(this IEventHubFactoryConfigurator configurator)
    {
        configurator.AddSerializer(new SystemTextJsonMessageSerializerFactory());
    }

    /// <summary>
    /// Serialize messages using the raw JSON message serializer
    /// </summary>
    /// <param name="configurator"></param>
    public static void UseRawJsonSerializer(this IEventHubFactoryConfigurator configurator)
    {
        configurator.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
    }
}
