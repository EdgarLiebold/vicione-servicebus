using System;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Computes stable partition hashes. Implementations must be safe for concurrent calls.</summary>
public interface IPartitionHashGenerator
{
    /// <summary>Computes a deterministic hash for a binary partition key.</summary>
    /// <param name="partitionKey">The key whose partition is selected.</param>
    /// <returns>The same unsigned value for the same key across processes and supported platforms.</returns>
    uint ComputeHash(ReadOnlySpan<byte> partitionKey);
}
