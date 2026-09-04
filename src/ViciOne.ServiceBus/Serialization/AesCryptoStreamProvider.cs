using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides an aes crypto stream provider implementation.
/// </summary>
public class AesCryptoStreamProvider :
    ICryptoStreamProvider
{
    readonly string _defaultKeyId;
    readonly ISymmetricKeyProvider _keyProvider;
    readonly PaddingMode _paddingMode;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="keyProvider">The key provider value.</param>
    /// <param name="defaultKeyId">The default key id value.</param>
    /// <param name="paddingMode">The padding mode value.</param>
    public AesCryptoStreamProvider(ISymmetricKeyProvider keyProvider, string defaultKeyId, PaddingMode paddingMode = PaddingMode.PKCS7)
    {
        _paddingMode = paddingMode;
        _keyProvider = keyProvider;
        _defaultKeyId = defaultKeyId;
    }

    Stream ICryptoStreamProvider.GetEncryptStream(Stream stream, string? keyId, CryptoStreamMode streamMode)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        keyId ??= _defaultKeyId;

        if (!_keyProvider.TryGetKey(keyId, out var key))
            throw new SerializationException("Encryption Key not found: " + keyId);

        var encryptor = CreateEncryptor(key.Key, key.IV);

        return new DisposingCryptoStream(stream, encryptor, streamMode);
    }

    Stream ICryptoStreamProvider.GetDecryptStream(Stream stream, string? keyId, CryptoStreamMode streamMode)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        keyId ??= _defaultKeyId;

        if (!_keyProvider.TryGetKey(keyId, out var key))
            throw new SerializationException("Encryption Key not found: " + keyId);

        var encryptor = CreateDecryptor(key.Key, key.IV);

        return new DisposingCryptoStream(stream, encryptor, streamMode);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("defaultKeyId", _defaultKeyId);
        context.Add("paddingMode", _paddingMode.ToString());
    }

    ICryptoTransform CreateDecryptor(byte[] key, byte[] iv)
    {
        using var provider = CreateAes();

        return provider.CreateDecryptor(key, iv);
    }

    /// <summary>
    /// Creates encryptor.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="iv">The iv value.</param>
    /// <returns>The result of the operation.</returns>
    public ICryptoTransform CreateEncryptor(byte[] key, byte[] iv)
    {
        using (var provider = CreateAes())
        {
            return provider.CreateEncryptor(key, iv);
        }
    }

    Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Padding = _paddingMode;
        return aes;
    }
}
