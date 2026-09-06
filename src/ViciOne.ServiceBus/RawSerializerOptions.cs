using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Specifies the available raw serializer options values.</summary>
[Flags]
public enum RawSerializerOptions
{
    /// <summary>Any message type is allowed, the supported message type array values are not checked.</summary>
    AnyMessageType = 1,

    /// <summary>Add the transport headers on the outbound message.</summary>
    AddTransportHeaders = 2,

    /// <summary>Copy message headers to outbound messages.</summary>
    CopyHeaders = 4,

    /// <summary>Indicates default.</summary>
    Default = CopyHeaders | AddTransportHeaders,

    /// <summary>Indicates all.</summary>
    All = AnyMessageType | AddTransportHeaders | CopyHeaders
}
