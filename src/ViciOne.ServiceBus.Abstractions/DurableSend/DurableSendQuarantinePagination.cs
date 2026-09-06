using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;


namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Canonical seek-token codec shared by durable persistence providers.</summary>
public static class DurableSendQuarantinePagination
{
    private const byte Version = 1;
    private const int TokenBytes = 25;

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the operation.</returns>
    public static DurableSendQuarantineSeek Validate(DurableSendQuarantineQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        DurableSendOperationLimits.ValidateQuarantinePageSize(query.PageSize, nameof(query.PageSize));
        return query.ContinuationToken is null
            ? default
            : Decode(query.ContinuationToken);
    }

    /// <summary>
    /// Creates page.
    /// </summary>
    /// <param name="fetchedEntries">The fetched entries value.</param>
    /// <param name="pageSize">The page size value.</param>
    /// <returns>The result of the operation.</returns>
    public static DurableSendQuarantinePage CreatePage(
        IEnumerable<DurableSendQuarantineEntry> fetchedEntries,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(fetchedEntries);
        DurableSendOperationLimits.ValidateQuarantinePageSize(pageSize, nameof(pageSize));
        DurableSendQuarantineEntry[] fetched = fetchedEntries.Take(checked(pageSize + 1)).ToArray();
        bool hasMore = fetched.Length > pageSize;
        DurableSendQuarantineEntry[] entries = hasMore ? fetched[..pageSize] : fetched;
        string? continuation = hasMore && entries.Length > 0
            ? Encode(new DurableSendQuarantineSeek(entries[^1].QuarantinedAt, entries[^1].Id))
            : null;
        return new DurableSendQuarantinePage(entries, continuation);
    }

    /// <summary>
    /// Performs the encode operation.
    /// </summary>
    /// <param name="seek">The seek value.</param>
    /// <returns>The result of the operation.</returns>
    public static string Encode(DurableSendQuarantineSeek seek)
    {
        if (!seek.HasValue)
            throw new ArgumentException("A quarantine continuation seek must have a value.", nameof(seek));

        Span<byte> bytes = stackalloc byte[TokenBytes];
        bytes[0] = Version;
        BinaryPrimitives.WriteInt64BigEndian(bytes[1..9], seek.QuarantinedAt.UtcTicks);
        seek.Id.Value.TryWriteBytes(bytes[9..], bigEndian: true, out int written);
        if (written != 16)
            throw new InvalidOperationException("The durable-send id could not be encoded into a continuation token.");

        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static DurableSendQuarantineSeek Decode(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64)
            throw InvalidToken();

        try
        {
            string padded = token.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            byte[] bytes = Convert.FromBase64String(padded);
            if (bytes.Length != TokenBytes || bytes[0] != Version)
                throw InvalidToken();

            long utcTicks = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(1, 8));
            var timestamp = new DateTimeOffset(utcTicks, TimeSpan.Zero);
            var id = new DurableSendId(new Guid(bytes.AsSpan(9, 16), bigEndian: true));
            return new DurableSendQuarantineSeek(timestamp, id);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            throw InvalidToken(exception);
        }
    }

    private static ArgumentException InvalidToken(Exception? inner = null)
        => new("The durable-send quarantine continuation token is invalid or unsupported.", "ContinuationToken", inner);
}

/// <summary>Decoded provider seek. Ordering is QuarantinedAt descending, then DurableSendId ascending.</summary>
public readonly record struct DurableSendQuarantineSeek(DateTimeOffset QuarantinedAt, DurableSendId Id)
{
    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue => Id.Value != Guid.Empty;
}
