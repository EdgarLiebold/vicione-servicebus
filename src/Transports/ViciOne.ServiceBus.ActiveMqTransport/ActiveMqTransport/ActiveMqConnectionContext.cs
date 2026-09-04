using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMqTransport;

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

    public IConnection Connection => _connection;
    public string Description { get; }
    public Uri HostAddress { get; }
    public IActiveMqBusTopology Topology { get; }

    public async Task<ISession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _executor.ExecuteAsync(() => _connection.CreateSessionAsync(AcknowledgementMode.IndividualAcknowledge), tokenSource.Token)
            .ConfigureAwait(false);
    }

    public bool IsVirtualTopicConsumer(string name)
    {
        return _virtualTopicConsumerPattern.IsMatch(name);
    }

    public IQueue GetTemporaryQueue(ISession session, string topicName)
    {
        return (IQueue)_temporaryEntities.GetOrAdd(topicName, _ => (IQueue)SessionUtil.GetDestination(session, topicName, DestinationType.TemporaryQueue));
    }

    public ITopic GetTemporaryTopic(ISession session, string topicName)
    {
        return (ITopic)_temporaryEntities.GetOrAdd(topicName, _ => (ITopic)SessionUtil.GetDestination(session, topicName, DestinationType.TemporaryTopic));
    }

    public bool TryGetTemporaryEntity(string name, out IDestination? destination)
    {
        return _temporaryEntities.TryGetValue(name, out destination);
    }

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
