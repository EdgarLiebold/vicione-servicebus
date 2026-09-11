using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Controls message-type validation and header propagation for raw serializers.</summary>
[Flags]
public enum RawSerializerOptions
{
    /// <summary>Uses declared message types and emits no transport or application headers.</summary>
    None = 0,

    /// <summary>Accepts messages without requiring a declared supported message type.</summary>
    AnyMessageType = 1,

    /// <summary>Adds transport metadata headers to outbound messages.</summary>
    AddTransportHeaders = 2,

    /// <summary>Copies application headers and diagnostic propagation metadata to outbound messages.</summary>
    CopyHeaders = 4,

    /// <summary>Emits transport, application, and diagnostic propagation headers while enforcing declared message types.</summary>
    Default = CopyHeaders | AddTransportHeaders,

    /// <summary>Enables every raw-serializer option.</summary>
    All = AnyMessageType | AddTransportHeaders | CopyHeaders
}
