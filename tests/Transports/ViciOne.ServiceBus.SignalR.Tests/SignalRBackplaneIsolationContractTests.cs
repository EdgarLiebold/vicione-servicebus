using System.Collections.Concurrent;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class SignalRBackplaneIsolationContractTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "normal-public-composition-retains-selected-bus-owner")]
    public async Task PublicComposition_PublishesEachHubThroughItsSelectedBusAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string unique = NewId.NextGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IHubProtocolResolver>(new TestHubProtocolResolver([new JsonHubProtocol()]));
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.AddSignalRBackplane<PrimaryHub>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/primary-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        services.AddViciOneServiceBus<ISecondaryBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.AddSignalRBackplane<SecondaryHub>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/secondary-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IBusControl primary = provider.GetRequiredService<IBusControl>();
        IBusControl secondary = Assert.IsAssignableFrom<IBusControl>(provider.GetRequiredService<ISecondaryBus>());
        HubLifetimeManager<PrimaryHub> primaryManager = provider.GetRequiredService<HubLifetimeManager<PrimaryHub>>();
        HubLifetimeManager<SecondaryHub> secondaryManager = provider.GetRequiredService<HubLifetimeManager<SecondaryHub>>();
        var primaryObserver = new CompletedPublishObserver();
        var secondaryObserver = new CompletedPublishObserver();
        using ConnectHandle primaryHandle = primary.ConnectPublishObserver(primaryObserver);
        using ConnectHandle secondaryHandle = secondary.ConnectPublishObserver(secondaryObserver);
        await using var primaryClient = new HubConnectionTestClient();
        await using var secondaryClient = new HubConnectionTestClient();
        try
        {
            await primary.StartAsync(token).WaitAsync(Timeout, token);
            await secondary.StartAsync(token).WaitAsync(Timeout, token);
            await primaryManager.OnConnectedAsync(primaryClient.HubConnection);
            await secondaryManager.OnConnectedAsync(secondaryClient.HubConnection);

            await primaryManager.SendAllAsync("Owner", ["primary"], token).WaitAsync(Timeout, token);
            InvocationMessage primaryMessage = await primaryClient.ReadInvocationAsync(Timeout, token);
            Assert.Equal("Owner", primaryMessage.Target);
            Assert.Equal("primary", Assert.Single(primaryMessage.Arguments)?.ToString());
            Assert.Equal(new[] { typeof(BroadcastMessage<PrimaryHub>) }, primaryObserver.Broadcasts.ToArray());
            Assert.Empty(secondaryObserver.Broadcasts);

            await secondaryManager.SendAllAsync("Owner", ["secondary"], token).WaitAsync(Timeout, token);
            Assert.Equal(new[] { typeof(BroadcastMessage<PrimaryHub>) }, primaryObserver.Broadcasts.ToArray());
            Assert.Equal(new[] { typeof(BroadcastMessage<SecondaryHub>) }, secondaryObserver.Broadcasts.ToArray());
            InvocationMessage secondaryMessage = await secondaryClient.ReadInvocationAsync(Timeout, token);
            Assert.Equal("Owner", secondaryMessage.Target);
            Assert.Equal("secondary", Assert.Single(secondaryMessage.Arguments)?.ToString());
        }
        finally
        {
            await Task.WhenAll(
                primary.StopAsync(CancellationToken.None),
                secondary.StopAsync(CancellationToken.None)).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "captured-add-cannot-restore-disconnected-membership")]
    public async Task CapturedGroupAdd_CannotRestoreMembershipAfterDisconnectAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using var environment = await HubLifetimeManagerTestEnvironment<TestHub>.StartAsync(1);
        var endpoint = environment.Endpoints[0];
        await using var delayedClient = new HubConnectionTestClient();
        await using var liveClient = new HubConnectionTestClient();
        using var connection = new GatedFeatureConnection(delayedClient.Connection, token)
        {
            Protocol = delayedClient.HubConnection.Protocol,
        };
        await endpoint.Manager.OnConnectedAsync(connection);
        await endpoint.Manager.OnConnectedAsync(liveClient.HubConnection);
        await endpoint.Manager.AddToGroupAsync(liveClient.HubConnection.ConnectionId, "race-group", token);
        connection.Arm();
        Task? delayedAdd = null;
        try
        {
            delayedAdd = Task.Run(() => endpoint.Manager.AddToGroupAsync(
                connection.ConnectionId, "race-group", token), token);
            await connection.Entered.WaitAsync(Timeout, token);
            await endpoint.Manager.OnDisconnectedAsync(connection).WaitAsync(Timeout, token);
            Assert.Null(endpoint.Manager.Connections[connection.ConnectionId]);
            Assert.Same(liveClient.HubConnection, Assert.Single(endpoint.Manager.Groups.GetConnections("race-group")));

            connection.Release();
            await delayedAdd.WaitAsync(Timeout, token);
            Assert.Same(liveClient.HubConnection, Assert.Single(endpoint.Manager.Groups.GetConnections("race-group")));
            Assert.Equal(1, endpoint.Manager.Groups.Count);
            Assert.Empty(connection.Features.Get<Runtime.ConnectionGroupFeature>()!.Snapshot());

            await endpoint.Manager.SendGroupAsync("race-group", "Live", ["still-member"], token);
            InvocationMessage message = await liveClient.ReadInvocationAsync(Timeout, token);
            Assert.Equal("Live", message.Target);
            Assert.Equal("still-member", Assert.Single(message.Arguments)?.ToString());
        }
        finally
        {
            connection.Release();
            if (delayedAdd is not null)
                await delayedAdd.WaitAsync(Timeout, CancellationToken.None);
        }
    }

    public interface ISecondaryBus : IBus;
    public sealed class PrimaryHub : Hub;
    public sealed class SecondaryHub : Hub;

    private sealed class CompletedPublishObserver : IPublishObserver
    {
        public ConcurrentQueue<Type> Broadcasts { get; } = new();
        public Task PrePublishAsync<T>(PublishContext<T> context) where T : class => Task.CompletedTask;
        public Task PostPublishAsync<T>(PublishContext<T> context) where T : class
        {
            if (typeof(T) == typeof(BroadcastMessage<PrimaryHub>) || typeof(T) == typeof(BroadcastMessage<SecondaryHub>))
                Broadcasts.Enqueue(typeof(T));
            return Task.CompletedTask;
        }
        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    private sealed class GatedFeatureConnection(ConnectionContext context, CancellationToken cancellationToken)
        : HubConnectionContext(context, new HubConnectionContextOptions(), NullLoggerFactory.Instance), IDisposable
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _release = new(false);
        private int _armed;
        public Task Entered => _entered.Task;
        public void Arm() => Interlocked.Exchange(ref _armed, 1);
        public void Release() => _release.Set();
        public override IFeatureCollection Features
        {
            get
            {
                if (Interlocked.Exchange(ref _armed, 0) == 1)
                {
                    _entered.TrySetResult();
                    _release.Wait(cancellationToken);
                }
                return base.Features;
            }
        }
        public void Dispose() => _release.Dispose();
    }
}
