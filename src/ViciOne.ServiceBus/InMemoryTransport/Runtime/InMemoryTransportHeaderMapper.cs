using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Copies transport-safe application headers into an in-memory transport envelope.</summary>
internal static class InMemoryTransportHeaderMapper
{
    /// <summary>Copies supported scalar values without replacing transport-owned headers.</summary>
    /// <param name="source">The application headers to copy.</param>
    /// <param name="destination">The transport envelope headers that receive supported values.</param>
    public static void Copy(Headers source, SendHeaders destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var converter = new SimpleHeaderValueConverter();

        foreach (HeaderValue header in source)
        {
            if (destination.TryGetHeader(header.Key, out _))
                continue;

            if (converter.TryConvert(header, out HeaderValue converted))
                destination.Set(converted.Key, converted.Value, overwrite: false);
        }
    }
}
