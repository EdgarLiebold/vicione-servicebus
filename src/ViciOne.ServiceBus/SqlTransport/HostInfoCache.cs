using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a host info cache implementation.
/// </summary>
public static class HostInfoCache
{
    static readonly Lazy<string> _hostInfoJson =
        new Lazy<string>(() => ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(HostMetadataCache.Host).GetString());

    /// <summary>
    /// Gets the host info json value.
    /// </summary>
    public static string HostInfoJson => _hostInfoJson.Value;
}
