using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Performs the configured RabbitMQ virtual-host preparation during host startup.</summary>
internal sealed class RabbitMqTestHarnessHostedService :
    IHostedService
{
    readonly ILogger<RabbitMqTestHarnessHostedService> _logger;
    readonly RabbitMqSslOptions _sslOptions;
    readonly RabbitMqTestHarnessOptions _testOptions;
    readonly RabbitMqTransportOptions _transportOptions;

    /// <summary>Creates the hosted service from the effective transport and test-harness options.</summary>
    /// <param name="transportOptions">Connection and management settings for the RabbitMQ broker.</param>
    /// <param name="sslOptions">TLS settings used by the RabbitMQ transport connection.</param>
    /// <param name="testOptions">Virtual-host preparation settings.</param>
    /// <param name="logger">The logger used for preparation and cleanup diagnostics.</param>
    public RabbitMqTestHarnessHostedService(
        IOptions<RabbitMqTransportOptions> transportOptions,
        IOptions<RabbitMqSslOptions> sslOptions,
        IOptions<RabbitMqTestHarnessOptions> testOptions,
        ILogger<RabbitMqTestHarnessHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(transportOptions);
        ArgumentNullException.ThrowIfNull(sslOptions);
        ArgumentNullException.ThrowIfNull(testOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _transportOptions = transportOptions.Value;
        _sslOptions = sslOptions.Value;
        _testOptions = testOptions.Value;
    }

    /// <summary>Performs the configured virtual-host creation, cleanup, and configuration sequence.</summary>
    /// <param name="cancellationToken">Cancellation checked before broker preparation begins.</param>
    /// <returns>A task that completes when virtual-host preparation has finished.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_testOptions.CreateVirtualHostIfMissing)
            await EnsureVirtualHostExistsAsync(cancellationToken).ConfigureAwait(false);

        if (_testOptions.CleanVirtualHostOnStart)
            await CleanVirtualHostAsync(cancellationToken).ConfigureAwait(false);

        if (_testOptions.ConfigureVirtualHostAsync is { } configureVirtualHost)
            await ConfigureVirtualHostAsync(configureVirtualHost, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Completes immediately because the service owns no resources after startup.</summary>
    /// <param name="cancellationToken">The token that cancels shutdown before completion is reported.</param>
    /// <returns>A completed task, or a canceled task when shutdown has already been canceled.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    async Task EnsureVirtualHostExistsAsync(CancellationToken cancellationToken)
    {
        var name = _transportOptions.VHost;

        if (string.IsNullOrWhiteSpace(name) || name == "/")
            return;

        using HttpClient client = CreateManagementHttpClient();
        Uri requestUri = BuildManagementUri($"api/vhosts/{Uri.EscapeDataString(name.Trim('/'))}");
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using HttpResponseMessage responseMessage = await client
            .PutAsync(requestUri, content, cancellationToken)
            .ConfigureAwait(false);

        responseMessage.EnsureSuccessStatusCode();

        if (responseMessage.StatusCode == HttpStatusCode.Created)
            _logger.LogInformation("Created virtual host: {VirtualHost}", name);
    }

    async Task CleanVirtualHostAsync(CancellationToken cancellationToken)
    {
        string virtualHost = _transportOptions.VHost;

        if (string.IsNullOrWhiteSpace(virtualHost) || virtualHost == "/")
        {
            if (!_testOptions.AllowRootVirtualHostCleanup)
            {
                const string message =
                    "CleanVirtualHostOnStart requires AllowRootVirtualHostCleanup when the configured virtual host is root.";
                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }
        }

        var exchangeCount = 0;
        var queueCount = 0;
        await ExecuteWithChannelAsync(async (channel, operationCancellationToken) =>
        {
            IList<string> exchanges = await GetVirtualHostEntitiesAsync("exchanges", operationCancellationToken)
                .ConfigureAwait(false);
            foreach (string exchange in exchanges)
            {
                await channel.ExchangeDeleteAsync(exchange, cancellationToken: operationCancellationToken)
                    .ConfigureAwait(false);
                exchangeCount++;
            }

            IList<string> queues = await GetVirtualHostEntitiesAsync("queues", operationCancellationToken)
                .ConfigureAwait(false);
            foreach (string queue in queues)
            {
                await channel.QueueDeleteAsync(queue, cancellationToken: operationCancellationToken)
                    .ConfigureAwait(false);
                queueCount++;
            }
        }, cancellationToken).ConfigureAwait(false);

        if (exchangeCount > 0 || queueCount > 0)
        {
            _logger.LogInformation(
                "Removed {QueueCount} queue(s) and {ExchangeCount} exchange(s)",
                queueCount,
                exchangeCount);
        }
    }

    /// <summary>
    /// Fits a connection close reason into what AMQP can carry.
    /// <para>
    /// RabbitMQ encodes a close reason as a short string containing at most 255 bytes. The returned
    /// value always fits that protocol field so connection cleanup cannot replace the primary failure.
    /// </para>
    /// <para>
    /// Trimming preserves complete UTF-16 surrogate pairs and therefore complete UTF-8 scalar values.
    /// </para>
    /// </summary>
    /// <param name="text">The proposed AMQP close reason.</param>
    /// <returns>A UTF-8 prefix no longer than 255 bytes.</returns>
    static string CloseReason(string text)
    {
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

    Task ConfigureVirtualHostAsync(
        Func<IChannel, CancellationToken, Task> configureVirtualHost,
        CancellationToken cancellationToken)
    {
        return ExecuteWithChannelAsync(configureVirtualHost, cancellationToken);
    }

    async Task ExecuteWithChannelAsync(
        Func<IChannel, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        RabbitMqHostSettings settings = CreateHostSettings();
        ConnectionFactory factory = settings.GetConnectionFactory();
        await settings.RefreshAsync(factory, cancellationToken).ConfigureAwait(false);

        await using IConnection connection = await factory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await using IChannel channel = await connection
                .CreateChannelAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await operation(channel, cancellationToken).ConfigureAwait(false);

            await channel.CloseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            await connection.CloseAsync(200, "Completed (Ok)", cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Preparing the virtual host failed");

            if (connection.IsOpen)
            {
                try
                {
                    await connection
                        .CloseAsync(
                            500,
                            CloseReason($"Completed (not OK): {exception.Message}"),
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception closeException)
                {
                    _logger.LogDebug(closeException, "Closing the connection after failed virtual-host preparation faulted");
                }
            }

            throw;
        }
    }

    RabbitMqHostSettings CreateHostSettings()
    {
        var configurator = new RabbitMqHostConfigurator(
            _transportOptions.Host,
            _transportOptions.VHost,
            _transportOptions.Port,
            _transportOptions.ConnectionName);
        configurator.Username(_transportOptions.User);
        configurator.Password(_transportOptions.Pass);

        if (_transportOptions.UseSsl)
        {
            configurator.UseSsl(ssl =>
            {
                ssl.ServerName = string.IsNullOrWhiteSpace(_sslOptions.ServerName)
                    ? _transportOptions.Host
                    : _sslOptions.ServerName;
                ssl.CertificatePath = _sslOptions.CertPath;
                ssl.CertificatePassphrase = _sslOptions.CertPassphrase;
                ssl.UseCertificateAsAuthenticationIdentity = _sslOptions.CertIdentity;
                ssl.Protocol = _sslOptions.Protocol;

                if (_sslOptions.Trust)
                {
                    ssl.AllowPolicyErrors(
                        SslPolicyErrors.RemoteCertificateNameMismatch
                        | SslPolicyErrors.RemoteCertificateChainErrors
                        | SslPolicyErrors.RemoteCertificateNotAvailable);
                }
            });
        }

        return configurator.Settings;
    }

    async Task<IList<string>> GetVirtualHostEntitiesAsync(
        string element,
        CancellationToken cancellationToken)
    {
        using HttpClient client = CreateManagementHttpClient();

        string virtualHost = EncodeVirtualHost(_transportOptions.VHost);
        Uri requestUri = BuildManagementUri($"api/{element}/{virtualHost}");

        byte[] bytes = await client.GetByteArrayAsync(requestUri, cancellationToken).ConfigureAwait(false);

        JsonElement rootElement = JsonSerializer.Deserialize<JsonElement>(bytes, ServiceBusMetadataJson.Options);

        string?[] entities = rootElement
            .EnumerateArray()
            .Select(static item => item.GetProperty("name").GetString())
            .ToArray();

        return entities
            .OfType<string>()
            .Where(static name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith("amq.", StringComparison.Ordinal))
            .ToList();
    }

    HttpClient CreateManagementHttpClient()
    {
        var client = new HttpClient();
        byte[] credentials = Encoding.ASCII.GetBytes($"{_transportOptions.User}:{_transportOptions.Pass}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
        return client;
    }

    Uri BuildManagementUri(string path)
    {
        return new UriBuilder(
            _transportOptions.UseSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            _transportOptions.Host,
            _transportOptions.ManagementPort,
            path).Uri;
    }

    static string EncodeVirtualHost(string? virtualHost)
    {
        string name = string.IsNullOrWhiteSpace(virtualHost) || virtualHost == "/"
            ? "/"
            : virtualHost.Trim('/');
        return Uri.EscapeDataString(name);
    }
}
