namespace ViciOne.ServiceBus.SqlTransport
{
    using System;
    using Metadata;
    using Serialization;


    public static class HostInfoCache
    {
        static readonly Lazy<string> _hostInfoJson =
            new Lazy<string>(() => ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(HostMetadataCache.Host).GetString());

        public static string HostInfoJson => _hostInfoJson.Value;
    }
}
