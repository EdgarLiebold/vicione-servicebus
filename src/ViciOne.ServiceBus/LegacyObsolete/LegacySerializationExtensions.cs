// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public static class LegacySerializationExtensions
    {
        [Obsolete("Deprecated, use ClearSerialization instead")]
        public static void ClearMessageDeserializers(this IBusFactoryConfigurator configurator)
        {
            configurator.ClearSerialization();
        }

        [Obsolete("Deprecated, use ClearSerialization instead")]
        public static void ClearMessageDeserializers(this IReceiveEndpointConfigurator configurator)
        {
            configurator.ClearSerialization();
        }
    }
}
