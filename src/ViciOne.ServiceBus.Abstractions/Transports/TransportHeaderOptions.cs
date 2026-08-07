// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System;


    [Flags]
    public enum TransportHeaderOptions
    {
        IncludeFaultMessage = 1,
        IncludeFaultDetail = 2,
        IncludeHost = 4,

        Default = IncludeFaultMessage | IncludeFaultDetail
    }
}
