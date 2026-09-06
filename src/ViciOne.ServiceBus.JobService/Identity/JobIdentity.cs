using System;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Creates deterministic identifiers from stable application keys.</summary>
internal static class JobIdentity
{
    /// <summary>Creates a deterministic identifier from the first 128 bits of a SHA-256 digest.</summary>
    /// <param name="key">The stable, namespace-qualified identity key.</param>
    /// <returns>The deterministic identifier encoded from the digest.</returns>
    public static Guid CreateDeterministicId(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }
}
