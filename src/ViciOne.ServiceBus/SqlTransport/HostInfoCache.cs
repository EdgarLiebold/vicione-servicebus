using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.SqlTransport;

public static class HostInfoCache
{
    static readonly Lazy<string> _hostInfoJson =
        new Lazy<string>(() => ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(HostMetadataCache.Host).GetString());

    public static string HostInfoJson => _hostInfoJson.Value;
}
