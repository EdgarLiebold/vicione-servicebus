using System;

namespace ViciOne.ServiceBus.Transports;

[Flags]
public enum TransportHeaderOptions
{
    IncludeFaultMessage = 1,
    IncludeFaultDetail = 2,
    IncludeHost = 4,

    Default = IncludeFaultMessage | IncludeFaultDetail
}
