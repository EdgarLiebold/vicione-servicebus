using System;
using System.Buffers;
using MessagePack;
using MessagePack.Resolvers;

namespace ViciOne.ServiceBus.Serialization;
/// <summary>
/// The only type in this module that names <see cref="MessagePackSerializer" />. Every other call site
/// goes through the members below, so a caller cannot end up on the global default option set by
/// leaving an argument out; there is no argument to leave out. Centralizing the boundary prevents
/// call sites from accidentally omitting the required option set.
/// </summary>
static class InternalMessagePackResolver
{
    static IFormatterResolver InternalResolverInstance { get; } =
        CompositeResolver.Create(NativeDateTimeResolver.Instance,
            ContractlessStandardResolverAllowPrivate.Instance,
            ViciOneServiceBusMessagePackFormatterResolver.Instance,
            DynamicGenericResolver.Instance);

    /// <summary>
    /// Everything arriving from a broker crosses a trust boundary, even over authenticated TLS: a
    /// credential or an authorised node can be compromised.
    /// <para>
    /// UntrustedData is defense in depth against selected attacks, chiefly forced hash collisions in
    /// keyed collections. It is not authentication, and it is not a general bound on what a hostile
    /// payload can make the deserializer allocate or construct; it and the default both stop at the
    /// same object graph depth. It sits on the one option set so that no call site can end up hardened
    /// while another is not.
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
        MessagePackSerializer.Serialize(type, writer, value, Options);
    }

    public static void Serialize<T>(IBufferWriter<byte> writer, T value)
    {
        MessagePackSerializer.Serialize(writer, value, Options);
    }

    public static T Deserialize<T>(byte[] buffer)
    {
        return MessagePackSerializer.Deserialize<T>(buffer, Options);
    }

    /// <summary>
    /// Nullable, because a payload is allowed to be nil on the wire and the caller decides what that
    /// means. Declaring it as never null here would only have moved the question out of sight.
    /// </summary>
    public static object? Deserialize(Type messageType, byte[] buffer)
    {
        return MessagePackSerializer.Deserialize(messageType, buffer, Options);
    }
}
