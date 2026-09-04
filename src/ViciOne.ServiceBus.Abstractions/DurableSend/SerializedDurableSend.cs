using System;
using System.ComponentModel;
using System.Linq;

namespace ViciOne.ServiceBus;

/// <summary>
/// Immutable serialized representation admitted into producer-side durable storage.
/// </summary>
/// <remarks>
/// <see cref="ContractIdentity"/> is the durable protocol identity. No assembly-qualified CLR type name is persisted.
/// Metadata is ServiceBus-owned infrastructure metadata; payload body remains opaque to the durable store.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SerializedDurableSend
{
    public const int MaximumDestinationAddressCharacters = 2048;
    public const int MaximumContentTypeCharacters = 256;
    public required DurableSendId Id { get; init; }

    public required MessageContractIdentity ContractIdentity { get; init; }

    public required Uri DestinationAddress { get; init; }

    public required string ContentType { get; init; }

    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>
    /// Opaque, bounded ServiceBus infrastructure metadata encoded with the stable infrastructure metadata codec.
    /// </summary>
    public ReadOnlyMemory<byte> Metadata { get; init; }

    public Guid? MessageId { get; init; }

    public Guid? CorrelationId { get; init; }

    /// <summary>
    /// Logical retained content bytes owned by this record: serialized payload body plus ServiceBus metadata bytes.
    /// This is the byte budget enforced by Durable Sender capacity accounting; it is deliberately not described as
    /// physical database/storage allocation because provider row, index, address and schema overhead is provider-owned.
    /// The independent retained-record limit bounds per-record overhead.
    /// </summary>
    public long StorageSize
    {
        get
        {
            checked
            {
                return (long)Body.Length + Metadata.Length;
            }
        }
    }

    /// <summary>Validates the durable infrastructure contract before it crosses a persistence boundary.</summary>
    public SerializedDurableSend Validate()
    {
        if (Id.Value == Guid.Empty)
            throw new ArgumentException("A durable send id cannot be empty.", nameof(Id));
        if (string.IsNullOrWhiteSpace(ContractIdentity.Name)
            || ContractIdentity.MajorVersion is < 1 or > ushort.MaxValue)
            throw new ArgumentException("A durable send requires a valid stable message contract identity.", nameof(ContractIdentity));
        if (DestinationAddress is null || !DestinationAddress.IsAbsoluteUri)
            throw new ArgumentException("A durable send destination must be an absolute URI.", nameof(DestinationAddress));
        if (DestinationAddress.AbsoluteUri.Length > MaximumDestinationAddressCharacters)
            throw new ArgumentOutOfRangeException(nameof(DestinationAddress), DestinationAddress.AbsoluteUri.Length,
                $"A durable send destination cannot exceed {MaximumDestinationAddressCharacters} characters.");
        if (string.IsNullOrWhiteSpace(ContentType))
            throw new ArgumentException("A durable send requires a content type.", nameof(ContentType));
        if (ContentType.Length > MaximumContentTypeCharacters)
            throw new ArgumentOutOfRangeException(nameof(ContentType), ContentType.Length,
                $"A durable send content type cannot exceed {MaximumContentTypeCharacters} characters.");
        if (ContentType.Any(char.IsControl))
            throw new ArgumentException("A durable send content type cannot contain control characters.", nameof(ContentType));

        // A zero-byte serialized body is a legitimate transport payload. Count capacity still provides a hard bound even
        // when both payload and infrastructure metadata are empty. Do not invent a non-empty-payload requirement here.
        _ = StorageSize; // also executes checked length accounting.
        return this;
    }
}
