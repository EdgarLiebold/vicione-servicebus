using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a queue client context implementation.
/// </summary>
public class QueueClientContext :
    BasePipeContext,
    ClientContext,
    IAsyncDisposable
{
    readonly IAgent _agent;
    readonly object _faultStopLock = new object();
    Task? _faultStopTask;
    readonly ReceiveSettings _settings;
    ServiceBusProcessor? _processor;
    ServiceBusSessionProcessor? _sessionProcessor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContext">The connection context value.</param>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="agent">The agent value.</param>
    public QueueClientContext(ConnectionContext connectionContext, Uri inputAddress, ReceiveSettings settings, IAgent agent)
    {
        _settings = settings;
        _agent = agent;
        ConnectionContext = connectionContext;
        InputAddress = inputAddress;
    }

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Gets the entity path value.
    /// </summary>
    public string EntityPath => _processor?.EntityPath ?? _sessionProcessor?.EntityPath
        ?? throw new InvalidOperationException("The Azure Service Bus queue client has not been initialized.");

    /// <summary>
    /// Gets the is closed or closing value.
    /// </summary>
    public bool IsClosedOrClosing => _processor?.IsClosed ?? _sessionProcessor?.IsClosed ?? false;

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress { get; }

    /// <summary>
    /// Performs the on message operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <param name="exceptionHandler">The exception handler value.</param>
    public void OnMessageAsync(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        if (_processor != null)
            throw new InvalidOperationException("OnMessageAsync can only be called once");
        if (_sessionProcessor != null)
            throw new InvalidOperationException("OnMessageAsync cannot be called with operating on a session");

        _processor = ConnectionContext.CreateQueueProcessor(_settings);

        _processor.ProcessMessageAsync += args => callback(args, args.Message, args.CancellationToken);
        _processor.ProcessErrorAsync += exceptionHandler;
    }

    /// <summary>
    /// Performs the on session operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <param name="exceptionHandler">The exception handler value.</param>
    public void OnSessionAsync(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        if (_sessionProcessor != null)
            throw new InvalidOperationException("OnSessionAsync can only be called once");
        if (_processor != null)
            throw new InvalidOperationException("OnSessionAsync cannot be called with operating without a session");

        _sessionProcessor = ConnectionContext.CreateQueueSessionProcessor(_settings);

        _sessionProcessor.ProcessMessageAsync += args => callback(args, args.Message, args.CancellationToken);
        _sessionProcessor.ProcessErrorAsync += exceptionHandler;
    }

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_processor != null)
            await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

        if (_sessionProcessor != null)
            await _sessionProcessor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the shutdown operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_processor is { IsClosed: false })
                await _processor.StopProcessingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_sessionProcessor is { IsClosed: false })
                await _sessionProcessor.StopProcessingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Stop processing client faulted: {InputAddress}", InputAddress);
        }
    }

    /// <summary>
    /// Performs the close operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_processor is { IsClosed: false })
                await _processor.CloseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_sessionProcessor is { IsClosed: false })
                await _sessionProcessor.CloseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Close client faulted: {InputAddress}", InputAddress);
        }
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="entityPath">The entity path value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyFaultedAsync(Exception exception, string entityPath, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken);        // Azure invokes this from the processor callback. Defer closing the same processor, but
        // retain the task and consume every stop outcome in this context owner.
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

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
    }
}
