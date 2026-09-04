using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

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

    public QueueClientContext(ConnectionContext connectionContext, Uri inputAddress, ReceiveSettings settings, IAgent agent)
    {
        _settings = settings;
        _agent = agent;
        ConnectionContext = connectionContext;
        InputAddress = inputAddress;
    }

    public ConnectionContext ConnectionContext { get; }

    public string EntityPath => _processor?.EntityPath ?? _sessionProcessor?.EntityPath
        ?? throw new InvalidOperationException("The Azure Service Bus queue client has not been initialized.");

    public bool IsClosedOrClosing => _processor?.IsClosed ?? _sessionProcessor?.IsClosed ?? false;

    public Uri InputAddress { get; }

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

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_processor != null)
            await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

        if (_sessionProcessor != null)
            await _sessionProcessor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
    }

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

    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
    }
}
