using System;
using System.Buffers;
using MessagePack;
using MessagePack.Resolvers;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>
/// Centralizes MessagePack serialization through the module's resolver chain and security options.
/// </summary>
static class InternalMessagePackResolver
{
    static IFormatterResolver InternalResolverInstance { get; } =
        CompositeResolver.Create(NativeDateTimeResolver.Instance,
            ContractlessStandardResolverAllowPrivate.Instance,
            ServiceBusMessagePackFormatterResolver.Instance,
            DynamicGenericResolver.Instance);

    /// <summary>
    /// Gets the shared serializer options used for every MessagePack payload in this module.
    /// <para>
    /// The resolver supports native date/time values, private contractless members, ViciOne contract
    /// mappings, and generic collection shapes. <see cref="MessagePackSecurity.UntrustedData" /> applies
    /// defensive collection handling to payloads received across the broker trust boundary.
    /// </para>
    /// </summary>
    public static MessagePackSerializerOptions Options { get; } = MessagePackSerializerOptions.Standard
        .WithResolver(InternalResolverInstance)
        .WithSecurity(MessagePackSecurity.UntrustedData);

    public static byte[] Serialize<T>(T value)
    {
        return MessagePackSerializer.Serialize(value, Options);
    }

    public static void Serialize(Type type, IBufferWriter<byte> writer, object? value)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(writer);
        MessagePackSerializer.Serialize(type, writer, value, Options);
    }

    public static void Serialize<T>(IBufferWriter<byte> writer, T value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        MessagePackSerializer.Serialize(writer, value, Options);
    }

    public static T Deserialize<T>(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return MessagePackSerializer.Deserialize<T>(buffer, Options);
    }

    /// <summary>
    /// Deserializes a payload using a message type selected at runtime.
    /// </summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="buffer">The MessagePack payload bytes.</param>
    /// <returns>The deserialized message, or <see langword="null" /> when the wire payload is nil.</returns>
    public static object? Deserialize(Type messageType, byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(buffer);
        return MessagePackSerializer.Deserialize(messageType, buffer, Options);
    }
}
