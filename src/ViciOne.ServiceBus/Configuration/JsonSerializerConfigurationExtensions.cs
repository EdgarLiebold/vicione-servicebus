using System;
using System.Linq;
using System.Text.Json;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Serialization.JsonConverters;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for json serializer configuration.</summary>
public static class JsonSerializerConfigurationExtensions
{
    /// <summary>Serialize and deserialize messages using the raw JSON message serializer.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void UseJsonSerializer(this IBusFactoryConfigurator configurator)
    {
        var factory = new SystemTextJsonMessageSerializerFactory();

        configurator.AddSerializer(factory);
        configurator.AddDeserializer(factory);
    }

    /// <summary>Deserialize messages using the raw JSON message serializer.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="isDefault">If true, set the default content type to the content type of the deserializer.</param>
    public static void UseJsonDeserializer(this IBusFactoryConfigurator configurator, bool isDefault = false)
    {
        var factory = new SystemTextJsonMessageSerializerFactory();

        configurator.AddDeserializer(factory, isDefault);
    }

    /// <summary>Serialize and deserialize messages using the raw JSON message serializer.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void UseJsonSerializer(this IReceiveEndpointConfigurator configurator)
    {
        var factory = new SystemTextJsonMessageSerializerFactory();

        configurator.AddSerializer(factory);
        configurator.AddDeserializer(factory);
    }

    /// <summary>Deserialize messages using the raw JSON message serializer.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="isDefault">If true, set the default content type to the content type of the deserializer.</param>
    public static void UseJsonDeserializer(this IReceiveEndpointConfigurator configurator, bool isDefault = false)
    {
        var factory = new SystemTextJsonMessageSerializerFactory();

        configurator.AddDeserializer(factory, isDefault);
    }

    /// <summary>
    /// Configures the System.Text.Json payload policy for this bus. The resulting runtime options are isolated
    /// from every other bus and receive endpoint and are immutable after the serializer collection is materialized.
    /// </summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ConfigureJsonSerializerOptions(this IBusFactoryConfigurator configurator,
        Func<JsonSerializerOptions, JsonSerializerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configure != null)
            configurator.ConfigureSystemTextJsonSerializerOptions(configure);
    }

    /// <summary>Configures the System.Text.Json payload policy for this receive endpoint only.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ConfigureJsonSerializerOptions(this IReceiveEndpointConfigurator configurator,
        Func<JsonSerializerOptions, JsonSerializerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configure != null)
            configurator.ConfigureSystemTextJsonSerializerOptions(configure);
    }

    /// <summary>Specify custom <see cref="JsonSerializerOptions"/> for a message type, removing any previous configured options for the same message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void SetMessageSerializerOptions<T>(this JsonSerializerOptions options,
        Func<JsonSerializerOptions, JsonSerializerOptions>? configure = null)
        where T : class
    {
        var existingConverter = options.Converters.FirstOrDefault(x => x is CustomMessageTypeJsonConverter<T>);
        if (existingConverter != null)
            options.Converters.Remove(existingConverter);

        var messageSerializerOptions = new JsonSerializerOptions();

        // The callback may mutate the supplied instance or replace it; its return value is authoritative.
        if (configure != null)
        {
            messageSerializerOptions = configure(messageSerializerOptions)
                ?? throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Json Serializer", "unknown", "The SetMessageSerializerOptions callback returned null. It has to return the options "
                    + "to use, either the instance it was given or another one.", "Correct the named configuration before starting the host"));
        }

        options.Converters.Insert(0, new CustomMessageTypeJsonConverter<T>(messageSerializerOptions));
    }
}
