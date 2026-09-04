using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a shared client context implementation.
/// </summary>
public class SharedClientContext :
    ProxyPipeContext,
    ClientContext
{
    readonly ClientContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedClientContext(ClientContext context, CancellationToken cancellationToken)
        : base(context)
    {
        CancellationToken = cancellationToken;
        _context = context;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress => _context.InputAddress;

    /// <summary>
    /// Gets the entity path value.
    /// </summary>
    public string EntityPath => _context.EntityPath;

    /// <summary>
    /// Gets the is closed or closing value.
    /// </summary>
    public bool IsClosedOrClosing => _context.IsClosedOrClosing;

    /// <summary>
    /// Performs the on message operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <param name="exceptionHandler">The exception handler value.</param>
    public void OnMessageAsync(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        _context.OnMessageAsync(callback, exceptionHandler);
    }

    /// <summary>
    /// Performs the on session operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <param name="exceptionHandler">The exception handler value.</param>
    public void OnSessionAsync(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        _context.OnSessionAsync(callback, exceptionHandler);
    }

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _context.StartAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the shutdown operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        return _context.ShutdownAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the close operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _context.CloseAsync(cancellationToken: cancellationToken);
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
        return _context.NotifyFaultedAsync(exception, entityPath, cancellationToken: cancellationToken);
    }
}
