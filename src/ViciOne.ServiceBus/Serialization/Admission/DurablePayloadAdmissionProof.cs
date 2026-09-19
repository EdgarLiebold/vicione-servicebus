using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Binds a previously admitted application-body length and offload decision to one wire envelope.</summary>
internal readonly record struct DurablePayloadAdmissionProof(
    int SerializedBodyBytes,
    bool MessageDataOffloadObserved,
    byte[] AdmissionSha256)
{
    internal static DurablePayloadAdmissionProof Create(
        int serializedBodyBytes,
        bool messageDataOffloadObserved,
        ReadOnlySpan<byte> envelopeSha256,
        string contentType)
        => new(serializedBodyBytes, messageDataOffloadObserved,
            ComputeDigest(envelopeSha256, serializedBodyBytes, messageDataOffloadObserved, contentType));

    internal bool MatchesEnvelope(ReadOnlySpan<byte> envelope, string contentType)
        => AdmissionSha256 is { Length: 32 }
            && CryptographicOperations.FixedTimeEquals(
                ComputeDigest(SHA256.HashData(envelope), SerializedBodyBytes, MessageDataOffloadObserved, contentType),
                AdmissionSha256);

    private static byte[] ComputeDigest(
        ReadOnlySpan<byte> envelopeSha256,
        int serializedBodyBytes,
        bool messageDataOffloadObserved,
        string contentType)
    {
        if (envelopeSha256.Length != 32)
            throw new ArgumentException("The envelope hash must contain 32 bytes.", nameof(envelopeSha256));
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        Span<byte> proofInput = stackalloc byte[70];
        envelopeSha256.CopyTo(proofInput);
        BinaryPrimitives.WriteInt32LittleEndian(proofInput[32..], serializedBodyBytes);
        proofInput[36] = messageDataOffloadObserved ? (byte)1 : (byte)0;
        proofInput[37] = 1; // Proof format version.
        SHA256.HashData(Encoding.UTF8.GetBytes(contentType), proofInput[38..]);
        return SHA256.HashData(proofInput);
    }
}
