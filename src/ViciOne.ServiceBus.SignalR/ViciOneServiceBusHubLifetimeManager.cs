using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Scoping;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>Manages vici one service bus hub lifetime.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class ViciOneServiceBusHubLifetimeManager<THub> :
    HubLifetimeManager<THub>
    where THub : Hub
{
    readonly HubLifetimeManagerOptions<THub> _options;
    readonly IHubProtocolResolver _resolver;
    readonly IHubLifetimeScopeProvider _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="scopeProvider">The scope provider.</param>
    /// <param name="resolver">The resolver.</param>
    public ViciOneServiceBusHubLifetimeManager(HubLifetimeManagerOptions<THub> options, IHubLifetimeScopeProvider scopeProvider, IHubProtocolResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
        _scopeProvider = scopeProvider;
        _resolver = resolver;
    }

    IReadOnlyList<IHubProtocol> Protocols => _resolver.AllProtocols;

    /// <summary>Gets the server name.</summary>
    public string ServerName => _options.ServerName;
    /// <summary>Gets the connections.</summary>
    public HubConnectionStore Connections => _options.ConnectionStore;
    /// <summary>Gets the groups.</summary>
    public ViciOneServiceBusSubscriptionManager Groups => _options.GroupsSubscriptionManager;
    /// <summary>Gets the users.</summary>
    public ViciOneServiceBusSubscriptionManager Users => _options.UsersSubscriptionManager;

    /// <summary>Handles the notification for connected.</summary>
    /// <param name="connection">The connection.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task OnConnectedAsync(HubConnectionContext connection)
    {
        var feature = new ViciOneServiceBusFeature();
        connection.Features.Set<IViciOneServiceBusFeature>(feature);

        Connections.Add(connection);
        if (!string.IsNullOrEmpty(connection.UserIdentifier))
            Users.AddSubscription(connection.UserIdentifier, connection);

        return Task.CompletedTask;
    }

    /// <summary>Handles the notification for disconnected.</summary>
    /// <param name="connection">The connection.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        Connections.Remove(connection);
        if (!string.IsNullOrEmpty(connection.UserIdentifier))
            Users.RemoveSubscription(connection.UserIdentifier, connection);

        // Disconnecting removes every local group subscription owned by this connection.
        ConcurrentHashSet<string>? groups = connection.Features.Get<IViciOneServiceBusFeature>()?.Groups;

        if (groups != null)
        {
            foreach (var groupName in groups.ToArray())
                RemoveGroupCore(connection, groupName);
        }

        return Task.CompletedTask;
    }

    /// <summary>Sends all.</summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        LogContext.Info?.Log("Publishing All<THub> message to ViciOne.ServiceBus.");
        await scope.PublishEndpoint.PublishAsync<All<THub>>(
            new { Messages = Protocols.ToProtocolDictionary(methodName, args) }, cancellationToken);
    }

    /// <summary>Sends all except.</summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="excludedConnectionIds">The excluded connection ids.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = default)
    {
        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        LogContext.Info?.Log("Publishing All<THub> message to ViciOne.ServiceBus, with exceptions.");
        await scope.PublishEndpoint.PublishAsync<All<THub>>(new
        {
            Messages = Protocols.ToProtocolDictionary(methodName, args),
            ExcludedConnectionIds = excludedConnectionIds.ToArray()
        }, cancellationToken);
    }

    /// <summary>Sends connection.</summary>
    /// <param name="connectionId">The connection id.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendConnectionAsync(string connectionId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        if (connectionId == null)
            throw new ArgumentNullException(nameof(connectionId));

        // Sticky connections permit direct delivery when this server owns the connection.
        var connection = Connections[connectionId];
        if (connection != null)
        {
            await connection.WriteAsync(new InvocationMessage(methodName, args), cancellationToken).AsTask();
            return;
        }

        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        LogContext.Info?.Log("Publishing Connection<THub> message to ViciOne.ServiceBus.");
        await scope.PublishEndpoint.PublishAsync<Connection<THub>>(new
        {
            ConnectionId = connectionId,
            Messages = Protocols.ToProtocolDictionary(methodName, args)
        },
            cancellationToken);
    }

    /// <summary>Sends connections.</summary>
    /// <param name="connectionIds">The connection ids.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (connectionIds == null)
            throw new ArgumentNullException(nameof(connectionIds));

        if (connectionIds.Any())
        {
            await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
            IReadOnlyDictionary<string, byte[]> protocolDictionary = Protocols.ToProtocolDictionary(methodName, args);
            IEnumerable<Task> publishTasks = connectionIds.Select(connectionId =>
                scope.PublishEndpoint.PublishAsync<Connection<THub>>(new
                {
                    ConnectionId = connectionId,
                    Messages = protocolDictionary
                }, cancellationToken));

            LogContext.Info?.Log("Publishing multiple Connection<THub> messages to ViciOne.ServiceBus.");
            await Task.WhenAll(publishTasks);
        }
    }

    /// <summary>Sends group.</summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendGroupAsync(string groupName, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        if (groupName == null)
            throw new ArgumentNullException(nameof(groupName));

        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        LogContext.Info?.Log("Publishing Group<THub> message to ViciOne.ServiceBus.");
        await scope.PublishEndpoint.PublishAsync<Group<THub>>(new
        {
            GroupName = groupName,
            Messages = Protocols.ToProtocolDictionary(methodName, args)
        },
            cancellationToken);
    }

    /// <summary>Sends group except.</summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="excludedConnectionIds">The excluded connection ids.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = default)
    {
        if (groupName == null)
            throw new ArgumentNullException(nameof(groupName));

        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        LogContext.Info?.Log("Publishing Group<THub> message to ViciOne.ServiceBus, with exceptions.");
        await scope.PublishEndpoint.PublishAsync<Group<THub>>(new
        {
            GroupName = groupName,
            Messages = Protocols.ToProtocolDictionary(methodName, args),
            ExcludedConnectionIds = excludedConnectionIds.ToArray()
        }, cancellationToken);
    }

    /// <summary>Sends groups.</summary>
    /// <param name="groupNames">The group names.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (groupNames == null)
            throw new ArgumentNullException(nameof(groupNames));

        if (groupNames.Any())
        {
            await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
            IReadOnlyDictionary<string, byte[]> protocolDictionary = Protocols.ToProtocolDictionary(methodName, args);
            IEnumerable<Task> publishTasks = groupNames.Where(x => !string.IsNullOrEmpty(x)).Select(groupName =>
                scope.PublishEndpoint.PublishAsync<Group<THub>>(new
                {
                    GroupName = groupName,
                    Messages = protocolDictionary
                }, cancellationToken));

            LogContext.Info?.Log("Publishing multiple Group<THub> messages to ViciOne.ServiceBus.");
            await Task.WhenAll(publishTasks);
        }
    }

    /// <summary>Sends user.</summary>
    /// <param name="userId">The user id.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendUserAsync(string userId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        LogContext.Info?.Log("Publishing User<THub> message to ViciOne.ServiceBus.");
        await scope.PublishEndpoint.PublishAsync<User<THub>>(new
        {
            UserId = userId,
            Messages = Protocols.ToProtocolDictionary(methodName, args)
        }, cancellationToken);
    }

    /// <summary>Sends users.</summary>
    /// <param name="userIds">The user ids.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (userIds == null)
            throw new ArgumentNullException(nameof(userIds));

        if (userIds.Any())
        {
            await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
            IReadOnlyDictionary<string, byte[]> protocolDictionary = Protocols.ToProtocolDictionary(methodName, args);
            IEnumerable<Task> publishTasks = userIds.Select(userId => scope.PublishEndpoint.PublishAsync<User<THub>>(new
            {
                UserId = userId,
                Messages = protocolDictionary
            }, cancellationToken));

            LogContext.Info?.Log("Publishing multiple User<THub> messages to ViciOne.ServiceBus.");
            await Task.WhenAll(publishTasks);
        }
    }

    /// <summary>Adds to group to the configuration.</summary>
    /// <param name="connectionId">The connection id.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (connectionId == null)
            throw new ArgumentNullException(nameof(connectionId));

        if (groupName == null)
            throw new ArgumentNullException(nameof(groupName));

        var connection = Connections[connectionId];
        if (connection != null)
        {
            // Local ownership makes the group update immediately authoritative.
            AddGroupCore(connection, groupName);

            return;
        }

        // Remote ownership requires an acknowledged group-management request.
        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        try
        {
            LogContext.Info?.Log("Publishing add GroupManagement<THub> message to ViciOne.ServiceBus.");
            RequestHandle<GroupManagement<THub>> request = scope.RequestClient.Create(new
            {
                ConnectionId = connectionId,
                GroupName = groupName,
                ServerName,
                Action = GroupAction.Add
            },
                cancellationToken: cancellationToken);

            Response<Ack<THub>> ack = await request.GetResponseAsync<Ack<THub>>(cancellationToken: cancellationToken);
            LogContext.Info?.Log($"Request Received for add GroupManagement<THub> from {ack.Message.ServerName}.");
        }
        catch (RequestTimeoutException e)
        {
            // A missing acknowledgement is non-fatal because connection ownership may have ended.
            LogContext.Warning?.Log(e, "GroupManagement<THub> add ack timed out.", e);
        }
    }

    /// <summary>Removes from group.</summary>
    /// <param name="connectionId">The connection id.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (connectionId == null)
            throw new ArgumentNullException(nameof(connectionId));

        if (groupName == null)
            throw new ArgumentNullException(nameof(groupName));

        var connection = Connections[connectionId];
        if (connection != null)
        {
            // Local ownership makes the group update immediately authoritative.
            RemoveGroupCore(connection, groupName);

            return;
        }

        // Remote ownership requires an acknowledged group-management request.
        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        try
        {
            LogContext.Info?.Log("Publishing remove GroupManagement<THub> message to ViciOne.ServiceBus.");
            RequestHandle<GroupManagement<THub>> request = scope.RequestClient.Create(new
            {
                ConnectionId = connectionId,
                GroupName = groupName,
                ServerName,
                Action = GroupAction.Remove
            },
                cancellationToken: cancellationToken);

            Response<Ack<THub>> ack = await request.GetResponseAsync<Ack<THub>>(cancellationToken: cancellationToken);
            LogContext.Info?.Log($"Request Received for remove GroupManagement<THub> from {ack.Message.ServerName}.");
        }
        catch (RequestTimeoutException e)
        {
            // A missing acknowledgement is non-fatal because connection ownership may have ended.
            LogContext.Warning?.Log(e, "GroupManagement<THub> remove ack timed out.", e);
        }
    }

    /// <summary>Adds the connection to the named group in local connection state.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="groupName">The group name.</param>
    public void AddGroupCore(HubConnectionContext connection, string groupName)
    {
        var feature = connection.Features.Get<IViciOneServiceBusFeature>()
            ?? throw new InvalidOperationException("The ViciOne ServiceBus connection feature was not initialized.");
        feature.Groups.Add(groupName);

        Groups.AddSubscription(groupName, connection);
    }

    /// <summary>Removes the connection from the named group in local connection state.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="groupName">The group name.</param>
    public void RemoveGroupCore(HubConnectionContext connection, string groupName)
    {
        Groups.RemoveSubscription(groupName, connection);

        var feature = connection.Features.Get<IViciOneServiceBusFeature>()
            ?? throw new InvalidOperationException("The ViciOne ServiceBus connection feature was not initialized.");
        feature.Groups.Remove(groupName);
    }
}
