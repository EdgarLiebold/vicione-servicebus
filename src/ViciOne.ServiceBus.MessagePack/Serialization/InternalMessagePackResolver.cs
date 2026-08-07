// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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

    public static MessagePackSerializerOptions Options { get; } = MessagePackSerializerOptions.Standard.WithResolver(InternalResolverInstance);
}
