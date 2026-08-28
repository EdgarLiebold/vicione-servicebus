namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;

using Apache.NMS;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

internal sealed class ActiveMqBroker : IDisposable
{
    public const string OpenWireFlavor = "activemq";
    public const string AmqpFlavor = "amqp";
    public const string ArtemisFlavor = "artemis";

    private readonly HttpClient? _managementClient;
    private readonly string _password;
    private readonly string _userName;

    private ActiveMqBroker(
        string flavor,
        Uri address,
        Uri? managementAddress,
        string userName,
        string password,
        TimeSpan operationTimeout,
        string prefix)
    {
        Flavor = flavor;
        Address = address;
        _userName = userName;
        _password = password;
        OperationTimeout = operationTimeout;
        Prefix = prefix;

        if (managementAddress is not null)
        {
            _managementClient = new HttpClient { BaseAddress = managementAddress };
            _managementClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
            _managementClient.DefaultRequestHeaders.Add("Origin", "http://localhost");
        }
    }

    public string Flavor { get; }
    public Uri Address { get; }
    public TimeSpan OperationTimeout { get; }
    public string Prefix { get; }

    public static ActiveMqBroker Create(string flavor, string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flavor);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        TestConfigurationProvider provider = TestConfigurationProvider.ForCurrentTestRun();
        ViciOneTestOptions options;
        string host;
        int port;
        int? managementPort;
        string userName;
        string password;
        string scheme;

        switch (flavor)
        {
            case OpenWireFlavor:
            case AmqpFlavor:
            {
                options = provider.GetValidatedLocalOptions(LocalTestResource.ActiveMq);
                ActiveMqLocalOptions activeMq = options.LocalInfrastructure!.ActiveMq!;
                host = activeMq.Host!;
                port = flavor == OpenWireFlavor
                    ? activeMq.OpenWirePort!.Value
                    : activeMq.AmqpPort!.Value;
                managementPort = activeMq.JolokiaPort!.Value;
                userName = activeMq.UserName!;
                password = activeMq.Password!;
                scheme = flavor == OpenWireFlavor
                    ? ActiveMqHostAddress.ActiveMqScheme
                    : ActiveMqHostAddress.AmqpScheme;
                break;
            }
            case ArtemisFlavor:
            {
                options = provider.GetValidatedLocalOptions(LocalTestResource.Artemis);
                ArtemisLocalOptions artemis = options.LocalInfrastructure!.Artemis!;
                host = artemis.Host!;
                port = artemis.Port!.Value;
                managementPort = null;
                userName = artemis.UserName!;
                password = artemis.Password!;
                scheme = ActiveMqHostAddress.AmqpScheme;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(flavor),
                    flavor,
                    $"The broker flavor must be one of '{OpenWireFlavor}', '{AmqpFlavor}', or '{ArtemisFlavor}'.");
        }

        return new ActiveMqBroker(
            flavor,
            new UriBuilder(scheme, host, port, "/").Uri,
            managementPort is null ? null : new UriBuilder(Uri.UriSchemeHttp, host, managementPort.Value, "/").Uri,
            userName,
            password,
            options.OperationTimeout!.Value,
            CreatePrefix(purpose, Guid.NewGuid()));
    }

    internal static string CreatePrefix(string purpose, Guid runId)
    {
        string normalized = new(purpose.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        if (normalized.Length == 0)
            throw new ArgumentException("The purpose must contain at least one ASCII letter or digit.", nameof(purpose));
        if (normalized.Length > 16)
            normalized = normalized[..16];

        return $"vsb-{normalized}-{runId:N}";
    }

    public string Name(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        string suffix = new(purpose.ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '-')
            .ToArray());
        if (suffix.Length == 0)
            throw new ArgumentException("The purpose must contain at least one broker-name character.", nameof(purpose));

        return $"{Prefix}-{suffix}";
    }

    public void ConfigureHost(IActiveMqBusFactoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Host(Address, host =>
        {
            host.Username(_userName);
            host.Password(_password);
        });

        if (Flavor == ArtemisFlavor)
        {
            configurator.EnableArtemisCompatibility();
            configurator.SetTemporaryQueueNamePrefix(Prefix + ".");
        }
    }

    public IConnection CreateConnection()
    {
        ConfigurationHostSettings settings = Address.Scheme == ActiveMqHostAddress.AmqpScheme
            ? new AmqpHostSettings(Address)
            : new OpenWireHostSettings(Address);
        settings.Username = _userName;
        settings.Password = _password;
        return settings.CreateConnection();
    }

    public async Task<ClassicTopicStatistics> GetClassicTopicStatistics(
        string topicName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        HttpClient client = _managementClient
            ?? throw new InvalidOperationException("Classic ActiveMQ management is unavailable for the Artemis fixture.");
        string mbean =
            $"org.apache.activemq:type=Broker,brokerName=localhost,destinationType=Topic,destinationName={topicName}";
        string payload = JsonSerializer.Serialize(new
        {
            type = "read",
            mbean,
            attribute = new[] { "EnqueueCount", "ConsumerCount" },
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/jolokia/")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        string content = await response.Content.ReadAsStringAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(content);
        JsonElement root = document.RootElement;
        int status = root.GetProperty("status").GetInt32();
        if (status != 200)
            throw new InvalidDataException($"Jolokia returned status {status} for topic '{topicName}': {content}");

        JsonElement value = root.GetProperty("value");
        return new ClassicTopicStatistics(
            value.GetProperty("EnqueueCount").GetInt64(),
            value.GetProperty("ConsumerCount").GetInt32());
    }

    public async Task<ClassicQueueStatistics> GetClassicQueueStatistics(
        string queueName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        HttpClient client = _managementClient
            ?? throw new InvalidOperationException("Classic ActiveMQ management is unavailable for the Artemis fixture.");
        string mbean =
            $"org.apache.activemq:type=Broker,brokerName=localhost,destinationType=Queue,destinationName={queueName}";
        string payload = JsonSerializer.Serialize(new
        {
            type = "read",
            mbean,
            attribute = new[] { "EnqueueCount", "DequeueCount", "QueueSize" },
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/jolokia/")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        string content = await response.Content.ReadAsStringAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(content);
        JsonElement root = document.RootElement;
        int status = root.GetProperty("status").GetInt32();
        if (status != 200)
            throw new InvalidDataException($"Jolokia returned status {status} for queue '{queueName}': {content}");

        JsonElement value = root.GetProperty("value");
        return new ClassicQueueStatistics(
            value.GetProperty("EnqueueCount").GetInt64(),
            value.GetProperty("DequeueCount").GetInt64(),
            value.GetProperty("QueueSize").GetInt64());
    }

    public void Dispose() => _managementClient?.Dispose();

    internal readonly record struct ClassicTopicStatistics(long EnqueueCount, int ConsumerCount);
    internal readonly record struct ClassicQueueStatistics(long EnqueueCount, long DequeueCount, long QueueSize);
}
