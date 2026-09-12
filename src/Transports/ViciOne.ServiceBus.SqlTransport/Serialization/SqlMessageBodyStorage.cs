using System.Net.Mime;

namespace ViciOne.ServiceBus.SqlTransport.Serialization;

/// <summary>Chooses the lossless SQL storage representation for one serialized message body.</summary>
internal readonly record struct SqlMessageBodyStorage(string? Text, byte[]? Binary)
{
    /// <summary>Stores JSON with a proven text carrier in the queryable text column and preserves every opaque body in the binary column.</summary>
    /// <param name="body">The serialized message body.</param>
    /// <param name="contentType">The body's media content type, when available.</param>
    /// <returns>The mutually exclusive text and binary column values.</returns>
    internal static SqlMessageBodyStorage Create(MessageBody body, ContentType? contentType)
    {
        ArgumentNullException.ThrowIfNull(body);

        string? mediaType = contentType?.MediaType;
        if (string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (body.TryGetTransportText(out var text))
                return new SqlMessageBodyStorage(text, null);
        }

        return new SqlMessageBodyStorage(null, body.ToArray());
    }
}
