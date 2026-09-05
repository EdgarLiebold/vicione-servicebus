using System;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.Configuration;

internal static class OutboxConsumerIdentity
{
    internal static Guid Create<TConsumer, TMessage>(string busKey, Uri inputAddress)
        where TConsumer : class
        where TMessage : class
    {
        if (string.IsNullOrWhiteSpace(busKey))
            throw new ArgumentException("A bus key is required.", nameof(busKey));
        ArgumentNullException.ThrowIfNull(inputAddress);

        string identity = $"{TypeCache<TConsumer>.ShortName}:{TypeCache<TMessage>.ShortName}:{busKey}|{inputAddress.AbsoluteUri}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return new Guid(hash.AsSpan(0, 16));
    }
}
