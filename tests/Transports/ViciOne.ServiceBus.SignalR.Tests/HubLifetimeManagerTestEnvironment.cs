using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.SignalR.Configuration;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class HubLifetimeManagerTestEnvironment<THub> : IAsyncDisposable
    where THub : Hub
{
    private readonly InMemoryTestHarness _harness;
    private readonly RecordingLogger<ServiceBusHubLifetimeManager<THub>> _logger;
    private readonly ILogContext? _previousLogContext;
    private readonly BusBackplaneScopeProvider _scopeProvider;

    private HubLifetimeManagerTestEnvironment(
        InMemoryTestHarness harness,
        IReadOnlyList<SignalRBackplaneEndpoint<THub>> endpoints,
        RecordingLogger<ServiceBusHubLifetimeManager<THub>> logger,
        ILogContext? previousLogContext,
        BusBackplaneScopeProvider scopeProvider)
    {
        _harness = harness;
        Endpoints = endpoints;
        _logger = logger;
        _previousLogContext = previousLogContext;
        _scopeProvider = scopeProvider;
    }

    public IReadOnlyList<SignalRBackplaneEndpoint<THub>> Endpoints { get; }

    public TimeSpan Timeout => _harness.TestTimeout;

    public static async Task<HubLifetimeManagerTestEnvironment<THub>> StartAsync(
        int endpointCount,
        Func<int, IReadOnlyList<IHubProtocol>>? protocolFactory = null)
    {
        if (endpointCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(endpointCount));

        ILogContext? previousLogContext = LogContext.Current;
        var logger = new RecordingLogger<ServiceBusHubLifetimeManager<THub>>();
        LogContext.ConfigureCurrentLogContext(logger);

        var harness = new InMemoryTestHarness($"signalr-{NewId.NextGuid():N}");
        var endpoints = Enumerable.Range(0, endpointCount)
            .Select(index => new SignalRBackplaneEndpoint<THub>(harness, $"signalr-endpoint-{index}"))
            .ToArray();
        BusBackplaneScopeProvider? scopeProvider = null;

        try
        {
            await harness.StartAsync().ConfigureAwait(false);
            scopeProvider = new BusBackplaneScopeProvider(harness.Bus);
            var environment = new HubLifetimeManagerTestEnvironment<THub>(
                harness,
                endpoints,
                logger,
                previousLogContext,
                scopeProvider);

            for (var index = 0; index < endpoints.Length; index++)
                endpoints[index].Attach(environment.CreateManager(protocolFactory?.Invoke(index)));

            return environment;
        }
        catch
        {
            if (scopeProvider is not null)
                await scopeProvider.DisposeAsync().ConfigureAwait(false);
            await harness.StopAsync().ConfigureAwait(false);
            harness.Dispose();
            LogContext.Current = previousLogContext!;
            throw;
        }
    }

    public Task<ConsumeContext<GroupCommandAcknowledgement<THub>>> ObserveNextAcknowledgementAsync() =>
        _harness.WaitForMessageAsync<GroupCommandAcknowledgement<THub>>();

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return _harness.Bus.PublishAsync(message, cancellationToken);
    }

    public async Task<Response<GroupCommandAcknowledgement<THub>>> RequestGroupCommandAsync(
        GroupCommand<THub> command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using IBackplaneScope<THub> scope =
            await _scopeProvider.CreateScopeAsync<THub>().ConfigureAwait(false);
        using RequestHandle<GroupCommand<THub>> request = scope.GroupCommandClient.Create(
            command,
            cancellationToken: cancellationToken);
        return await request.GetResponseAsync<GroupCommandAcknowledgement<THub>>(
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<LogEntry> ObserveLogAsync(
        Func<LogEntry, bool> predicate,
        CancellationToken cancellationToken = default) =>
        _logger.WaitForAsync(predicate, Timeout, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _scopeProvider.DisposeAsync().ConfigureAwait(false);
            await _harness.StopAsync().ConfigureAwait(false);
            _harness.Dispose();
        }
        finally
        {
            LogContext.Current = _previousLogContext!;
        }
    }

    private ServiceBusHubLifetimeManager<THub> CreateManager(IReadOnlyList<IHubProtocol>? protocols)
    {
        IReadOnlyList<IHubProtocol> availableProtocols = protocols ?? [new JsonHubProtocol()];

        return new ServiceBusHubLifetimeManager<THub>(
            new SignalRBackplaneSettings<THub>(new RequestTimeout(Timeout)),
            _scopeProvider,
            new TestHubProtocolResolver(availableProtocols),
            _logger);
    }
}
