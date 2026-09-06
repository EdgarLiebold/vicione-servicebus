using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Caches host info data.</summary>
public static class HostInfoCache
{
    static readonly Lazy<string> _hostInfoJson =
        new Lazy<string>(() => ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(HostMetadataCache.Host).GetString());

    /// <summary>Gets the host info json.</summary>
    public static string HostInfoJson => _hostInfoJson.Value;
}
