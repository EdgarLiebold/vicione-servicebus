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
                managementPort = artemis.JolokiaPort!.Value;
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

    public void ConfigureHost(IActiveMqBusFactoryConfigurator configurator) => ConfigureHost(configurator, null);

    public void ConfigureHost(
        IActiveMqBusFactoryConfigurator configurator,
        Action<IActiveMqHostConfigurator>? configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Host(Address, host =>
        {
            host.Username(_userName);
            host.Password(_password);
            configure?.Invoke(host);
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

    public async Task<bool> ClassicTopicExists(string topicName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        HttpClient client = _managementClient
            ?? throw new InvalidOperationException("Classic ActiveMQ management is unavailable for the Artemis fixture.");
        string mbean =
            $"org.apache.activemq:type=Broker,brokerName=localhost,destinationType=Topic,destinationName={topicName}";
        string payload = JsonSerializer.Serialize(new { type = "read", mbean });
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
        int status = document.RootElement.GetProperty("status").GetInt32();
        return status switch
        {
            200 => true,
            404 => false,
            _ => throw new InvalidDataException(
                $"Jolokia returned status {status} while probing topic '{topicName}': {content}"),
        };
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

    public async Task<BrokerQueueStatistics> GetQueueStatistics(
        string queueName,
        CancellationToken cancellationToken)
    {
        if (Flavor != ArtemisFlavor)
        {
            ClassicQueueStatistics classic = await GetClassicQueueStatistics(queueName, cancellationToken);
            return new BrokerQueueStatistics(
                classic.EnqueueCount,
                classic.DequeueCount,
                classic.QueueSize,
                ScheduledCount: 0,
                DeliveringCount: 0);
        }

        string mbean = await GetArtemisQueueMBean(queueName, cancellationToken);
        _ = await InvokeJolokia(
            new
            {
                type = "exec",
                mbean,
                operation = "flushExecutor()",
                arguments = Array.Empty<object>(),
            },
            cancellationToken);
        JsonElement value = await InvokeJolokia(
            new
            {
                type = "read",
                mbean,
                attribute = new[]
                {
                    "MessagesAdded",
                    "MessagesAcknowledged",
                    "MessageCount",
                    "ScheduledCount",
                    "DeliveringCount",
                },
            },
            cancellationToken);

        return new BrokerQueueStatistics(
            value.GetProperty("MessagesAdded").GetInt64(),
            value.GetProperty("MessagesAcknowledged").GetInt64(),
            value.GetProperty("MessageCount").GetInt64(),
            value.GetProperty("ScheduledCount").GetInt64(),
            value.GetProperty("DeliveringCount").GetInt64());
    }

    public async Task<int> GetScheduledMessageCount(string queueName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        if (Flavor == ArtemisFlavor)
            return checked((int)(await GetQueueStatistics(queueName, cancellationToken)).ScheduledCount);

        JsonElement jobs = await InvokeJolokia(
            new
            {
                type = "exec",
                mbean = "org.apache.activemq:type=Broker,brokerName=localhost,service=JobScheduler,name=JMS",
                operation = "getAllJobs(boolean)",
                arguments = new object[] { true },
            },
            cancellationToken);
        return CountScheduledDestinations(jobs, queueName);
    }

    async Task<string> GetArtemisQueueMBean(string queueName, CancellationToken cancellationToken)
    {
        JsonElement value = await InvokeJolokia(
            new
            {
                type = "search",
                mbean = "org.apache.activemq.artemis:broker=*,component=addresses,address=*,subcomponent=queues,routing-type=*,queue=*",
            },
            cancellationToken);
        string escapedQueueName = queueName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        string[] queueProperties = [$",queue=\"{escapedQueueName}\"", $",queue=\"{escapedQueueName}:global\""];
        string[] available =
        [
            .. value.EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => item is not null)!
        ];
        string[] matches =
        [
            .. available.Where(item => queueProperties.Any(property => item.Contains(property, StringComparison.Ordinal)))
        ];

        return matches.Length == 1
            ? matches[0]
            : throw new InvalidDataException(
                $"Expected exactly one Artemis queue MBean for '{queueName}', but found {matches.Length}. "
                + $"Available queue MBeans: {string.Join(", ", available)}");
    }

    async Task<JsonElement> InvokeJolokia(object command, CancellationToken cancellationToken)
    {
        HttpClient client = _managementClient
            ?? throw new InvalidOperationException("Broker management is unavailable for this fixture.");
        string payload = JsonSerializer.Serialize(command);
        string relativePath = Flavor == ArtemisFlavor ? "console/jolokia/" : "api/jolokia/";
        using var request = new HttpRequestMessage(HttpMethod.Post, relativePath)
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
            throw new InvalidDataException($"Jolokia returned status {status}: {content}");

        return root.GetProperty("value").Clone();
    }

    static int CountScheduledDestinations(JsonElement element, string queueName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("destinationName", out JsonElement destination)
                && destination.ValueKind == JsonValueKind.String)
            {
                string? value = destination.GetString();
                return value == queueName || value == $"queue://{queueName}" || value == $"queue:{queueName}" ? 1 : 0;
            }

            return element.EnumerateObject().Sum(property => CountScheduledDestinations(property.Value, queueName));
        }

        if (element.ValueKind == JsonValueKind.Array)
            return element.EnumerateArray().Sum(item => CountScheduledDestinations(item, queueName));

        return 0;
    }

    public async Task<bool> ClassicQueueExists(string queueName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        HttpClient client = _managementClient
            ?? throw new InvalidOperationException("Classic ActiveMQ management is unavailable for the Artemis fixture.");
        string mbean =
            $"org.apache.activemq:type=Broker,brokerName=localhost,destinationType=Queue,destinationName={queueName}";
        string payload = JsonSerializer.Serialize(new { type = "read", mbean });
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
        int status = document.RootElement.GetProperty("status").GetInt32();
        return status switch
        {
            200 => true,
            404 => false,
            _ => throw new InvalidDataException(
                $"Jolokia returned status {status} while probing queue '{queueName}': {content}"),
        };
    }

    public async Task DeleteClassicQueue(string queueName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        HttpClient client = _managementClient
            ?? throw new InvalidOperationException("Classic ActiveMQ management is unavailable for the Artemis fixture.");
        string payload = JsonSerializer.Serialize(new
        {
            type = "exec",
            mbean = "org.apache.activemq:type=Broker,brokerName=localhost",
            operation = "removeQueue(java.lang.String)",
            arguments = new[] { queueName },
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
        int status = document.RootElement.GetProperty("status").GetInt32();
        if (status != 200)
            throw new InvalidDataException($"Jolokia returned status {status} while deleting queue '{queueName}': {content}");
    }

    public void Dispose() => _managementClient?.Dispose();

    internal readonly record struct ClassicTopicStatistics(long EnqueueCount, int ConsumerCount);
    internal readonly record struct ClassicQueueStatistics(long EnqueueCount, long DequeueCount, long QueueSize);
    internal readonly record struct BrokerQueueStatistics(
        long EnqueueCount,
        long DequeueCount,
        long QueueSize,
        long ScheduledCount,
        long DeliveringCount);
}
