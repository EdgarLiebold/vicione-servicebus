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

/// <summary>
/// Provides an active mq connection context implementation.
/// </summary>
public class ActiveMqConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly IConnection _connection;
    readonly TaskExecutor _executor;
    readonly ConcurrentDictionary<string, IDestination> _temporaryEntities;

    /// <summary>
    /// Regular expression to distinguish if a destination is not for consuming data from a VirtualTopic. If yes we must get a standard destination because the name of
    /// the destination must match specific
    /// pattern. A temporary destination has generated name.
    /// </summary>
    /// <seealso href="https://activemq.apache.org/virtual-destinations">Virtual Destinations</seealso>
    readonly Regex _virtualTopicConsumerPattern;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ActiveMqConnectionContext(IConnection connection, IActiveMqHostConfiguration hostConfiguration, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _connection = connection;

        Description = hostConfiguration.Settings.ToDescription();
        HostAddress = hostConfiguration.HostAddress;

        Topology = hostConfiguration.Topology;

        _executor = new TaskExecutor();
        _temporaryEntities = new ConcurrentDictionary<string, IDestination>();

        _virtualTopicConsumerPattern = new Regex(hostConfiguration.Topology.PublishTopology.VirtualTopicConsumerPattern, RegexOptions.Compiled);
    }

    /// <summary>
    /// Gets the connection value.
    /// </summary>
    public IConnection Connection => _connection;
    /// <summary>
    /// Gets the description value.
    /// </summary>
    public string Description { get; }
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress { get; }
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public IActiveMqBusTopology Topology { get; }

    /// <summary>
    /// Creates session.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ISession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _executor.ExecuteAsync(() => _connection.CreateSessionAsync(AcknowledgementMode.IndividualAcknowledge), tokenSource.Token)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether virtual topic consumer.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsVirtualTopicConsumer(string name)
    {
        return _virtualTopicConsumerPattern.IsMatch(name);
    }

    /// <summary>
    /// Gets temporary queue.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <returns>The result of the operation.</returns>
    public IQueue GetTemporaryQueue(ISession session, string topicName)
    {
        return (IQueue)_temporaryEntities.GetOrAdd(topicName, _ => (IQueue)SessionUtil.GetDestination(session, topicName, DestinationType.TemporaryQueue));
    }

    /// <summary>
    /// Gets temporary topic.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <returns>The result of the operation.</returns>
    public ITopic GetTemporaryTopic(ISession session, string topicName)
    {
        return (ITopic)_temporaryEntities.GetOrAdd(topicName, _ => (ITopic)SessionUtil.GetDestination(session, topicName, DestinationType.TemporaryTopic));
    }

    /// <summary>
    /// Attempts to get temporary entity.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="destination">The destination value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetTemporaryEntity(string name, out IDestination? destination)
    {
        return _temporaryEntities.TryGetValue(name, out destination);
    }

    /// <summary>
    /// Performs the try remove temporary entity operation.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryRemoveTemporaryEntity(ISession session, string name)
    {
        if (_temporaryEntities.TryRemove(name, out var destination))
        {
            try
            {
                session.DeleteDestination(destination);
                return true;
            }
            catch
            {
                // A failed broker delete must remain discoverable for a later retry. Do not
                // overwrite a newer mapping if another operation recreated the same name.
                _temporaryEntities.TryAdd(name, destination);
                throw;
            }
        }

        return false;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        TransportLogMessages.DisconnectHost(Description);
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

        TransportLogMessages.DisconnectedHost(Description);
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
