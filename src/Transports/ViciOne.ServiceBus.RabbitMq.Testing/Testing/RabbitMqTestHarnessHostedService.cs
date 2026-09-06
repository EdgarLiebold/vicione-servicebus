using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Creates, cleans, and configures the RabbitMQ virtual host before the application host starts.</summary>
public class RabbitMqTestHarnessHostedService :
    IHostedService
{
    readonly ILogger<RabbitMqTestHarnessHostedService> _logger;
    readonly RabbitMqTestHarnessOptions _testOptions;
    readonly RabbitMqTransportOptions _transportOptions;

    /// <summary>Creates the hosted service from the effective transport and test-harness options.</summary>
    /// <param name="transportOptions">Connection and management settings for the RabbitMQ broker.</param>
    /// <param name="testOptions">Virtual-host preparation settings.</param>
    /// <param name="logger">The logger used for preparation and cleanup diagnostics.</param>
    public RabbitMqTestHarnessHostedService(IOptions<RabbitMqTransportOptions> transportOptions, IOptions<RabbitMqTestHarnessOptions> testOptions,
        ILogger<RabbitMqTestHarnessHostedService> logger)
    {
        _logger = logger;
        _transportOptions = transportOptions.Value;
        _testOptions = testOptions.Value;
    }

    /// <summary>Performs the configured virtual-host creation, cleanup, and configuration sequence.</summary>
    /// <param name="cancellationToken">Cancellation checked before broker preparation begins.</param>
    /// <returns>A task that completes when virtual-host preparation has finished.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_testOptions.CreateVirtualHostIfNotExists)
            await EnsureVirtualHostExistsAsync();

        if (_testOptions.CleanVirtualHost)
            await CleanVirtualHostAsync();

        if (_testOptions.ConfigureVirtualHostCallback is { } configureVirtualHost)
            await ConfigureVirtualHostAsync(configureVirtualHost);
    }

    /// <summary>Completes immediately because the service owns no resources after startup.</summary>
    /// <param name="cancellationToken">Cancellation that faults the returned task when already requested.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    async Task EnsureVirtualHostExistsAsync()
    {
        var name = _transportOptions.VHost;

        if (string.IsNullOrWhiteSpace(name) || name == "/")
            return;

        using var client = GetHttpClient();

        var builder = GetUriBuilder($"api/vhosts/{name}");

        var responseMessage = await client.PutAsync(builder.Uri, new StringContent("{}", Encoding.UTF8, "application/json"));

        responseMessage.EnsureSuccessStatusCode();

        if (responseMessage.StatusCode == HttpStatusCode.Created)
            _logger.LogInformation("Created virtual host: {VirtualHost}", name);
    }

    async Task CleanVirtualHostAsync()
    {
        var virtualHost = _transportOptions.VHost;

        if (string.IsNullOrWhiteSpace(virtualHost) || virtualHost == "/")
        {
            if (!_testOptions.ForceCleanRootVirtualHost)
            {
                const string message = "CleanVirtualHost was specified on the root virtual host without ForceCleanRootVirtualHost";
                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }
        }

        var factory = new ConnectionFactory
        {
            HostName = _transportOptions.Host,
            Port = _transportOptions.Port,
            VirtualHost = virtualHost ?? "/",
            UserName = _transportOptions.User,
            Password = _transportOptions.Pass
        };

        var connection = await factory.CreateConnectionAsync();
        try
        {
            await using var channel = await connection.CreateChannelAsync();

            var exchangeCount = 0;
            var queueCount = 0;

            IList<string> exchanges = await GetVirtualHostEntitiesAsync("exchanges");
            foreach (var exchange in exchanges)
            {
                await channel.ExchangeDeleteAsync(exchange);
                exchangeCount++;
            }

            IList<string> queues = await GetVirtualHostEntitiesAsync("queues");
            foreach (var queue in queues)
            {
                await channel.QueueDeleteAsync(queue);
                queueCount++;
            }

            await channel.CloseAsync();

            if (exchangeCount > 0 || queueCount > 0)
                _logger.LogInformation("Removed {QueueCount} queue(s), {ExchangeCount} exchange(s)", queueCount, exchangeCount);

            await connection.CloseAsync(200, "Completed (Ok)");
        }
        catch (Exception ex)
        {
            // Setup failure remains authoritative while connection cleanup is best effort.
            _logger.LogError(ex, "Preparing the virtual host failed");

            if (connection.IsOpen)
            {
                try
                {
                    await connection.CloseAsync(500, CloseReason($"Completed (not OK): {ex.Message}"));
                }
                catch (Exception closeException)
                {
                    // Closing stays best effort. A failure here is diagnosable on its own and must
                    // neither replace nor swallow the setup failure.
                    _logger.LogDebug(closeException, "Closing the connection after a failed clean up faulted");
                }
            }

            // Bare rethrow preserves the primary setup failure and its stack.
            throw;
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
        while (length > 0 && Encoding.UTF8.GetByteCount(text.Substring(0, length)) > maximumBytes)
            length--;

        // A prefix ending in a high surrogate would split its Unicode scalar value.
        if (length > 0 && char.IsHighSurrogate(text[length - 1]))
            length--;

        return text.Substring(0, length);
    }

    async Task ConfigureVirtualHostAsync(Func<IChannel, Task> configureVirtualHost)
    {
        var virtualHost = _transportOptions.VHost;

        var factory = new ConnectionFactory
        {
            HostName = _transportOptions.Host,
            Port = _transportOptions.Port,
            VirtualHost = virtualHost ?? "/",
            UserName = _transportOptions.User,
            Password = _transportOptions.Pass
        };

        var connection = await factory.CreateConnectionAsync();
        try
        {
            await using var channel = await connection.CreateChannelAsync();

            await configureVirtualHost(channel);

            await channel.CloseAsync();

            await connection.CloseAsync(200, "Completed (Ok)");
        }
        catch (Exception ex)
        {
            // Setup failure remains authoritative while connection cleanup is best effort.
            _logger.LogError(ex, "Preparing the virtual host failed");

            if (connection.IsOpen)
            {
                try
                {
                    await connection.CloseAsync(500, CloseReason($"Completed (not OK): {ex.Message}"));
                }
                catch (Exception closeException)
                {
                    // Closing stays best effort. A failure here is diagnosable on its own and must
                    // neither replace nor swallow the setup failure.
                    _logger.LogDebug(closeException, "Closing the connection after a failed clean up faulted");
                }
            }

            // Bare rethrow preserves the primary setup failure and its stack.
            throw;
        }
    }

    async Task<IList<string>> GetVirtualHostEntitiesAsync(string element)
    {
        using var client = GetHttpClient();

        var builder = GetUriBuilder($"api/{element}/{_transportOptions.VHost.Trim('/')}");

        var bytes = await client.GetByteArrayAsync(builder.Uri);

        var rootElement = JsonSerializer.Deserialize<JsonElement>(bytes, ServiceBusMetadataJson.Options);

        var entities = rootElement.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();

        return entities.OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("amq.")).ToList();
    }

    HttpClient GetHttpClient()
    {
        var client = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes($"{_transportOptions.User}:{_transportOptions.Pass}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        return client;
    }

    UriBuilder GetUriBuilder(string pathValue)
    {
        return new UriBuilder(_transportOptions.UseSsl ? "https" : "http", _transportOptions.Host, _transportOptions.ManagementPort, pathValue);
    }
}
