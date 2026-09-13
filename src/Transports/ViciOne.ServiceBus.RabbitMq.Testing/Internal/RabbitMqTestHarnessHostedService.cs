using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Performs the configured RabbitMQ virtual-host preparation during host startup.</summary>
internal sealed class RabbitMqTestHarnessHostedService :
    IHostedService
{
    readonly Func<CancellationToken, Task<IConnection>> _createConnection;
    readonly Func<HttpClient> _createManagementHttpClient;
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
        _createConnection = CreateConnectionAsync;
        _createManagementHttpClient = CreateDefaultManagementHttpClient;
    }

    /// <summary>Creates the hosted service with explicit broker-resource factories.</summary>
    /// <param name="transportOptions">The effective RabbitMQ transport options.</param>
    /// <param name="sslOptions">The effective RabbitMQ TLS options.</param>
    /// <param name="testOptions">The effective test-harness options.</param>
    /// <param name="logger">The logger used for preparation and cleanup diagnostics.</param>
    /// <param name="createManagementHttpClient">Creates a management API client for each request group.</param>
    /// <param name="createConnection">Creates the AMQP connection used for channel operations.</param>
    internal RabbitMqTestHarnessHostedService(
        RabbitMqTransportOptions transportOptions,
        RabbitMqSslOptions sslOptions,
        RabbitMqTestHarnessOptions testOptions,
        ILogger<RabbitMqTestHarnessHostedService> logger,
        Func<HttpClient> createManagementHttpClient,
        Func<CancellationToken, Task<IConnection>> createConnection)
    {
        ArgumentNullException.ThrowIfNull(transportOptions);
        ArgumentNullException.ThrowIfNull(sslOptions);
        ArgumentNullException.ThrowIfNull(testOptions);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(createManagementHttpClient);
        ArgumentNullException.ThrowIfNull(createConnection);

        _transportOptions = transportOptions;
        _sslOptions = sslOptions;
        _testOptions = testOptions;
        _logger = logger;
        _createManagementHttpClient = createManagementHttpClient;
        _createConnection = createConnection;
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

        if (RabbitMqManagementApi.IsRootVirtualHost(name))
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

        if (RabbitMqManagementApi.IsRootVirtualHost(virtualHost)
            && !_testOptions.AllowRootVirtualHostCleanup)
        {
            const string message =
                "CleanVirtualHostOnStart requires AllowRootVirtualHostCleanup when the configured virtual host is root.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
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
        await using IConnection connection = await _createConnection(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The RabbitMQ connection factory returned null.");
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
                            RabbitMqManagementApi.CreateConnectionCloseReason(
                                $"Completed (not OK): {exception.Message}"),
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

    async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        RabbitMqHostSettings settings = CreateHostSettings();
        ConnectionFactory factory = settings.GetConnectionFactory();
        await settings.RefreshAsync(factory, cancellationToken).ConfigureAwait(false);
        return await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds the RabbitMQ connection settings used for startup preparation.</summary>
    /// <returns>The effective connection settings.</returns>
    internal RabbitMqHostSettings CreateHostSettings()
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

        return await RabbitMqManagementApi
            .GetEntityNamesAsync(client, requestUri, cancellationToken)
            .ConfigureAwait(false);
    }

    HttpClient CreateManagementHttpClient()
    {
        HttpClient client = _createManagementHttpClient();
        return client ?? throw new InvalidOperationException(
            "The RabbitMQ management-client factory returned null.");
    }

    HttpClient CreateDefaultManagementHttpClient() =>
        RabbitMqManagementApi.CreateClient(_transportOptions.User, _transportOptions.Pass);

    Uri BuildManagementUri(string path)
    {
        return RabbitMqManagementApi.BuildUri(
            _transportOptions.Host,
            _transportOptions.ManagementPort,
            _transportOptions.UseSsl,
            path);
    }

    static string EncodeVirtualHost(string? virtualHost)
    {
        return RabbitMqManagementApi.EncodeVirtualHost(virtualHost);
    }
}
