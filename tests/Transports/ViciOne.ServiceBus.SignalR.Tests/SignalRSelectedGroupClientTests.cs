using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class SignalRSelectedGroupClientTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "selected-group-client-receives-acknowledgement-from-owning-bus")]
    public async Task GroupClients_ApplyAndAcknowledgeCommandsThroughEachSelectedBusAsync()
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
            registration.AddSignalRBackplane<PrimaryGroupHub>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/group-primary-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        services.AddViciOneServiceBus<IGroupBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.AddSignalRBackplane<SecondaryGroupHub>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/group-secondary-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IBusControl primary = provider.GetRequiredService<IBusControl>();
        IBusControl secondary = Assert.IsAssignableFrom<IBusControl>(provider.GetRequiredService<IGroupBus>());
        var primaryManager = provider.GetRequiredService<ServiceBusHubLifetimeManager<PrimaryGroupHub>>();
        var secondaryManager = provider.GetRequiredService<ServiceBusHubLifetimeManager<SecondaryGroupHub>>();
        var scopeProvider = provider.GetRequiredService<IBackplaneScopeProvider>();
        var primaryObserver = new GroupPublishObserver();
        var secondaryObserver = new GroupPublishObserver();
        using ConnectHandle primaryHandle = primary.ConnectPublishObserver(primaryObserver);
        using ConnectHandle secondaryHandle = secondary.ConnectPublishObserver(secondaryObserver);
        await using var primaryClient = new HubConnectionTestClient();
        await using var secondaryClient = new HubConnectionTestClient();
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        operationCancellation.CancelAfter(Timeout);
        try
        {
            await primary.StartAsync(token).WaitAsync(Timeout, token);
            await secondary.StartAsync(token).WaitAsync(Timeout, token);
            await primaryManager.OnConnectedAsync(primaryClient.HubConnection);
            await secondaryManager.OnConnectedAsync(secondaryClient.HubConnection);

            await using IBackplaneScope<PrimaryGroupHub> primaryScope = await scopeProvider.CreateScopeAsync<PrimaryGroupHub>();
            Response<GroupCommandAcknowledgement<PrimaryGroupHub>> first =
                await SendAndReceiveAsync(primaryScope.GroupCommandClient,
                    new GroupCommand<PrimaryGroupHub>(GroupCommandAction.Add, "primary-members", primaryClient.HubConnection.ConnectionId),
                    operationCancellation.Token);
            Assert.Equal(primaryManager.NodeId, first.Message.NodeId);
            Assert.Same(primaryClient.HubConnection, Assert.Single(primaryManager.Groups.GetConnections("primary-members")));
            Assert.Equal(new[] { typeof(GroupCommand<PrimaryGroupHub>) }, primaryObserver.Commands.ToArray());
            Assert.Empty(secondaryObserver.Commands);

            await using IBackplaneScope<SecondaryGroupHub> secondaryScope = await scopeProvider.CreateScopeAsync<SecondaryGroupHub>();
            Response<GroupCommandAcknowledgement<SecondaryGroupHub>> second =
                await SendAndReceiveAsync(secondaryScope.GroupCommandClient,
                    new GroupCommand<SecondaryGroupHub>(GroupCommandAction.Add, "secondary-members", secondaryClient.HubConnection.ConnectionId),
                    operationCancellation.Token);
            Assert.Equal(secondaryManager.NodeId, second.Message.NodeId);
            Assert.Same(secondaryClient.HubConnection, Assert.Single(secondaryManager.Groups.GetConnections("secondary-members")));
            Assert.Equal(new[] { typeof(GroupCommand<PrimaryGroupHub>) }, primaryObserver.Commands.ToArray());
            Assert.Equal(new[] { typeof(GroupCommand<SecondaryGroupHub>) }, secondaryObserver.Commands.ToArray());
            Assert.Empty(primaryManager.Groups.GetConnections("secondary-members"));
            Assert.Empty(secondaryManager.Groups.GetConnections("primary-members"));
        }
        finally
        {
            operationCancellation.Cancel();
            await Task.WhenAll(primary.StopAsync(CancellationToken.None), secondary.StopAsync(CancellationToken.None))
                .WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static async Task<Response<GroupCommandAcknowledgement<THub>>> SendAndReceiveAsync<THub>(
        IRequestClient<GroupCommand<THub>> client, GroupCommand<THub> command, CancellationToken cancellationToken)
        where THub : Hub
    {
        using RequestHandle<GroupCommand<THub>> request = client.Create(command, cancellationToken: cancellationToken);
        Task<Response<GroupCommandAcknowledgement<THub>>> response =
            request.GetResponseAsync<GroupCommandAcknowledgement<THub>>(cancellationToken: cancellationToken);
        await Task.WhenAll(request.Message, response);
        return await response;
    }

    public interface IGroupBus : IBus;
    public sealed class PrimaryGroupHub : Hub;
    public sealed class SecondaryGroupHub : Hub;

    private sealed class GroupPublishObserver : IPublishObserver
    {
        public ConcurrentQueue<Type> Commands { get; } = new();
        public Task PrePublishAsync<T>(PublishContext<T> context) where T : class => Task.CompletedTask;
        public Task PostPublishAsync<T>(PublishContext<T> context) where T : class
        {
            if (typeof(T) == typeof(GroupCommand<PrimaryGroupHub>) || typeof(T) == typeof(GroupCommand<SecondaryGroupHub>))
                Commands.Enqueue(typeof(T));
            return Task.CompletedTask;
        }
        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }
}
