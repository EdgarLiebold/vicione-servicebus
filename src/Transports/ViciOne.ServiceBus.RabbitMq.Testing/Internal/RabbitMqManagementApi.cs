using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Creates RabbitMQ management API requests from transport settings.</summary>
internal static class RabbitMqManagementApi
{
    /// <summary>Creates an HTTP client with UTF-8 Basic authentication credentials.</summary>
    /// <param name="username">The RabbitMQ management user name.</param>
    /// <param name="password">The RabbitMQ management password.</param>
    /// <returns>An authenticated HTTP client.</returns>
    internal static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient();
        byte[] credentials = Encoding.UTF8.GetBytes($"{username}:{password}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
        return client;
    }

    /// <summary>Builds an absolute RabbitMQ management API URI.</summary>
    /// <param name="host">The management API host.</param>
    /// <param name="port">The management API TCP port.</param>
    /// <param name="useTls">Whether to use HTTPS.</param>
    /// <param name="path">The management API path.</param>
    /// <returns>The normalized management API URI.</returns>
    internal static Uri BuildUri(string host, int port, bool useTls, string path)
    {
        string scheme = useTls ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        return new UriBuilder(scheme, host, port, path).Uri;
    }

    /// <summary>Encodes a RabbitMQ virtual-host name as one management API path segment.</summary>
    /// <param name="virtualHost">The configured virtual-host name.</param>
    /// <returns>The escaped virtual-host path segment.</returns>
    internal static string EncodeVirtualHost(string? virtualHost)
    {
        string name = IsRootVirtualHost(virtualHost)
            ? "/"
            : virtualHost!.Trim('/');
        return Uri.EscapeDataString(name);
    }

    /// <summary>Determines whether a configured virtual-host name denotes the broker root.</summary>
    /// <param name="virtualHost">The configured virtual-host name.</param>
    /// <returns><see langword="true"/> when the value denotes the root virtual host.</returns>
    internal static bool IsRootVirtualHost(string? virtualHost) =>
        string.IsNullOrWhiteSpace(virtualHost) || string.IsNullOrWhiteSpace(virtualHost.Trim('/'));

    /// <summary>Reads user-created entity names from a RabbitMQ management API collection.</summary>
    /// <param name="client">The management API client.</param>
    /// <param name="requestUri">The collection URI.</param>
    /// <param name="cancellationToken">The token that cancels the HTTP request.</param>
    /// <returns>The non-system, nonblank entity names returned by the broker.</returns>
    internal static async Task<IList<string>> GetEntityNamesAsync(
        HttpClient client,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestUri);

        byte[] bytes = await client.GetByteArrayAsync(requestUri, cancellationToken).ConfigureAwait(false);
        JsonElement rootElement = JsonSerializer.Deserialize<JsonElement>(bytes, ServiceBusMetadataJson.Options);

        return rootElement
            .EnumerateArray()
            .Select(static item => item.GetProperty("name").GetString())
            .OfType<string>()
            .Where(static name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith("amq.", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Fits an AMQP connection close reason into its 255-byte protocol field.</summary>
    /// <param name="text">The proposed close reason.</param>
    /// <returns>A complete UTF-8 scalar prefix no longer than 255 bytes.</returns>
    internal static string CreateConnectionCloseReason(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        const int maximumBytes = 255;
        if (Encoding.UTF8.GetByteCount(text) <= maximumBytes)
            return text;

        var length = Math.Min(text.Length, maximumBytes);
        while (length > 0 && Encoding.UTF8.GetByteCount(text.AsSpan(0, length)) > maximumBytes)
            length--;

        if (length > 0 && char.IsHighSurrogate(text[length - 1]))
            length--;

        return text[..length];
    }
}
