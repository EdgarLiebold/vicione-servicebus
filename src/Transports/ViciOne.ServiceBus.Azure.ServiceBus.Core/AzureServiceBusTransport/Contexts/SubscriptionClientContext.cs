using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

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

    public SubscriptionClientContext(ConnectionContext connectionContext, Uri inputAddress,
        SubscriptionSettings settings, IAgent agent)
    {
        _settings = settings;
        _agent = agent;

        ConnectionContext = connectionContext;
        InputAddress = inputAddress;
    }

    public ConnectionContext ConnectionContext { get; }

    public string EntityPath => _settings.CreateTopicOptions.Name;

    public bool IsClosedOrClosing => _sessionClient?.IsClosed ?? _queueClient?.IsClosed ?? false;

    public Uri InputAddress { get; }

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

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_queueClient != null)
            await _queueClient.StartProcessingAsync(cancellationToken: cancellationToken);

        if (_sessionClient != null)
            await _sessionClient.StartProcessingAsync(cancellationToken: cancellationToken);
    }

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
