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

public class RabbitMqTestHarnessHostedService :
    IHostedService
{
    readonly ILogger<RabbitMqTestHarnessHostedService> _logger;
    readonly RabbitMqTestHarnessOptions _testOptions;
    readonly RabbitMqTransportOptions _transportOptions;

    public RabbitMqTestHarnessHostedService(IOptions<RabbitMqTransportOptions> transportOptions, IOptions<RabbitMqTestHarnessOptions> testOptions,
        ILogger<RabbitMqTestHarnessHostedService> logger)
    {
        _logger = logger;
        _transportOptions = transportOptions.Value;
        _testOptions = testOptions.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_testOptions.CreateVirtualHostIfNotExists)
            await EnsureVirtualHostExists();

        if (_testOptions.CleanVirtualHost)
            await CleanVirtualHost();

        if (_testOptions.ConfigureVirtualHostCallback != null)
            await ConfigureVirtualHost();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    async Task EnsureVirtualHostExists()
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

    async Task CleanVirtualHost()
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

            IList<string> exchanges = await GetVirtualHostEntities("exchanges");
            foreach (var exchange in exchanges)
            {
                await channel.ExchangeDeleteAsync(exchange);
                exchangeCount++;
            }

            IList<string> queues = await GetVirtualHostEntities("queues");
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
            // Two different failures, and only one of them is the answer. The setup failure is what
            // actually went wrong and it used to disappear here entirely: the method returned
            // normally, the caller learned nothing, and a broken virtual host first became visible
            // as an unrelated failure in some later spec. Logging it was not enough — a log entry is
            // not an error contract — so it is rethrown below with its own type, cause and stack.
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

            // Bare rethrow: the original stack is preserved, which a "throw ex" would discard.
            throw;
        }
    }


    /// <summary>
    /// Fits a connection close reason into what AMQP can carry.
    /// <para>
    /// The reason travels in a shortstr, which holds at most 255 bytes. Both close sites built it
    /// from an exception message, so any message longer than that overflowed the frame buffer and
    /// threw ArgumentException: "The output byte buffer is too small to contain the encoded data,
    /// encoding codepage '65001'". Because that happened inside a catch handler, the real failure
    /// vanished and eight job specs reported an encoding error for a harness start that had failed
    /// for an entirely different reason.
    /// </para>
    /// <para>
    /// Trimming runs over characters rather than bytes so a multi byte character is never cut in
    /// half, which would produce a replacement character and a reason nobody can read.
    /// </para>
    /// </summary>
    static string CloseReason(string text)
    {
        const int maximumBytes = 255;

        if (Encoding.UTF8.GetByteCount(text) <= maximumBytes)
            return text;

        // Substring rather than a span: this assembly also targets netstandard2.0, which has no
        // span overload for GetByteCount. It is a rare error path, so the allocation is cheap.
        var length = Math.Min(text.Length, maximumBytes);
        while (length > 0 && Encoding.UTF8.GetByteCount(text.Substring(0, length)) > maximumBytes)
            length--;

        // Characters are UTF-16 code units, not code points. A character outside the basic plane is
        // a surrogate pair of two of them, and cutting between the two leaves a lone high surrogate
        // that encodes as the replacement character — exactly what the paragraph above promises not
        // to produce. True for two and three byte characters, false for four byte ones until here.
        if (length > 0 && char.IsHighSurrogate(text[length - 1]))
            length--;

        return text.Substring(0, length);
    }

    async Task ConfigureVirtualHost()
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

            await _testOptions.ConfigureVirtualHostCallback(channel);

            await channel.CloseAsync();

            await connection.CloseAsync(200, "Completed (Ok)");
        }
        catch (Exception ex)
        {
            // Two different failures, and only one of them is the answer. The setup failure is what
            // actually went wrong and it used to disappear here entirely: the method returned
            // normally, the caller learned nothing, and a broken virtual host first became visible
            // as an unrelated failure in some later spec. Logging it was not enough — a log entry is
            // not an error contract — so it is rethrown below with its own type, cause and stack.
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

            // Bare rethrow: the original stack is preserved, which a "throw ex" would discard.
            throw;
        }
    }

    async Task<IList<string>> GetVirtualHostEntities(string element)
    {
        using var client = GetHttpClient();

        var builder = GetUriBuilder($"api/{element}/{_transportOptions.VHost.Trim('/')}");

        var bytes = await client.GetByteArrayAsync(builder.Uri);

        var rootElement = JsonSerializer.Deserialize<JsonElement>(bytes, ServiceBusMetadataJson.Options);

        var entities = rootElement.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();

        return entities.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("amq.")).ToList();
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
