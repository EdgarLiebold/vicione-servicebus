using System;
using System.Linq;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Immutable serialized representation admitted into producer-side durable storage.</summary>
/// <remarks>
/// <see cref="ContractIdentity"/> is the durable protocol identity. No assembly-qualified CLR type name is persisted.
/// Metadata is ServiceBus-owned infrastructure metadata; payload body remains opaque to the durable store.
/// </remarks>
public sealed record SerializedDurableSend
{
    /// <summary>Exposes the maximum destination address characters used by the containing type.</summary>
    public const int MaximumDestinationAddressCharacters = 2048;
    /// <summary>Exposes the maximum content type characters used by the containing type.</summary>
    public const int MaximumContentTypeCharacters = 256;
    /// <summary>Gets or sets the id.</summary>
    public required DurableSendId Id { get; init; }

    /// <summary>Gets or sets the contract identity.</summary>
    public required MessageContractIdentity ContractIdentity { get; init; }

    /// <summary>Gets or sets the destination address.</summary>
    public required Uri DestinationAddress { get; init; }

    /// <summary>Gets or sets the content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets or sets the body.</summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>Opaque, bounded ServiceBus infrastructure metadata encoded with the stable infrastructure metadata codec.</summary>
    public ReadOnlyMemory<byte> Metadata { get; init; }

    /// <summary>Gets or sets the message id.</summary>
    public Guid? MessageId { get; init; }

    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>
    /// Optional first-delivery time. A missing value means immediately due; a future value is persisted as outbox state
    /// and is never owned by an in-process timer.
    /// </summary>
    public DateTimeOffset? DueAt { get; init; }

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
    /// <returns>The validation failures.</returns>
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
