using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.SignalR.Consumers;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class SignalRBackplaneEndpoint<THub>
    where THub : Hub
{
    private readonly IReadOnlyList<IHubLifetimeManagerConsumerFactory<THub>> _factories;

    public SignalRBackplaneEndpoint(BusTestHarness harness, string queuePrefix)
    {
        var broadcastFactory = Factory<BroadcastConsumer<THub>>(manager => new BroadcastConsumer<THub>(manager));
        var connectionFactory = Factory<ConnectionConsumer<THub>>(manager => new ConnectionConsumer<THub>(manager));
        var groupFactory = Factory<GroupConsumer<THub>>(manager => new GroupConsumer<THub>(manager));
        var groupCommandFactory = Factory<GroupCommandConsumer<THub>>(manager => new GroupCommandConsumer<THub>(manager));
        var userFactory = Factory<UserConsumer<THub>>(manager => new UserConsumer<THub>(manager));
        var clientResultFactory = Factory<ClientResultConsumer<THub>>(manager => new ClientResultConsumer<THub>(manager));
        var cancellationFactory = Factory<InvocationCancellationConsumer<THub>>(
            manager => new InvocationCancellationConsumer<THub>(manager));

        _factories =
        [
            broadcastFactory,
            connectionFactory,
            groupFactory,
            groupCommandFactory,
            userFactory,
            clientResultFactory,
            cancellationFactory,
        ];
        Broadcast = new ConsumerTestHarness<BroadcastConsumer<THub>>(
            harness,
            broadcastFactory,
            $"{queuePrefix}-broadcast");
        Connection = new ConsumerTestHarness<ConnectionConsumer<THub>>(
            harness,
            connectionFactory,
            $"{queuePrefix}-connection");
        Group = new ConsumerTestHarness<GroupConsumer<THub>>(harness, groupFactory, $"{queuePrefix}-group");
        GroupCommand = new ConsumerTestHarness<GroupCommandConsumer<THub>>(
            harness,
            groupCommandFactory,
            $"{queuePrefix}-group-command");
        User = new ConsumerTestHarness<UserConsumer<THub>>(harness, userFactory, $"{queuePrefix}-user");
        ClientResult = new ConsumerTestHarness<ClientResultConsumer<THub>>(
            harness,
            clientResultFactory,
            $"{queuePrefix}-client-result");
        InvocationCancellation = new ConsumerTestHarness<InvocationCancellationConsumer<THub>>(
            harness,
            cancellationFactory,
            $"{queuePrefix}-invocation-cancellation");
    }

    public ConsumerTestHarness<BroadcastConsumer<THub>> Broadcast { get; }

    public ConsumerTestHarness<ConnectionConsumer<THub>> Connection { get; }

    public ConsumerTestHarness<GroupConsumer<THub>> Group { get; }

    public ConsumerTestHarness<GroupCommandConsumer<THub>> GroupCommand { get; }

    public ConsumerTestHarness<UserConsumer<THub>> User { get; }

    public ConsumerTestHarness<ClientResultConsumer<THub>> ClientResult { get; }

    public ConsumerTestHarness<InvocationCancellationConsumer<THub>> InvocationCancellation { get; }

    public ServiceBusHubLifetimeManager<THub> Manager { get; private set; } = null!;

    public void Attach(ServiceBusHubLifetimeManager<THub> manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        foreach (IHubLifetimeManagerConsumerFactory<THub> factory in _factories)
            factory.Manager = manager;

        Manager = manager;
    }

    private static HubLifetimeManagerConsumerFactory<TConsumer, THub> Factory<TConsumer>(
        Func<ServiceBusHubLifetimeManager<THub>, TConsumer> factory)
        where TConsumer : class, IConsumer =>
        new(factory);
}
