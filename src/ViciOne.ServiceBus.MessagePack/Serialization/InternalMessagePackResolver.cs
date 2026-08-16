namespace ViciOne.ServiceBus.Serialization;

using MessagePack;
using MessagePack.Resolvers;


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
}
