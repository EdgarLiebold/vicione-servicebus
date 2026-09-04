using System;
using System.IO;
using System.Security.Cryptography;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides an aes crypto stream provider v2 implementation.
/// </summary>
public class AesCryptoStreamProviderV2 :
    ICryptoStreamProviderV2
{
    readonly PaddingMode _paddingMode;
    readonly ISecureKeyProvider _secureKeyProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="secureKeyProvider">The secure key provider value.</param>
    /// <param name="paddingMode">The padding mode value.</param>
    public AesCryptoStreamProviderV2(ISecureKeyProvider secureKeyProvider, PaddingMode paddingMode = PaddingMode.PKCS7)
    {
        _secureKeyProvider = secureKeyProvider;
        _paddingMode = paddingMode;
    }

    /// <summary>
    /// Gets decrypt stream.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="headers">The headers value.</param>
    /// <returns>The result of the operation.</returns>
    public Stream GetDecryptStream(Stream stream, Headers headers)
    {
        var key = _secureKeyProvider.GetKey(headers);

        var iv = new byte[16];
        var read = stream.Read(iv, 0, iv.Length);
        if (read != iv.Length)
            throw new InvalidOperationException("The stream does not contain enough bytes to read the IV.");

        var encryptor = CreateDecryptor(key, iv);

        return new DisposingCryptoStream(stream, encryptor, CryptoStreamMode.Read);
    }

    /// <summary>
    /// Gets encrypt stream.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="headers">The headers value.</param>
    /// <returns>The result of the operation.</returns>
    public Stream GetEncryptStream(Stream stream, Headers headers)
    {
        var key = _secureKeyProvider.GetKey(headers);

        var iv = GenerateIv();

        stream.Write(iv, 0, iv.Length);
        var encryptor = CreateEncryptor(key, iv);

        return new DisposingCryptoStream(stream, encryptor, CryptoStreamMode.Write);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("aes");

        scope.Add("paddingMode", _paddingMode.ToString());

        _secureKeyProvider.Probe(scope);
    }

    ICryptoTransform CreateDecryptor(byte[] key, byte[] iv)
    {
        using var provider = CreateAes();

        return provider.CreateDecryptor(key, iv);
    }

    byte[] GenerateIv()
    {
        using var aes = CreateAes();

        aes.GenerateIV();

        return aes.IV;
    }

    ICryptoTransform CreateEncryptor(byte[] key, byte[] iv)
    {
        using var provider = CreateAes();

        return provider.CreateEncryptor(key, iv);
    }

    Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Padding = _paddingMode;
        return aes;
    }
}
