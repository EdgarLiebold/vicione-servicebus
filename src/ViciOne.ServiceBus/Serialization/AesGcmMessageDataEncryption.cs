using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Encrypts and authenticates complete message-data objects in a versioned AES-GCM envelope.</summary>
internal static class AesGcmMessageDataEncryption
{
    internal const int NonceSize = 12;
    internal const int TagSize = 16;
    internal const byte Version = 1;

    const int KeyIdLengthSize = sizeof(ushort);
    static readonly byte[] Magic = "VOSB"u8.ToArray();

    public static async Task<Stream> DecryptAsync(
        Stream source,
        IEncryptionKeyProvider keyProvider,
        int maximumPlaintextBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keyProvider);

        int maximumEnvelopeBytes = checked(maximumPlaintextBytes + Magic.Length + sizeof(byte) + KeyIdLengthSize + ushort.MaxValue + NonceSize + TagSize);
        byte[] envelope = await ReadAllBytesAsync(source, maximumEnvelopeBytes, cancellationToken).ConfigureAwait(false);
        int minimumLength = Magic.Length + sizeof(byte) + KeyIdLengthSize + 1 + NonceSize + TagSize;
        if (envelope.Length < minimumLength)
            throw InvalidEnvelope("The envelope is truncated.");

        ReadOnlySpan<byte> envelopeSpan = envelope;
        if (!envelopeSpan[..Magic.Length].SequenceEqual(Magic))
            throw InvalidEnvelope("The magic value is invalid.");

        int offset = Magic.Length;
        byte version = envelopeSpan[offset++];
        if (version != Version)
            throw InvalidEnvelope($"Envelope version {version} is not supported.");

        int keyIdLength = BinaryPrimitives.ReadUInt16BigEndian(envelopeSpan.Slice(offset, KeyIdLengthSize));
        offset += KeyIdLengthSize;
        if (keyIdLength == 0 || envelope.Length - offset < keyIdLength + NonceSize + TagSize)
            throw InvalidEnvelope("The key identifier length is invalid.");

        ReadOnlySpan<byte> keyIdBytes = envelopeSpan.Slice(offset, keyIdLength);
        string keyId;
        try
        {
            keyId = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(keyIdBytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw InvalidEnvelope("The key identifier is not valid UTF-8.", exception);
        }

        offset += keyIdLength;
        ReadOnlySpan<byte> nonce = envelopeSpan.Slice(offset, NonceSize);
        offset += NonceSize;
        int ciphertextLength = envelope.Length - offset - TagSize;
        if (ciphertextLength > maximumPlaintextBytes)
        {
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.MessageData,
                ciphertextLength,
                maximumPlaintextBytes,
                $"Encrypted message data contains {ciphertextLength} plaintext bytes and exceeds the configured object limit of {maximumPlaintextBytes} bytes.");
        }

        ReadOnlySpan<byte> ciphertext = envelopeSpan.Slice(offset, ciphertextLength);
        ReadOnlySpan<byte> tag = envelopeSpan[^TagSize..];

        if (!keyProvider.TryGetKey(keyId, out EncryptionKey? key) || key is null)
            throw new SerializationException($"Encryption key '{keyId}' was not found.");

        byte[] keyMaterial = key.ExportKeyMaterial();
        byte[] plaintext = new byte[ciphertextLength];
        byte[] associatedData = CreateAssociatedData(keyIdBytes, version);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var aes = new AesGcm(keyMaterial, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            cancellationToken.ThrowIfCancellationRequested();
            return new MemoryStream(plaintext, writable: false);
        }
        catch (AuthenticationTagMismatchException exception)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw new SerializationException("Encrypted message data authentication failed.", exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyMaterial);
            CryptographicOperations.ZeroMemory(associatedData);
        }
    }

    public static async Task<Stream> EncryptAsync(
        Stream source,
        IEncryptionKeyProvider keyProvider,
        int maximumPlaintextBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keyProvider);

        EncryptionKey key = keyProvider.GetCurrentKey()
            ?? throw new SerializationException("The encryption key provider returned no current key.");
        byte[] keyId = Encoding.UTF8.GetBytes(key.KeyId);
        if (keyId.Length is 0 or > ushort.MaxValue)
            throw new SerializationException("The current encryption key identifier must encode to between 1 and 65535 UTF-8 bytes.");

        byte[] plaintext = await ReadAllBytesAsync(source, maximumPlaintextBytes, cancellationToken).ConfigureAwait(false);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];
        byte[] associatedData = CreateAssociatedData(keyId, Version);
        byte[] keyMaterial = key.ExportKeyMaterial();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var aes = new AesGcm(keyMaterial, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            cancellationToken.ThrowIfCancellationRequested();

            int envelopeLength = checked(Magic.Length + sizeof(byte) + KeyIdLengthSize + keyId.Length + NonceSize + ciphertext.Length + TagSize);
            byte[] envelope = new byte[envelopeLength];
            int offset = 0;
            Magic.CopyTo(envelope, offset);
            offset += Magic.Length;
            envelope[offset++] = Version;
            BinaryPrimitives.WriteUInt16BigEndian(envelope.AsSpan(offset, KeyIdLengthSize), checked((ushort)keyId.Length));
            offset += KeyIdLengthSize;
            keyId.CopyTo(envelope, offset);
            offset += keyId.Length;
            nonce.CopyTo(envelope, offset);
            offset += NonceSize;
            ciphertext.CopyTo(envelope, offset);
            tag.CopyTo(envelope, offset + ciphertext.Length);
            return new MemoryStream(envelope, writable: false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyMaterial);
            CryptographicOperations.ZeroMemory(associatedData);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    static byte[] CreateAssociatedData(ReadOnlySpan<byte> keyId, byte version)
    {
        byte[] associatedData = new byte[keyId.Length + sizeof(byte)];
        keyId.CopyTo(associatedData);
        associatedData[^1] = version;
        return associatedData;
    }

    static SerializationException InvalidEnvelope(string problem, Exception? innerException = null)
    {
        return new SerializationException($"Encrypted message data envelope is invalid. {problem}", innerException);
    }

    static async Task<byte[]> ReadAllBytesAsync(Stream source, int maximumBytes, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 1);

        using var buffer = new MemoryStream();
        byte[] copyBuffer = ArrayPool<byte>.Shared.Rent(Math.Min(81920, checked(maximumBytes + 1)));
        try
        {
            while (true)
            {
                int remainingWithOverflowSentinel = checked(maximumBytes + 1 - (int)buffer.Length);
                int read = await source
                    .ReadAsync(copyBuffer.AsMemory(0, Math.Min(copyBuffer.Length, remainingWithOverflowSentinel)), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                    return buffer.ToArray();

                if (buffer.Length + read > maximumBytes)
                {
                    throw new PayloadAdmissionException(
                        PayloadAdmissionStage.MessageData,
                        checked(maximumBytes + 1L),
                        maximumBytes,
                        $"Message data exceeds the configured encrypted-object limit of {maximumBytes} bytes.");
                }

                buffer.Write(copyBuffer, 0, read);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copyBuffer);
            ArrayPool<byte>.Shared.Return(copyBuffer);
        }
    }
}
