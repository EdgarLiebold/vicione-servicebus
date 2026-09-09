using System;
using System.Buffers.Binary;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Computes deterministic, platform-independent MurmurHash3 x86 32-bit partition hashes.</summary>
public sealed class Murmur3PartitionHashGenerator :
    IPartitionHashGenerator
{
    const uint C1 = 0xcc9e2d51;
    const uint C2 = 0x1b873593;
    const uint Seed = 0xc58f1a7b;

    /// <summary>Computes a platform-independent hash for a binary partition key.</summary>
    /// <param name="partitionKey">The key whose partition is selected.</param>
    /// <returns>The MurmurHash3 value produced with the ViciOne partition seed.</returns>
    public uint ComputeHash(ReadOnlySpan<byte> partitionKey)
    {
        uint hash = Seed;
        int offset = 0;
        while (partitionKey.Length - offset >= sizeof(uint))
        {
            uint block = BinaryPrimitives.ReadUInt32LittleEndian(partitionKey[offset..]);
            block *= C1;
            block = RotateLeft(block, 15);
            block *= C2;

            hash ^= block;
            hash = RotateLeft(hash, 13);
            hash = hash * 5 + 0xe6546b64;
            offset += sizeof(uint);
        }

        uint tail = 0;
        ReadOnlySpan<byte> remainder = partitionKey[offset..];
        switch (remainder.Length)
        {
            case 3:
                tail ^= (uint)remainder[2] << 16;
                goto case 2;
            case 2:
                tail ^= (uint)remainder[1] << 8;
                goto case 1;
            case 1:
                tail ^= remainder[0];
                tail *= C1;
                tail = RotateLeft(tail, 15);
                tail *= C2;
                hash ^= tail;
                break;
        }

        hash ^= (uint)partitionKey.Length;
        hash ^= hash >> 16;
        hash *= 0x85ebca6b;
        hash ^= hash >> 13;
        hash *= 0xc2b2ae35;
        hash ^= hash >> 16;
        return hash;
    }

    static uint RotateLeft(uint value, int count) =>
        (value << count) | (value >> (32 - count));
}
