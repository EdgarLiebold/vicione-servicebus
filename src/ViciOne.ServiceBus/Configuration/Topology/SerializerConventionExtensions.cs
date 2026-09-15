using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures message-specific serializer selection for send topology.</summary>
public static class SerializerConventionExtensions
{
    /// <summary>Uses the serializer registered for a content type when sending one message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="contentType">The serializer content type.</param>
    public static void UseSerializer<T>(this IMessageSendTopologyConfigurator<T> configurator, ContentType contentType)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(contentType);

        configurator.AddOrUpdateConvention<ISetSerializerMessageSendTopologyConvention<T>>(
            () =>
            {
                var convention = new SetSerializerMessageSendTopologyConvention<T>();
                convention.SetSerializer(contentType);

                return convention;
            },
            update =>
            {
                update.SetSerializer(contentType);

                return update;
            });
    }

    /// <summary>Uses the serializer registered for a media-type string when sending one message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="contentType">The serializer media type.</param>
    public static void UseSerializer<T>(this IMessageSendTopologyConfigurator<T> configurator, string contentType)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var type = new ContentType(contentType);

        UseSerializer(configurator, type);
    }
}
