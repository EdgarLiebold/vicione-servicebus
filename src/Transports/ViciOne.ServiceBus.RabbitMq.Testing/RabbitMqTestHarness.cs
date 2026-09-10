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
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Hosts an isolated RabbitMQ bus and provides virtual-host cleanup for transport tests.</summary>
public class RabbitMqTestHarness :
    BusTestHarness
{
    Uri? _hostAddress;
    Uri? _inputQueueAddress;
    string? _clusterNodeAddress;
    int _managementPort = ReadPort(ManagementPortVariable, DefaultManagementPort);
    string _password = null!;
    string _username = null!;

    /// <summary>Identifies the environment variable that supplies the broker user name.</summary>
    public const string UsernameVariable = "VICIONE_SERVICEBUS_RMQ_USER";

    /// <summary>Identifies the environment variable that supplies the broker password.</summary>
    public const string PasswordVariable = "VICIONE_SERVICEBUS_RMQ_PASS";

    /// <summary>Identifies the environment variable that supplies the broker host.</summary>
    public const string HostVariable = "VICIONE_SERVICEBUS_RMQ_HOST";

    /// <summary>Identifies the environment variable that supplies the AMQP port.</summary>
    public const string PortVariable = "VICIONE_SERVICEBUS_RMQ_PORT";

    /// <summary>Identifies the environment variable that supplies the management API port.</summary>
    public const string ManagementPortVariable = "VICIONE_SERVICEBUS_RMQ_MGMT_PORT";

    const int DefaultManagementPort = 15672;

    /// <summary>Gets or sets the TCP port of the RabbitMQ management API.</summary>
    public int ManagementPort
    {
        get => _managementPort;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 65535);
            _managementPort = value;
        }
    }

    static int ReadPort(string variable, int fallback)
    {
        string? value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrEmpty(value))
            return fallback;
        if (int.TryParse(value, out int port) && port is >= 1 and <= 65535)
            return port;

        throw new InvalidOperationException(
            $"Environment variable '{variable}' must contain a TCP port from 1 through 65535.");
    }

    static Uri ReadHostAddress()
    {
        string? host = Environment.GetEnvironmentVariable(HostVariable);
        if (string.IsNullOrWhiteSpace(host))
            host = "localhost";
        if (!Uri.TryCreate($"rabbitmq://{host}/test/", UriKind.Absolute, out Uri? configuredAddress)
            || configuredAddress.Host.Length == 0
            || configuredAddress.UserInfo.Length > 0
            || !configuredAddress.IsDefaultPort
            || configuredAddress.Query.Length > 0
            || configuredAddress.Fragment.Length > 0
            || configuredAddress.AbsolutePath != "/test/")
        {
            throw new InvalidOperationException(
                $"Environment variable '{HostVariable}' must contain a RabbitMQ host name without a scheme, port, or path.");
        }

        var port = ReadPort(PortVariable, 0);

        return new UriBuilder("rabbitmq", configuredAddress.Host, port > 0 ? port : -1, "test/").Uri;
    }

    /// <summary>Creates a harness from environment-provided broker settings or local development defaults.</summary>
    /// <param name="inputQueueName">The receive queue name, or <c>input_queue</c> when omitted.</param>
    public RabbitMqTestHarness(string? inputQueueName = null)
    {
        Username = Environment.GetEnvironmentVariable(UsernameVariable) ?? "guest";
        Password = Environment.GetEnvironmentVariable(PasswordVariable) ?? "guest";

        string effectiveInputQueueName = inputQueueName ?? "input_queue";
        ArgumentException.ThrowIfNullOrWhiteSpace(effectiveInputQueueName, nameof(inputQueueName));
        InputQueueName = effectiveInputQueueName;

        HostAddress = ReadHostAddress();
    }

    /// <summary>Gets or sets the RabbitMQ host and virtual-host address.</summary>
    public Uri HostAddress
    {
        get => _hostAddress ?? throw new InvalidOperationException("The RabbitMQ host address has not been configured.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!value.IsAbsoluteUri
                || value.Scheme is not ("rabbitmq" or "rabbitmqs")
                || value.Host.Length == 0
                || value.UserInfo.Length > 0
                || value.Query.Length > 0
                || value.Fragment.Length > 0)
            {
                throw new ArgumentException(
                    "The RabbitMQ host address must be an absolute rabbitmq or rabbitmqs URI without credentials, a query, or a fragment.",
                    nameof(value));
            }

            _hostAddress = value;
            _inputQueueAddress = new Uri($"queue:{InputQueueName}");
        }
    }

    /// <summary>Gets or sets the RabbitMQ user name.</summary>
    public string Username
    {
        get => _username;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _username = value;
        }
    }
    /// <summary>Gets or sets the RabbitMQ password.</summary>
    public string Password
    {
        get => _password;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _password = value;
        }
    }
    /// <summary>Gets or sets whether the virtual host is cleaned before the test bus is created.</summary>
    public bool CleanVirtualHostOnStart { get; set; } = true;
    /// <summary>Gets the receive queue name used by the test bus.</summary>
    public override string InputQueueName { get; }
    /// <summary>Gets or sets the optional RabbitMQ cluster node as a host or host-and-port authority.</summary>
    public string? ClusterNodeAddress
    {
        get => _clusterNodeAddress;
        set => _clusterNodeAddress = NormalizeClusterNodeAddress(value);
    }

    /// <summary>Gets the transport address of the harness receive queue.</summary>
    public override Uri InputQueueAddress => _inputQueueAddress
        ?? throw new InvalidOperationException("The RabbitMQ input queue address has not been configured.");

    /// <summary>Occurs while the RabbitMQ bus factory is being configured.</summary>
    public event Action<IRabbitMqBusFactoryConfigurator>? RabbitMqConfiguring;
    /// <summary>Occurs while the provider-specific receive endpoint is being configured.</summary>
    public event Action<IRabbitMqReceiveEndpointConfigurator>? RabbitMqReceiveEndpointConfiguring;
    /// <summary>Occurs while RabbitMQ connection settings are being configured.</summary>
    public event Action<IRabbitMqHostConfigurator>? RabbitMqHostConfiguring;

    /// <summary>Gets or sets asynchronous work invoked with an open channel after built-in cleanup.</summary>
    public Func<IChannel, CancellationToken, Task>? CleanupVirtualHostAsync { get; set; }

    /// <summary>Invokes subscribers that customize the RabbitMQ bus.</summary>
    /// <param name="configurator">The bus factory configurator.</param>
    protected virtual void ConfigureRabbitMqBus(IRabbitMqBusFactoryConfigurator configurator)
    {
        RabbitMqConfiguring?.Invoke(configurator);
    }

    /// <summary>Invokes subscribers that customize the harness receive endpoint.</summary>
    /// <param name="configurator">The RabbitMQ receive-endpoint configurator.</param>
    protected virtual void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
    {
        RabbitMqReceiveEndpointConfiguring?.Invoke(configurator);
    }

    /// <summary>Invokes subscribers that customize the RabbitMQ host.</summary>
    /// <param name="configurator">The RabbitMQ host configurator.</param>
    protected virtual void ConfigureRabbitMqHost(IRabbitMqHostConfigurator configurator)
    {
        RabbitMqHostConfiguring?.Invoke(configurator);
    }

    /// <summary>Removes test resources from the RabbitMQ virtual host.</summary>
    /// <param name="channel">The open channel used by custom cleanup.</param>
    /// <param name="cancellationToken">The token that cancels custom cleanup.</param>
    /// <returns>A task that completes when custom cleanup completes.</returns>
    protected virtual Task RunCustomCleanupAsync(IChannel channel, CancellationToken cancellationToken)
    {
        return CleanupVirtualHostAsync is { } cleanup
            ? cleanup(channel, cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Applies harness credentials, cluster selection, and host callbacks to the bus factory.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    protected virtual void ConfigureHost(IRabbitMqBusFactoryConfigurator configurator)
    {
        configurator.Host(HostAddress, h =>
        {
            ConfigureHostSettings(h);
        });
    }

    /// <summary>Builds the effective RabbitMQ host settings used for direct cleanup connections.</summary>
    /// <returns>The configured host settings.</returns>
    public RabbitMqHostSettings GetHostSettings()
    {
        var host = new RabbitMqHostConfigurator(HostAddress);

        ConfigureHostSettings(host);

        return host.Settings;
    }

    /// <summary>Deletes every non-system exchange and queue currently listed by the management API.</summary>
    /// <param name="cancellationToken">Cancellation for AMQP connection, deletion, and close operations.</param>
    /// <returns>A task that completes after the listed broker entities have been deleted.</returns>
    public override async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        var settings = GetHostSettings();

        var connectionFactory = settings.GetConnectionFactory();

        await using var connection = settings.EndpointResolver != null
            ? await connectionFactory.CreateConnectionAsync(settings.EndpointResolver, settings.Host, cancellationToken: cancellationToken)
            : await connectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken);

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        IList<string> exchanges = await GetVirtualHostEntitiesAsync("exchanges", cancellationToken).ConfigureAwait(false);
        foreach (string exchange in exchanges)
            await channel.ExchangeDeleteAsync(exchange, cancellationToken: cancellationToken).ConfigureAwait(false);

        IList<string> queues = await GetVirtualHostEntitiesAsync("queues", cancellationToken).ConfigureAwait(false);
        foreach (string queue in queues)
            await channel.QueueDeleteAsync(queue, cancellationToken: cancellationToken).ConfigureAwait(false);

        await RunCustomCleanupAsync(channel, cancellationToken).ConfigureAwait(false);

        await channel.CloseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        CleanVirtualHostOnStart = false;
    }

    /// <summary>
    /// Drops the virtual host and creates it again, which is the only reset that is guaranteed to
    /// be complete.
    /// <para>
    /// <see cref="CleanAsync" /> enumerates exchanges and queues and deletes them one by one. That
    /// leaves behind anything a plugin keeps outside those two entity types — most notably the
    /// scheduled message store of the delayed message exchange. Recreating the virtual host
    /// removes that store with it, because the store belongs to the virtual host.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">The token that cancels management API operations.</param>
    /// <returns>A task that completes after the dedicated virtual host has been deleted and recreated.</returns>
    public async Task RecreateVirtualHostAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string virtualHost = Uri.UnescapeDataString(HostAddress.AbsolutePath.Trim('/'));
        if (string.IsNullOrWhiteSpace(virtualHost))
        {
            throw new InvalidOperationException(
                "Refusing to recreate the root virtual host. The test fixture must run against a dedicated virtual host.");
        }

        using HttpClient client = CreateManagementHttpClient();
        Uri requestUri = BuildManagementUri($"api/vhosts/{Uri.EscapeDataString(virtualHost)}");

        using HttpResponseMessage delete = await client
            .DeleteAsync(requestUri, cancellationToken)
            .ConfigureAwait(false);
        if (delete.StatusCode != HttpStatusCode.NoContent && delete.StatusCode != HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Deleting the virtual host '{virtualHost}' failed with {(int)delete.StatusCode} {delete.ReasonPhrase}.");
        }

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using HttpResponseMessage create = await client
            .PutAsync(requestUri, content, cancellationToken)
            .ConfigureAwait(false);
        if (create.StatusCode != HttpStatusCode.Created && create.StatusCode != HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException(
                $"Creating the virtual host '{virtualHost}' failed with {(int)create.StatusCode} {create.ReasonPhrase}.");
        }

        CleanVirtualHostOnStart = false;
    }

    async Task<IList<string>> GetVirtualHostEntitiesAsync(string element, CancellationToken cancellationToken)
    {
        using HttpClient client = CreateManagementHttpClient();
        string virtualHost = EncodeVirtualHost(HostAddress);
        Uri requestUri = BuildManagementUri($"api/{element}/{virtualHost}");
        byte[] bytes = await client.GetByteArrayAsync(requestUri, cancellationToken).ConfigureAwait(false);

        var rootElement = JsonSerializer.Deserialize<JsonElement>(bytes, ServiceBusMetadataJson.Options);

        var entities = rootElement.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();

        return entities
            .OfType<string>()
            .Where(static name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith("amq.", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Creates the RabbitMQ test bus and optionally cleans its virtual host before returning it.</summary>
    /// <param name="cancellationToken">The token that cancels virtual-host cleanup.</param>
    /// <returns>The configured bus control.</returns>
    protected override async Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var busControl = ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingRabbitMq(x =>
        {
            ConfigureHost(x);

            ConfigureBus(x);

            ConfigureRabbitMqBus(x);

            x.ReceiveEndpoint(InputQueueName, e =>
            {
                e.PrefetchCount = 16;
                e.PurgeOnStartup = true;

                ConfigureReceiveEndpoint(e);

                ConfigureRabbitMqReceiveEndpoint(e);

                _inputQueueAddress = e.InputAddress;
            });
        });

        if (CleanVirtualHostOnStart)
            await CleanAsync(cancellationToken).ConfigureAwait(false);

        return busControl;
    }

    void ConfigureHostSettings(IRabbitMqHostConfigurator configurator)
    {
        configurator.Username(Username);
        configurator.Password(Password);

        if (ClusterNodeAddress is { } clusterNodeAddress)
            configurator.UseCluster(c => c.Node(clusterNodeAddress));

        ConfigureRabbitMqHost(configurator);
    }

    /// <summary>Creates an authenticated management API client for virtual-host operations.</summary>
    /// <returns>An HTTP client configured with the harness credentials.</returns>
    protected virtual HttpClient CreateManagementHttpClient()
    {
        var client = new HttpClient();
        byte[] credentials = Encoding.UTF8.GetBytes($"{Username}:{Password}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
        return client;
    }

    Uri BuildManagementUri(string path)
    {
        string host = HostAddress.Host;
        if (ClusterNodeAddress is { } clusterNodeAddress)
        {
            var nodeUri = new Uri($"rabbitmq://{clusterNodeAddress}");

            host = nodeUri.Host;
        }

        string scheme = HostAddress.Scheme == "rabbitmqs" ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        return new UriBuilder(scheme, host, ManagementPort, path).Uri;
    }

    static string EncodeVirtualHost(Uri hostAddress)
    {
        string path = Uri.UnescapeDataString(hostAddress.AbsolutePath.Trim('/'));
        string name = string.IsNullOrWhiteSpace(path) ? "/" : path;
        return Uri.EscapeDataString(name);
    }

    static string? NormalizeClusterNodeAddress(string? value)
    {
        if (value == null)
            return null;

        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Uri.TryCreate($"rabbitmq://{value}", UriKind.Absolute, out Uri? nodeUri)
            || nodeUri.Host.Length == 0
            || nodeUri.UserInfo.Length > 0
            || nodeUri.AbsolutePath != "/"
            || nodeUri.Query.Length > 0
            || nodeUri.Fragment.Length > 0)
        {
            throw new ArgumentException(
                "The cluster node address must contain only a host and optional AMQP port.",
                nameof(value));
        }

        string host = nodeUri.HostNameType == UriHostNameType.IPv6 ? $"[{nodeUri.Host}]" : nodeUri.Host;
        return nodeUri.IsDefaultPort ? host : $"{host}:{nodeUri.Port}";
    }
}
