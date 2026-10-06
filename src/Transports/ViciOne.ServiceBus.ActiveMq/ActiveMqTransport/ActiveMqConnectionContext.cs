using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Owns an Apache NMS connection, its session executor, and cached temporary destinations.</summary>
public class ActiveMqConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly IConnection _connection;
    readonly TaskExecutor _executor;
    readonly ConcurrentDictionary<(string Name, DestinationType Type), IDestination> _temporaryEntities;

    /// <summary>
    /// Matches consumer destinations that follow the configured ActiveMQ virtual-topic naming pattern.
    /// Such destinations must retain their configured names and cannot be replaced with broker-generated names.
    /// </summary>
    /// <seealso href="https://activemq.apache.org/virtual-destinations">Virtual Destinations</seealso>
    readonly Regex _virtualTopicConsumerPattern;

    /// <summary>Creates a connection context for an established Apache NMS connection.</summary>
    /// <param name="connection">The established Apache NMS connection owned by the context.</param>
    /// <param name="hostConfiguration">The ActiveMQ host and topology configuration.</param>
    /// <param name="cancellationToken">The token that signals connection-context shutdown.</param>
    public ActiveMqConnectionContext(IConnection connection, IActiveMqHostConfiguration hostConfiguration, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _connection = connection;

        Description = hostConfiguration.Settings.ToDescription();
        HostAddress = hostConfiguration.HostAddress;

        Topology = hostConfiguration.Topology;

        _executor = new TaskExecutor();
        _temporaryEntities = new ConcurrentDictionary<(string Name, DestinationType Type), IDestination>();

        _virtualTopicConsumerPattern = new Regex(hostConfiguration.Topology.PublishTopology.VirtualTopicConsumerPattern, RegexOptions.Compiled);
    }

    /// <summary>Gets the underlying Apache NMS connection.</summary>
    public IConnection Connection => _connection;
    /// <summary>Gets the broker description used for diagnostics.</summary>
    public string Description { get; }
    /// <summary>Gets the configured broker address.</summary>
    public Uri HostAddress { get; }
    /// <summary>Gets the ActiveMQ bus topology.</summary>
    public IActiveMqBusTopology Topology { get; }

    /// <summary>Creates an Apache NMS session that uses individual acknowledgement.</summary>
    /// <param name="cancellationToken">The token used to cancel session creation.</param>
    /// <returns>A task that produces the newly created session.</returns>
    public async Task<ISession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _executor.ExecuteAsync(() => _connection.CreateSessionAsync(AcknowledgementMode.IndividualAcknowledge), tokenSource.Token)
            .ConfigureAwait(false);
    }

    /// <summary>Determines whether a destination name matches the configured virtual-topic consumer pattern.</summary>
    /// <param name="name">The destination name to test.</param>
    /// <returns><see langword="true" /> when the name identifies a virtual-topic consumer; otherwise, <see langword="false" />.</returns>
    public bool IsVirtualTopicConsumer(string name)
    {
        return _virtualTopicConsumerPattern.IsMatch(name);
    }

    /// <summary>Gets or creates the cached temporary queue for a destination name.</summary>
    /// <param name="session">The session used to resolve the broker destination.</param>
    /// <param name="topicName">The destination name used as the cache key.</param>
    /// <returns>The cached or newly resolved temporary queue.</returns>
    public IQueue GetTemporaryQueue(ISession session, string topicName)
    {
        // Different sessions share this cache. GetOrAdd alone may invoke a native
        // creation factory twice and leave the losing destination unowned.
        lock (_temporaryEntities)
            return (IQueue)_temporaryEntities.GetOrAdd((topicName, DestinationType.TemporaryQueue),
                _ => (IQueue)SessionUtil.GetDestination(session, topicName, DestinationType.TemporaryQueue));
    }

    /// <summary>Gets or creates the cached temporary topic for a destination name.</summary>
    /// <param name="session">The session used to resolve the broker destination.</param>
    /// <param name="topicName">The destination name used as the cache key.</param>
    /// <returns>The cached or newly resolved temporary topic.</returns>
    public ITopic GetTemporaryTopic(ISession session, string topicName)
    {
        lock (_temporaryEntities)
            return (ITopic)_temporaryEntities.GetOrAdd((topicName, DestinationType.TemporaryTopic),
                _ => (ITopic)SessionUtil.GetDestination(session, topicName, DestinationType.TemporaryTopic));
    }

    /// <summary>Tries to retrieve a cached temporary destination by name and destination type.</summary>
    /// <param name="name">The destination name.</param>
    /// <param name="destinationType">The queue or topic destination type.</param>
    /// <param name="destination">The cached destination, when found.</param>
    /// <returns><see langword="true" /> when the destination is cached; otherwise, <see langword="false" />.</returns>
    public bool TryGetTemporaryEntity(string name, DestinationType destinationType, out IDestination? destination)
    {
        return _temporaryEntities.TryGetValue((name, TemporaryType(destinationType)), out destination);
    }

    /// <summary>Tries to remove a cached temporary destination and delete it from the broker.</summary>
    /// <param name="session">The session used to delete the broker destination.</param>
    /// <param name="name">The cached destination name.</param>
    /// <param name="destinationType">The queue or topic destination type to remove.</param>
    /// <returns><see langword="true" /> when a cached destination was deleted; otherwise, <see langword="false" />.</returns>
    public bool TryRemoveTemporaryEntity(ISession session, string name, DestinationType destinationType)
    {
        var key = (name, TemporaryType(destinationType));
        IDestination? destination;
        lock (_temporaryEntities)
        {
            if (!_temporaryEntities.TryRemove(key, out destination))
                return false;
        }

        try
        {
            session.DeleteDestination(destination);
            return true;
        }
        catch
        {
            // A failed broker delete must remain discoverable for a later retry. Do not
            // overwrite a newer mapping if another operation recreated the same name.
            lock (_temporaryEntities)
                _temporaryEntities.TryAdd(key, destination);
            throw;
        }
    }

    static DestinationType TemporaryType(DestinationType destinationType) => destinationType switch
    {
        DestinationType.Queue or DestinationType.TemporaryQueue => DestinationType.TemporaryQueue,
        DestinationType.Topic or DestinationType.TemporaryTopic => DestinationType.TemporaryTopic,
        _ => throw new ArgumentOutOfRangeException(nameof(destinationType), destinationType, "A queue or topic destination type is required.")
    };

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that completes after connection and executor cleanup.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            TransportLogMessages.DisconnectHost(Description);
        }
        catch (Exception)
        {
            // Optional diagnostics must not prevent owned cleanup or replace its result.
        }
        var failures = new ActiveMqCleanupFailures();

        await failures.CaptureAsync(
                () => _connection.CloseAsync(),
                exception => LogWarning(exception, "Close Connection Faulted: {Host}", Description))
            .ConfigureAwait(false);
        failures.Capture(
            () => _connection.Dispose(),
            exception => LogWarning(exception, "Dispose Connection Faulted: {Host}", Description));
        await failures.CaptureAsync(
                () => _executor.DisposeAsync(),
                exception => LogWarning(exception, "Dispose Connection Executor Faulted: {Host}", Description))
            .ConfigureAwait(false);

        try
        {
            TransportLogMessages.DisconnectedHost(Description);
        }
        catch (Exception)
        {
            // Optional diagnostics must not prevent owned cleanup or replace its result.
        }
        failures.ThrowIfAny("One or more ActiveMQ connection cleanup stages failed.");
    }

    static void LogWarning(Exception exception, string message, string description)
    {
        try
        {
            LogContext.Warning?.Log(exception, message, description);
        }
        catch
        {
            // Cleanup failures remain the product result even if a diagnostic listener fails.
        }
    }
}


internal sealed class ActiveMqCleanupFailures
{
    readonly List<Exception> _failures = new List<Exception>();

    public async ValueTask CaptureAsync(Func<Task> stage, Action<Exception> onFailure)
    {
        try
        {
            await stage().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Add(exception);
            onFailure(exception);
        }
    }

    public async ValueTask CaptureAsync(Func<ValueTask> stage, Action<Exception> onFailure)
    {
        try
        {
            await stage().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Add(exception);
            onFailure(exception);
        }
    }

    public void Capture(Action stage, Action<Exception> onFailure)
    {
        try
        {
            stage();
        }
        catch (Exception exception)
        {
            Add(exception);
            onFailure(exception);
        }
    }

    public void ThrowIfAny(string message)
    {
        if (_failures.Count == 0)
            return;

        if (_failures.Count == 1)
            ExceptionDispatchInfo.Capture(_failures[0]).Throw();

        throw new AggregateException(message, _failures);
    }

    void Add(Exception exception)
    {
        // One failed cleanup stage must preserve the exact exception object thrown by that
        // stage, including an AggregateException. Multiple stage failures are aggregated by
        // this owner in their stable execution order without rewriting their identities.
        _failures.Add(exception);
    }
}
