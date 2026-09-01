namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.Infrastructure;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

internal sealed class RabbitMqBroker : IDisposable
{
    private readonly HttpClient _management;
    private readonly string _password;
    private readonly string _userName;

    private RabbitMqBroker(
        Uri address,
        Uri managementAddress,
        string userName,
        string password,
        TimeSpan operationTimeout,
        string prefix)
    {
        Address = address;
        OperationTimeout = operationTimeout;
        Prefix = prefix;
        _userName = userName;
        _password = password;
        _management = new HttpClient { BaseAddress = managementAddress };
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
    }

    public Uri Address { get; }

    public TimeSpan OperationTimeout { get; }

    public string Prefix { get; }

    public static RabbitMqBroker Create(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ViciOneTestOptions options = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.RabbitMq);
        RabbitMqLocalOptions rabbit = options.LocalInfrastructure!.RabbitMq!;
        string normalized = new(purpose.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        if (normalized.Length == 0)
            throw new ArgumentException("The purpose must contain an ASCII letter or digit.", nameof(purpose));
        if (normalized.Length > 16)
            normalized = normalized[..16];

        return new RabbitMqBroker(
            new UriBuilder(RabbitMqHostAddress.RabbitMqScheme, rabbit.Host, rabbit.Port!.Value, "/").Uri,
            new UriBuilder(Uri.UriSchemeHttp, rabbit.Host, rabbit.ManagementPort!.Value, "/").Uri,
            rabbit.UserName!,
            rabbit.Password!,
            options.OperationTimeout!.Value,
            $"vsb-{normalized}-{Guid.NewGuid():N}");
    }

    public string Name(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        string suffix = new(purpose.ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .ToArray());
        if (suffix.Length == 0)
            throw new ArgumentException("The entity purpose must contain a broker-name character.", nameof(purpose));
        return $"{Prefix}-{suffix}";
    }

    public void ConfigureHost(IRabbitMqBusFactoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.Host(Address, host =>
        {
            host.Username(_userName);
            host.Password(_password);
        });
    }

    public void ConfigureClusteredHost(IRabbitMqBusFactoryConfigurator configurator, Uri logicalAddress)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(logicalAddress);
        configurator.Host(logicalAddress, host =>
        {
            host.Username(_userName);
            host.Password(_password);
            host.UseCluster(cluster => cluster.Node($"{Address.Host}:{Address.Port}"));
        });
    }

    public ConnectionFactory CreateConnectionFactory() => new()
    {
        HostName = Address.Host,
        Port = Address.Port,
        VirtualHost = "/",
        UserName = _userName,
        Password = _password,
        RequestedConnectionTimeout = OperationTimeout,
    };

    public CancellationTokenSource OperationCancellation() => new(OperationTimeout);

    public async Task<uint> QueueMessageCount(string queueName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ConnectionFactory factory = CreateConnectionFactory();
        await using IConnection connection = await factory.CreateConnectionAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        QueueDeclareOk queue = await channel.QueueDeclarePassiveAsync(queueName, cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        return queue.MessageCount;
    }

    public async Task WaitUntilQueueIsReleased(string queueName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(OperationTimeout);
        while ((await Queue(queueName, timeout.Token)).Exists)
            timeout.Token.ThrowIfCancellationRequested();
    }

    public async Task<QueueState> Queue(string queueName, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _management.GetAsync(
                $"api/queues/%2F/{Uri.EscapeDataString(queueName)}",
                cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return QueueState.Missing(queueName);
        response.EnsureSuccessStatusCode();

        using JsonDocument json = JsonDocument.Parse(body);
        JsonElement root = json.RootElement;
        return new QueueState(
            queueName,
            true,
            Int64OrZero(root, "messages"),
            Int64OrZero(root, "messages_ready"),
            Int64OrZero(root, "messages_unacknowledged"),
            checked((int)Int64OrZero(root, "consumers")),
            root.GetProperty("durable").GetBoolean(),
            root.GetProperty("auto_delete").GetBoolean(),
            root.GetProperty("exclusive").GetBoolean(),
            Arguments(root));
    }

    public async Task<ExchangeState> Exchange(string exchangeName, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _management.GetAsync(
                $"api/exchanges/%2F/{Uri.EscapeDataString(exchangeName)}",
                cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return ExchangeState.Missing(exchangeName);
        response.EnsureSuccessStatusCode();

        using JsonDocument json = JsonDocument.Parse(body);
        JsonElement root = json.RootElement;
        return new ExchangeState(
            exchangeName,
            true,
            root.GetProperty("type").GetString()!,
            root.GetProperty("durable").GetBoolean(),
            root.GetProperty("auto_delete").GetBoolean(),
            Arguments(root));
    }

    public async Task<IReadOnlyList<BindingState>> QueueBindings(string queueName, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _management.GetAsync(
                $"api/queues/%2F/{Uri.EscapeDataString(queueName)}/bindings",
                cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(body);
        return json.RootElement.EnumerateArray()
            .Select(element => new BindingState(
                element.GetProperty("source").GetString()!,
                element.GetProperty("destination").GetString()!,
                element.GetProperty("destination_type").GetString()!,
                element.GetProperty("routing_key").GetString()!))
            .ToArray();
    }

    public async Task<IReadOnlyList<BindingState>> Bindings(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _management.GetAsync("api/bindings/%2F", cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(body);
        return json.RootElement.EnumerateArray()
            .Select(element => new BindingState(
                element.GetProperty("source").GetString()!,
                element.GetProperty("destination").GetString()!,
                element.GetProperty("destination_type").GetString()!,
                element.GetProperty("routing_key").GetString()!))
            .ToArray();
    }

    public async Task CleanupAsync()
    {
        using CancellationTokenSource timeout = OperationCancellation();
        await DeleteOwned("api/queues/%2F", "name", timeout.Token);
        await DeleteOwned("api/exchanges/%2F", "name", timeout.Token);
    }

    private async Task DeleteOwned(string collectionPath, string nameProperty, CancellationToken cancellationToken)
    {
        string listPath = collectionPath[..collectionPath.LastIndexOf('/', collectionPath.Length - 2)];
        using HttpResponseMessage listResponse = await _management.GetAsync(listPath, cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        string body = await listResponse.Content.ReadAsStringAsync(cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        listResponse.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(body);
        string[] names = json.RootElement.EnumerateArray()
            .Select(element => element.GetProperty(nameProperty).GetString()!)
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .ToArray();
        foreach (string name in names)
        {
            using HttpResponseMessage deleted = await _management.DeleteAsync(
                    collectionPath + "/" + Uri.EscapeDataString(name),
                    cancellationToken)
                .WaitAsync(OperationTimeout, cancellationToken);
            if (deleted.StatusCode is not (HttpStatusCode.NoContent or HttpStatusCode.NotFound))
                deleted.EnsureSuccessStatusCode();
        }
    }

    private static long Int64OrZero(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64()
            : 0;

    private static IReadOnlyDictionary<string, string> Arguments(JsonElement element) =>
        element.TryGetProperty("arguments", out JsonElement arguments) && arguments.ValueKind == JsonValueKind.Object
            ? arguments.EnumerateObject().ToDictionary(
                property => property.Name,
                property => property.Value.ToString(),
                StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

    public void Dispose() => _management.Dispose();

    internal sealed record QueueState(
        string Name,
        bool Exists,
        long Messages,
        long Ready,
        long Unacknowledged,
        int Consumers,
        bool Durable,
        bool AutoDelete,
        bool Exclusive,
        IReadOnlyDictionary<string, string> Arguments)
    {
        public static QueueState Missing(string name) => new(
            name,
            false,
            0,
            0,
            0,
            0,
            false,
            false,
            false,
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    internal sealed record ExchangeState(
        string Name,
        bool Exists,
        string Type,
        bool Durable,
        bool AutoDelete,
        IReadOnlyDictionary<string, string> Arguments)
    {
        public static ExchangeState Missing(string name) => new(
            name,
            false,
            string.Empty,
            false,
            false,
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    internal sealed record BindingState(string Source, string Destination, string DestinationType, string RoutingKey);
}
