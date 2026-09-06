using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Specifies the available transport header options values.</summary>
[Flags]
public enum TransportHeaderOptions
{
    /// <summary>Indicates include fault message.</summary>
    IncludeFaultMessage = 1,
    /// <summary>Indicates include fault detail.</summary>
    IncludeFaultDetail = 2,
    /// <summary>Indicates include host.</summary>
    IncludeHost = 4,

    /// <summary>Indicates default.</summary>
    Default = IncludeFaultMessage | IncludeFaultDetail
}
