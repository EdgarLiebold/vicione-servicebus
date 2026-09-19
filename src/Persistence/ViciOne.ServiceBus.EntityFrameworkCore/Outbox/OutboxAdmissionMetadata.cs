using System;
using System.Globalization;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Frames an EF-owned admission proof outside the application header dictionary.</summary>
internal static class OutboxAdmissionMetadata
{
    private const string Prefix = "VOSB-EF-OUTBOX-ADMISSION/1:";

    internal static string Encode(string? headers, DurablePayloadAdmissionProof proof)
    {
        if (proof.SerializedBodyBytes < 0 || proof.AdmissionSha256 is not { Length: 32 })
            throw new InvalidOperationException("The outbox payload-admission proof is incomplete.");

        return string.Concat(
            Prefix,
            proof.SerializedBodyBytes.ToString(CultureInfo.InvariantCulture),
            ":",
            proof.MessageDataOffloadObserved ? "1" : "0",
            ":",
            Convert.ToHexString(proof.AdmissionSha256),
            "\n",
            headers);
    }

    internal static (string? Headers, DurablePayloadAdmissionProof? Proof) Decode(string? storedHeaders)
    {
        if (storedHeaders is null || !storedHeaders.StartsWith(Prefix, StringComparison.Ordinal))
            return (storedHeaders, null);

        ReadOnlySpan<char> record = storedHeaders.AsSpan(Prefix.Length);
        int lengthEnd = record.IndexOf(':');
        if (lengthEnd < 1
            || !int.TryParse(record[..lengthEnd], NumberStyles.None, CultureInfo.InvariantCulture, out int bodyBytes))
            throw InvalidProof();

        record = record[(lengthEnd + 1)..];
        if (record.Length < 67 || record[1] != ':' || record[66] != '\n'
            || record[0] is not ('0' or '1'))
            throw InvalidProof();

        byte[] digest;
        try
        {
            digest = Convert.FromHexString(record.Slice(2, 64));
        }
        catch (FormatException)
        {
            throw InvalidProof();
        }

        string? headers = record.Length == 67 ? null : record[67..].ToString();
        return (headers, new DurablePayloadAdmissionProof(bodyBytes, record[0] == '1', digest));
    }

    private static SerializationException InvalidProof() =>
        new("The persisted outbox payload-admission proof is malformed.");
}
