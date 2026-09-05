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

/// <summary>
/// Provides a vici one service bus hub lifetime manager implementation.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public class ViciOneServiceBusHubLifetimeManager<THub> :
    HubLifetimeManager<THub>
    where THub : Hub
{
    readonly HubLifetimeManagerOptions<THub> _options;
    readonly IHubProtocolResolver _resolver;
    readonly IHubLifetimeScopeProvider _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="scopeProvider">The scope provider value.</param>
    /// <param name="resolver">The resolver value.</param>
    public ViciOneServiceBusHubLifetimeManager(HubLifetimeManagerOptions<THub> options, IHubLifetimeScopeProvider scopeProvider, IHubProtocolResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
        _scopeProvider = scopeProvider;
        _resolver = resolver;
    }

    IReadOnlyList<IHubProtocol> Protocols => _resolver.AllProtocols;

    /// <summary>
    /// Gets the server name value.
    /// </summary>
    public string ServerName => _options.ServerName;
    /// <summary>
    /// Gets the connections value.
    /// </summary>
    public HubConnectionStore Connections => _options.ConnectionStore;
    /// <summary>
    /// Gets the groups value.
    /// </summary>
    public ViciOneServiceBusSubscriptionManager Groups => _options.GroupsSubscriptionManager;
    /// <summary>
    /// Gets the users value.
    /// </summary>
    public ViciOneServiceBusSubscriptionManager Users => _options.UsersSubscriptionManager;

    /// <summary>
    /// Performs the on connected operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <returns>The result of the operation.</returns>
    public override Task OnConnectedAsync(HubConnectionContext connection)
    {
        var feature = new ViciOneServiceBusFeature();
        connection.Features.Set<IViciOneServiceBusFeature>(feature);

        Connections.Add(connection);
        if (!string.IsNullOrEmpty(connection.UserIdentifier))
            Users.AddSubscription(connection.UserIdentifier, connection);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the on disconnected operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <returns>The result of the operation.</returns>
    public override Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        Connections.Remove(connection);
        if (!string.IsNullOrEmpty(connection.UserIdentifier))
            Users.RemoveSubscription(connection.UserIdentifier, connection);

        // Also unsubscribe from any groups
        ConcurrentHashSet<string>? groups = connection.Features.Get<IViciOneServiceBusFeature>()?.Groups;

        if (groups != null)
        {
            // Removes connection from all groups locally
            foreach (var groupName in groups.ToArray())
                RemoveGroupAsyncCore(connection, groupName);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends all.
    /// </summary>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        await using IHubLifetimeScope<THub> scope = _scopeProvider.CreateScope<THub>();
        LogContext.Info?.Log("Publishing All<THub> message to ViciOne.ServiceBus.");
        await scope.PublishEndpoint.PublishAsync<All<THub>>(
            new { Messages = Protocols.ToProtocolDictionary(methodName, args) }, cancellationToken);
    }

    /// <summary>
    /// Sends all except.
    /// </summary>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="excludedConnectionIds">The excluded connection ids value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Sends connection.
    /// </summary>
    /// <param name="connectionId">The connection id value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task SendConnectionAsync(string connectionId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        if (connectionId == null)
            throw new ArgumentNullException(nameof(connectionId));

        // If the connection is local we can skip sending the message through the bus since we require sticky connections.
        // This also saves serializing and deserializing the message!
        var connection = Connections[connectionId];
        if (connection != null)
        {
            // Connection is local, so we can skip publish
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

    /// <summary>
    /// Sends connections.
    /// </summary>
    /// <param name="connectionIds">The connection ids value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Sends group.
    /// </summary>
    /// <param name="groupName">The group name value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Sends group except.
    /// </summary>
    /// <param name="groupName">The group name value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="excludedConnectionIds">The excluded connection ids value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Sends groups.
    /// </summary>
    /// <param name="groupNames">The group names value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Sends user.
    /// </summary>
    /// <param name="userId">The user id value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Sends users.
    /// </summary>
    /// <param name="userIds">The user ids value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Adds to group to the configuration.
    /// </summary>
    /// <param name="connectionId">The connection id value.</param>
    /// <param name="groupName">The group name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (connectionId == null)
            throw new ArgumentNullException(nameof(connectionId));

        if (groupName == null)
            throw new ArgumentNullException(nameof(groupName));

        var connection = Connections[connectionId];
        if (connection != null)
        {
            // short circuit if connection is on this server
            AddGroupAsyncCore(connection, groupName);

            return;
        }

        // Publish to ViciOne.ServiceBus group management instead, but it waits for an ack...
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
                cancellationToken);

            Response<Ack<THub>> ack = await request.GetResponseAsync<Ack<THub>>(cancellationToken: cancellationToken);
            LogContext.Info?.Log($"Request Received for add GroupManagement<THub> from {ack.Message.ServerName}.");
        }
        catch (RequestTimeoutException e)
        {
            // That's okay, just log and swallow
            LogContext.Warning?.Log(e, "GroupManagement<THub> add ack timed out.", e);
        }
    }

    /// <summary>
    /// Performs the remove from group operation.
    /// </summary>
    /// <param name="connectionId">The connection id value.</param>
    /// <param name="groupName">The group name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (connectionId == null)
            throw new ArgumentNullException(nameof(connectionId));

        if (groupName == null)
            throw new ArgumentNullException(nameof(groupName));

        var connection = Connections[connectionId];
        if (connection != null)
        {
            // short circuit if connection is on this server
            RemoveGroupAsyncCore(connection, groupName);

            return;
        }

        // Publish to ViciOne.ServiceBus group management instead, but it waits for an ack...
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
                cancellationToken);

            Response<Ack<THub>> ack = await request.GetResponseAsync<Ack<THub>>(cancellationToken: cancellationToken);
            LogContext.Info?.Log($"Request Received for remove GroupManagement<THub> from {ack.Message.ServerName}.");
        }
        catch (RequestTimeoutException e)
        {
            // That's okay, just log and swallow
            LogContext.Warning?.Log(e, "GroupManagement<THub> remove ack timed out.", e);
        }
    }

    /// <summary>
    /// Adds group async core to the configuration.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="groupName">The group name value.</param>
    public void AddGroupAsyncCore(HubConnectionContext connection, string groupName)
    {
        var feature = connection.Features.Get<IViciOneServiceBusFeature>()
            ?? throw new InvalidOperationException("The ViciOne ServiceBus connection feature was not initialized.");
        feature.Groups.Add(groupName);

        Groups.AddSubscription(groupName, connection);
    }

    /// <summary>
    /// Performs the remove group async core operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="groupName">The group name value.</param>
    public void RemoveGroupAsyncCore(HubConnectionContext connection, string groupName)
    {
        Groups.RemoveSubscription(groupName, connection);

        var feature = connection.Features.Get<IViciOneServiceBusFeature>()
            ?? throw new InvalidOperationException("The ViciOne ServiceBus connection feature was not initialized.");
        feature.Groups.Remove(groupName);
    }
}
