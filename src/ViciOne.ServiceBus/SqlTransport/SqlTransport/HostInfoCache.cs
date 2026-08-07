// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    using System;
    using Metadata;
    using Serialization;


    public static class HostInfoCache
    {
        static readonly Lazy<string> _hostInfoJson =
            new Lazy<string>(() => SystemTextJsonMessageSerializer.Instance.SerializeObject(HostMetadataCache.Host).GetString());

        public static string HostInfoJson => _hostInfoJson.Value;
    }
}
