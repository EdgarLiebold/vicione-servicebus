using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.SignalR.Consumers;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Scoping;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class HubLifetimeManagerTestEnvironment<THub> : IAsyncDisposable
    where THub : Hub
{
    private readonly InMemoryTestHarness _harness;
    private readonly RecordingLogger _logger;
    private readonly ILogContext? _previousLogContext;

    private HubLifetimeManagerTestEnvironment(
        InMemoryTestHarness harness,
        IReadOnlyList<SignalRBackplaneEndpoint<THub>> endpoints,
        RecordingLogger logger,
        ILogContext? previousLogContext)
    {
        _harness = harness;
        Endpoints = endpoints;
        _logger = logger;
        _previousLogContext = previousLogContext;
    }

    public IReadOnlyList<SignalRBackplaneEndpoint<THub>> Endpoints { get; }

    public TimeSpan Timeout => _harness.TestTimeout;

    public static async Task<HubLifetimeManagerTestEnvironment<THub>> StartAsync(
        int endpointCount,
        Func<int, IReadOnlyList<IHubProtocol>>? protocolFactory = null)
    {
        if (endpointCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endpointCount));
        }

        ILogContext? previousLogContext = LogContext.Current;
        var logger = new RecordingLogger();
        LogContext.ConfigureCurrentLogContext(logger);

        var harness = new InMemoryTestHarness($"signalr-{NewId.NextGuid():N}");
        var endpoints = Enumerable.Range(0, endpointCount)
            .Select(index => new SignalRBackplaneEndpoint<THub>(harness, $"signalr-endpoint-{index}"))
            .ToArray();
        var environment = new HubLifetimeManagerTestEnvironment<THub>(
            harness,
            endpoints,
            logger,
            previousLogContext);

        try
        {
            await harness.Start().ConfigureAwait(false);

            for (var index = 0; index < endpoints.Length; index++)
            {
                endpoints[index].Attach(
                    environment.CreateManager(index, protocolFactory?.Invoke(index)));
            }

            return environment;
        }
        catch
        {
            await environment.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public Task<ConsumeContext<Ack<THub>>> ObserveNextAcknowledgement() =>
        _harness.SubscribeHandler<Ack<THub>>();

    public Task<LogEntry> ObserveLogAsync(
        Func<LogEntry, bool> predicate,
        CancellationToken cancellationToken = default) =>
        _logger.WaitForAsync(predicate, Timeout, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _harness.Stop().ConfigureAwait(false);
            _harness.Dispose();
        }
        finally
        {
            LogContext.Current = _previousLogContext!;
        }
    }

    private ViciOneServiceBusHubLifetimeManager<THub> CreateManager(
        int index,
        IReadOnlyList<IHubProtocol>? protocols)
    {
        var availableProtocols = protocols ?? [new JsonHubProtocol()];

        return new ViciOneServiceBusHubLifetimeManager<THub>(
            new HubLifetimeManagerOptions<THub>
            {
                ServerName = $"signalr-test-server-{index}",
                RequestTimeout = Timeout,
            },
            new BusHubLifetimeScopeProvider(_harness.Bus),
            new TestHubProtocolResolver(availableProtocols));
    }
}

internal sealed class SignalRBackplaneEndpoint<THub>
    where THub : Hub
{
    private readonly IReadOnlyList<IHubLifetimeManagerConsumerFactory<THub>> _factories;

    public SignalRBackplaneEndpoint(BusTestHarness harness, string queuePrefix)
    {
        var allFactory = Factory<AllConsumer<THub>>(manager => new AllConsumer<THub>(manager));
        var connectionFactory = Factory<ConnectionConsumer<THub>>(manager => new ConnectionConsumer<THub>(manager));
        var groupFactory = Factory<GroupConsumer<THub>>(manager => new GroupConsumer<THub>(manager));
        var groupManagementFactory = Factory<GroupManagementConsumer<THub>>(
            manager => new GroupManagementConsumer<THub>(manager));
        var userFactory = Factory<UserConsumer<THub>>(manager => new UserConsumer<THub>(manager));

        _factories = [allFactory, connectionFactory, groupFactory, groupManagementFactory, userFactory];
        All = new ConsumerTestHarness<AllConsumer<THub>>(harness, allFactory, $"{queuePrefix}-all");
        Connection = new ConsumerTestHarness<ConnectionConsumer<THub>>(
            harness,
            connectionFactory,
            $"{queuePrefix}-connection");
        Group = new ConsumerTestHarness<GroupConsumer<THub>>(harness, groupFactory, $"{queuePrefix}-group");
        GroupManagement = new ConsumerTestHarness<GroupManagementConsumer<THub>>(
            harness,
            groupManagementFactory,
            $"{queuePrefix}-group-management");
        User = new ConsumerTestHarness<UserConsumer<THub>>(harness, userFactory, $"{queuePrefix}-user");
    }

    public ConsumerTestHarness<AllConsumer<THub>> All { get; }

    public ConsumerTestHarness<ConnectionConsumer<THub>> Connection { get; }

    public ConsumerTestHarness<GroupConsumer<THub>> Group { get; }

    public ConsumerTestHarness<GroupManagementConsumer<THub>> GroupManagement { get; }

    public ConsumerTestHarness<UserConsumer<THub>> User { get; }

    public ViciOneServiceBusHubLifetimeManager<THub> Manager { get; private set; } = null!;

    public void Attach(ViciOneServiceBusHubLifetimeManager<THub> manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        foreach (var factory in _factories)
        {
            factory.Manager = manager;
        }

        Manager = manager;
    }

    private static HubLifetimeManagerConsumerFactory<TConsumer, THub> Factory<TConsumer>(
        Func<ViciOneServiceBusHubLifetimeManager<THub>, TConsumer> factory)
        where TConsumer : class, IConsumer =>
        new(factory);
}

internal interface IHubLifetimeManagerConsumerFactory<THub>
    where THub : Hub
{
    ViciOneServiceBusHubLifetimeManager<THub> Manager { set; }
}

internal sealed class HubLifetimeManagerConsumerFactory<TConsumer, THub>(
    Func<ViciOneServiceBusHubLifetimeManager<THub>, TConsumer> factory)
    : IConsumerFactory<TConsumer>, IHubLifetimeManagerConsumerFactory<THub>
    where TConsumer : class, IConsumer
    where THub : Hub
{
    public ViciOneServiceBusHubLifetimeManager<THub> Manager { private get; set; } = null!;

    public async Task Send<TMessage>(
        ConsumeContext<TMessage> context,
        IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        var consumer = factory(Manager)
            ?? throw new ConsumerException($"Unable to create consumer '{TypeCache<TConsumer>.ShortName}'.");

        try
        {
            await next.Send(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, consumer))
                .ConfigureAwait(false);
        }
        finally
        {
            (consumer as IDisposable)?.Dispose();
        }
    }

    public void Probe(ProbeContext context) =>
        context.CreateConsumerFactoryScope<TConsumer>("signalRHubLifetimeManagerFactory");
}

internal sealed class BusHubLifetimeScopeProvider(IBus bus) : IHubLifetimeScopeProvider
{
    private readonly IClientFactory _clientFactory = bus.CreateClientFactory();

    public IHubLifetimeScope<THub> CreateScope<THub>()
        where THub : Hub =>
        new HubLifetimeScope<THub>(bus, _clientFactory);

    private sealed class HubLifetimeScope<THub>(
        IPublishEndpoint publishEndpoint,
        IClientFactory clientFactory) : IHubLifetimeScope<THub>
        where THub : Hub
    {
        public IPublishEndpoint PublishEndpoint { get; } = publishEndpoint;

        public IRequestClient<GroupManagement<THub>> RequestClient { get; } =
            clientFactory.CreateRequestClient<GroupManagement<THub>>();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal sealed class TestHubProtocolResolver : IHubProtocolResolver
{
    private readonly IReadOnlyDictionary<string, IHubProtocol> _protocols;

    public TestHubProtocolResolver(IEnumerable<IHubProtocol> protocols)
    {
        ArgumentNullException.ThrowIfNull(protocols);

        _protocols = protocols.ToDictionary(protocol => protocol.Name, StringComparer.OrdinalIgnoreCase);
        AllProtocols = _protocols.Values.ToArray();

        if (AllProtocols.Count == 0)
        {
            throw new ArgumentException("At least one SignalR protocol is required.", nameof(protocols));
        }
    }

    public IReadOnlyList<IHubProtocol> AllProtocols { get; }

    public IHubProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols)
    {
        ArgumentNullException.ThrowIfNull(protocolName);

        return _protocols.TryGetValue(protocolName, out var protocol) &&
               (supportedProtocols is null || supportedProtocols.Contains(protocolName, StringComparer.OrdinalIgnoreCase))
            ? protocol
            : null;
    }
}
