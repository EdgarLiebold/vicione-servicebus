using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Provides one process-wide JSON snapshot of the current host metadata.</summary>
public static class HostInfoCache
{
    static readonly Lazy<string> _hostInfoJson =
        new(() => ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(HostMetadataCache.Host).GetRequiredTransportText());

    /// <summary>Gets the serialized host metadata snapshot.</summary>
    public static string HostInfoJson => _hostInfoJson.Value;
}
