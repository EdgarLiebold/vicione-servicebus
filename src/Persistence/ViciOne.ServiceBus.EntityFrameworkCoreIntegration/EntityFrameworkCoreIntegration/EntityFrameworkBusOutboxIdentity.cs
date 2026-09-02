namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;
using System.Security.Cryptography;
using System.Text;


internal static class EntityFrameworkBusOutboxIdentity<TBus>
    where TBus : class, IBus
{
    internal const int MaximumBusKeyLength = 256;

    public static string BusKey { get; } = CreateBusKey();

    static string CreateBusKey()
    {
        if (typeof(TBus) == typeof(IBus))
            return "default";

        string typeName = typeof(TBus).FullName ?? typeof(TBus).Name;
        string assemblyName = typeof(TBus).Assembly.GetName().Name ?? "unknown";
        string identity = $"{assemblyName}:{typeName}";
        if (identity.Length <= MaximumBusKeyLength)
            return identity;

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        int prefixLength = MaximumBusKeyLength - hash.Length - 1;
        return $"{identity[..prefixLength]}#{hash}";
    }
}
