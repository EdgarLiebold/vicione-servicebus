using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class SharedClientContext :
    ProxyPipeContext,
    ClientContext
{
    readonly ClientContext _context;

    public SharedClientContext(ClientContext context, CancellationToken cancellationToken)
        : base(context)
    {
        CancellationToken = cancellationToken;
        _context = context;
    }

    public override CancellationToken CancellationToken { get; }

    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    public Uri InputAddress => _context.InputAddress;

    public string EntityPath => _context.EntityPath;

    public bool IsClosedOrClosing => _context.IsClosedOrClosing;

    public void OnMessageAsync(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        _context.OnMessageAsync(callback, exceptionHandler);
    }

    public void OnSessionAsync(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler)
    {
        _context.OnSessionAsync(callback, exceptionHandler);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _context.StartAsync(cancellationToken: cancellationToken);
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        return _context.ShutdownAsync(cancellationToken: cancellationToken);
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _context.CloseAsync(cancellationToken: cancellationToken);
    }

    public Task NotifyFaultedAsync(Exception exception, string entityPath, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(exception, entityPath, cancellationToken: cancellationToken);
    }
}
