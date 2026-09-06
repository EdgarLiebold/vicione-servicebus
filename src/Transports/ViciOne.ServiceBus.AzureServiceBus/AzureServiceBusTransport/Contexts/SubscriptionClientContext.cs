using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Owns the Azure Service Bus processor for a topic subscription and coordinates it with its supervisor.</summary>
public class SubscriptionClientContext :
    BasePipeContext,
    ClientContext,
    IAsyncDisposable
{
    readonly IAgent _agent;
    readonly object _faultStopLock = new object();
    Task? _faultStopTask;
    readonly SubscriptionSettings _settings;
    ServiceBusProcessor? _queueClient;
    ServiceBusSessionProcessor? _sessionClient;

    /// <summary>Initializes a subscription client before its message or session processor is selected.</summary>
    /// <param name="connectionContext">The namespace connection used to create the processor.</param>
    /// <param name="inputAddress">The subscription transport address.</param>
    /// <param name="settings">The topic, subscription, and processor settings.</param>
    /// <param name="agent">The supervising agent stopped after an unrecoverable processor fault.</param>
    public SubscriptionClientContext(ConnectionContext connectionContext, Uri inputAddress,
        SubscriptionSettings settings, IAgent agent)
    {
        _settings = settings;
        _agent = agent;

        ConnectionContext = connectionContext;
        InputAddress = inputAddress;
    }

    /// <summary>Gets the namespace connection that owns the processor.</summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>Gets the subscription's source topic path.</summary>
    public string EntityPath => _settings.CreateTopicOptions.Name;

    /// <summary>Gets whether the initialized processor is closed.</summary>
    public bool IsClosedOrClosing => _sessionClient?.IsClosed ?? _queueClient?.IsClosed ?? false;

    /// <summary>Gets the topic-subscription transport address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Creates a non-session subscription processor and registers its asynchronous message and error callbacks.</summary>
    /// <param name="callback">The callback that processes each received message.</param>
    /// <param name="exceptionHandler">The callback that processes SDK processor errors.</param>
    public void OnMessageAsync(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        if (_queueClient != null)
            throw new InvalidOperationException("OnMessageAsync can only be called once");
        if (_sessionClient != null)
            throw new InvalidOperationException("OnMessageAsync cannot be called with operating on a session");

        _queueClient = ConnectionContext.CreateSubscriptionProcessor(_settings);

        _queueClient.ProcessMessageAsync += args => callback(args, args.Message, args.CancellationToken);
        _queueClient.ProcessErrorAsync += exceptionHandler;
    }

    /// <summary>Creates a session-aware subscription processor and registers its asynchronous message and error callbacks.</summary>
    /// <param name="callback">The callback that processes each received session message.</param>
    /// <param name="exceptionHandler">The callback that processes SDK processor errors.</param>
    public void OnSessionAsync(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        if (_sessionClient != null)
            throw new InvalidOperationException("OnSessionAsync can only be called once");
        if (_queueClient != null)
            throw new InvalidOperationException("OnSessionAsync cannot be called with operating without a session");

        _sessionClient = ConnectionContext.CreateSubscriptionSessionProcessor(_settings);

        _sessionClient.ProcessMessageAsync += args => callback(args, args.Message, args.CancellationToken);
        _sessionClient.ProcessErrorAsync += exceptionHandler;
    }

    /// <summary>Starts the configured subscription processor.</summary>
    /// <param name="cancellationToken">The token that cancels processor startup.</param>
    /// <returns>A task that completes when the processor has started.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_queueClient != null)
            await _queueClient.StartProcessingAsync(cancellationToken: cancellationToken);

        if (_sessionClient != null)
            await _sessionClient.StartProcessingAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Stops message processing and logs, rather than propagates, stop failures.</summary>
    /// <param name="cancellationToken">The token that cancels the stop request.</param>
    /// <returns>A task that completes after the stop attempt.</returns>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_queueClient is { IsClosed: false })
                await _queueClient.StopProcessingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_sessionClient is { IsClosed: false })
                await _sessionClient.StopProcessingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Stop processing client faulted: {InputAddress}", InputAddress);
        }
    }

    /// <summary>Closes the processor and logs, rather than propagates, close failures.</summary>
    /// <param name="cancellationToken">The token that cancels processor closure.</param>
    /// <returns>A task that completes after the close attempt.</returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_queueClient is { IsClosed: false })
                await _queueClient.CloseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_sessionClient is { IsClosed: false })
                await _sessionClient.CloseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Close client faulted: {InputAddress}", InputAddress);
        }
    }

    /// <summary>Schedules supervised shutdown after a non-transient processor fault without closing the processor inside its callback.</summary>
    /// <param name="exception">The processor exception reported by the callback.</param>
    /// <param name="entityPath">The path included in the supervisor stop reason.</param>
    /// <param name="cancellationToken">The token checked before scheduling shutdown.</param>
    /// <returns>A completed task once shutdown has been scheduled, or a canceled task when cancellation was already requested.</returns>
    public Task NotifyFaultedAsync(Exception exception, string entityPath, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken);

        // Closing an Azure processor from its own callback can deadlock. Defer supervisor shutdown,
        // retain the task, and observe the stop outcome in this context owner.
        lock (_faultStopLock)
        {
            if (_faultStopTask == null || _faultStopTask.IsCompleted)
                _faultStopTask = StopAfterCallbackAsync(entityPath);
        }

        return Task.CompletedTask;
    }

    async Task StopAfterCallbackAsync(string entityPath)
    {
        await Task.Yield();

        try
        {
            await _agent.StopAsync($"Unrecoverable exception on {entityPath}").ConfigureAwait(false);
        }
        catch (Exception stopException)
        {
            LogContext.Error?.Log(stopException, "Stopping faulted Azure client context failed: {EntityPath}", entityPath);
        }
    }

    /// <summary>Closes the subscription processor.</summary>
    /// <returns>A task that completes after the close attempt.</returns>
    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
    }
}
