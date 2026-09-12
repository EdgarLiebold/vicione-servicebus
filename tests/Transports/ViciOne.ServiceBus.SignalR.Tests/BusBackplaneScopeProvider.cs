using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class BusBackplaneScopeProvider : IBackplaneScopeProvider, IAsyncDisposable
{
    private readonly IBus _bus;
    private readonly IClientFactory _clientFactory;

    public BusBackplaneScopeProvider(IBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _clientFactory = bus.CreateClientFactory();
    }

    public ValueTask<IBackplaneScope<THub>> CreateScopeAsync<THub>()
        where THub : Hub =>
        ValueTask.FromResult<IBackplaneScope<THub>>(new BackplaneScope<THub>(_bus, _clientFactory));

    public ValueTask DisposeAsync() => _clientFactory.DisposeAsync();

    private sealed class BackplaneScope<THub>(
        IPublishEndpoint publishEndpoint,
        IClientFactory clientFactory) : IBackplaneScope<THub>
        where THub : Hub
    {
        public IPublishEndpoint PublishEndpoint { get; } = publishEndpoint;

        public IRequestClient<GroupCommand<THub>> GroupCommandClient { get; } =
            clientFactory.CreateRequestClient<GroupCommand<THub>>();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
