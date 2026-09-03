#nullable enable

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;

/// <summary>Registration identity for an EF-backed durable sender store.</summary>
public sealed class EntityFrameworkDurableSendStoreOptions<TBus>
    where TBus : class, IBus
{
    public string? StoreKey { get; set; }

    internal string ValidateStoreKey()
    {
        var storeKey = StoreKey;
        if (string.IsNullOrWhiteSpace(storeKey))
        {
            if (typeof(TBus) == typeof(IBus))
                return "default";

            throw new ConfigurationException($"A stable durable sender {nameof(StoreKey)} is required for bus '{typeof(TBus)}'.");
        }

        if (storeKey.Length > 128)
            throw new ConfigurationException($"{nameof(StoreKey)} cannot exceed 128 characters.");

        foreach (var character in storeKey)
        {
            if (char.IsControl(character))
                throw new ConfigurationException($"{nameof(StoreKey)} cannot contain control characters.");
        }

        return storeKey;
    }
}
