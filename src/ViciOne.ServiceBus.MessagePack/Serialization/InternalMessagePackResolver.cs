namespace ViciOne.ServiceBus.Serialization;

using System;
using MessagePack;
using MessagePack.Resolvers;


/// <summary>
/// The only type in this module that names <see cref="MessagePackSerializer" />. Every other call site
/// goes through the members below, so a caller cannot end up on the global default option set by
/// leaving an argument out; there is no argument to leave out. An assembly shape test holds this
/// boundary, because passing the option set by hand at eleven call sites is a convention, and a
/// convention is exactly what a later edit drops without anybody noticing.
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
    /// credential or an authorised node can be compromised. UntrustedData bounds what a hostile payload
    /// can make the deserializer allocate or construct. It sits on the one option set so that no call
    /// site can end up hardened while another is not.
    /// </summary>
    public static MessagePackSerializerOptions Options { get; } = MessagePackSerializerOptions.Standard
        .WithResolver(InternalResolverInstance)
        .WithSecurity(MessagePackSecurity.UntrustedData);

    public static byte[] Serialize<T>(T value)
    {
        return MessagePackSerializer.Serialize(value, Options);
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
