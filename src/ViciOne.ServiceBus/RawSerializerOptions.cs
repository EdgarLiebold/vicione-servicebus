// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Flags]
    public enum RawSerializerOptions
    {
        /// <summary>
        /// Any message type is allowed, the supported message type array values are not checked
        /// </summary>
        AnyMessageType = 1,

        /// <summary>
        /// Add the transport headers on the outbound message
        /// </summary>
        AddTransportHeaders = 2,

        /// <summary>
        /// Copy message headers to outbound messages
        /// </summary>
        CopyHeaders = 4,

        Default = CopyHeaders | AddTransportHeaders,

        All = AnyMessageType | AddTransportHeaders | CopyHeaders
    }
}
