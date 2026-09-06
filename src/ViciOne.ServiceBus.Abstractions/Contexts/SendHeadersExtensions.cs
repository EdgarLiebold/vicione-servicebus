using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for send headers.</summary>
public static class SendHeadersExtensions
{
    /// <summary>Copy the headers from an existing header collection into the <paramref name="sendHeaders" /> header collection.</summary>
    /// <param name="sendHeaders">The send headers.</param>
    /// <param name="headers">The source header collection.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static void CopyFrom(this SendHeaders sendHeaders, Headers headers)
    {
        if (sendHeaders == null)
            throw new ArgumentNullException(nameof(sendHeaders));
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        foreach (var header in headers)
            sendHeaders.Set(header.Key, header.Value);
    }

    /// <summary>Copy the headers from an existing header collection into the <paramref name="sendHeaders" /> header collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="adapter">The dictionary adapter for setting headers.</param>
    /// <param name="sendHeaders">The send header collection.</param>
    /// <param name="headers">The source header collection.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static void CopyFrom<T>(this ITransportSetHeaderAdapter<T> adapter, IDictionary<string, T> sendHeaders, Headers headers)
    {
        if (adapter == null)
            throw new ArgumentNullException(nameof(adapter));
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        foreach (var header in headers)
            adapter.Set(sendHeaders, header);
    }
}
