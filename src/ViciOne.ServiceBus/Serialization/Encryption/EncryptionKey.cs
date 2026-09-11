using System;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Represents a named AES key that can be selected from an encrypted message-data envelope.</summary>
public sealed class EncryptionKey
{
    readonly byte[] _keyMaterial;

    /// <summary>Initializes a named encryption key and takes a defensive copy of its key material.</summary>
    /// <param name="keyId">The stable identifier written to encrypted envelopes.</param>
    /// <param name="keyMaterial">A 128-, 192-, or 256-bit AES key.</param>
    /// <exception cref="ArgumentException">
    /// The key identifier is empty or the key material is not a supported AES key size.
    /// </exception>
    public EncryptionKey(string keyId, ReadOnlySpan<byte> keyMaterial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        if (Encoding.UTF8.GetByteCount(keyId) > ushort.MaxValue)
            throw new ArgumentException("The key identifier cannot exceed 65535 UTF-8 bytes.", nameof(keyId));

        if (keyMaterial.Length is not (16 or 24 or 32))
            throw new ArgumentException("AES key material must contain 16, 24, or 32 bytes.", nameof(keyMaterial));

        KeyId = keyId;
        _keyMaterial = keyMaterial.ToArray();
    }

    /// <summary>Gets the stable identifier written to encrypted envelopes.</summary>
    public string KeyId { get; }

    /// <summary>Returns a defensive copy of the AES key material for one cryptographic operation.</summary>
    /// <returns>A new byte array containing the AES key.</returns>
    internal byte[] ExportKeyMaterial()
    {
        return _keyMaterial.ToArray();
    }
}
