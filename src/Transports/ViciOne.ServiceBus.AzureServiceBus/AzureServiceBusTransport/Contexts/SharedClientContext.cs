using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Leases a client context with a caller-specific cancellation token without owning the underlying processor.</summary>
public class SharedClientContext :
    ProxyPipeContext,
    ClientContext
{
    readonly ClientContext _context;

    /// <summary>Initializes a lease over an existing client context.</summary>
    /// <param name="context">The shared processor context.</param>
    /// <param name="cancellationToken">The token that bounds this lease.</param>
    public SharedClientContext(ClientContext context, CancellationToken cancellationToken)
        : base(context)
    {
        CancellationToken = cancellationToken;
        _context = context;
    }

    /// <summary>Gets the token that bounds this lease.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the connection context.</summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _context.InputAddress;

    /// <summary>Gets the entity path.</summary>
    public string EntityPath => _context.EntityPath;

    /// <summary>Gets a value indicating whether closed or closing.</summary>
    public bool IsClosedOrClosing => _context.IsClosedOrClosing;

    /// <summary>Forwards asynchronous message and error callback registration to the shared client.</summary>
    /// <param name="callback">The callback that processes each received message.</param>
    /// <param name="exceptionHandler">The callback that processes SDK processor errors.</param>
    public void OnMessageAsync(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        _context.OnMessageAsync(callback, exceptionHandler);
    }

    /// <summary>Forwards asynchronous session-message and error callback registration to the shared client.</summary>
    /// <param name="callback">The callback that processes each received session message.</param>
    /// <param name="exceptionHandler">The callback that processes SDK processor errors.</param>
    public void OnSessionAsync(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        _context.OnSessionAsync(callback, exceptionHandler);
    }

    /// <summary>Starts the shared client's configured processor.</summary>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A task that completes when the processor has started.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _context.StartAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Stops processing on the shared client.</summary>
    /// <param name="cancellationToken">The token that cancels the stop request.</param>
    /// <returns>A task that completes after the shared client handles shutdown.</returns>
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        return _context.ShutdownAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Closes the shared client's processor.</summary>
    /// <param name="cancellationToken">The token that cancels closure.</param>
    /// <returns>A task that completes after the shared client handles closure.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _context.CloseAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Forwards a non-transient processor fault to the shared client.</summary>
    /// <param name="exception">The processor failure.</param>
    /// <param name="entityPath">The affected entity path.</param>
    /// <param name="cancellationToken">The token that cancels fault notification.</param>
    /// <returns>A task that completes when the shared client handles the notification.</returns>
    public Task NotifyFaultedAsync(Exception exception, string entityPath, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(exception, entityPath, cancellationToken: cancellationToken);
    }
}
