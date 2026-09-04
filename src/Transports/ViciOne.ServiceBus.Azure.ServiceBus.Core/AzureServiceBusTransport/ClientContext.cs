using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

/// <summary>
/// The client context is used to access the queue/subscription/topic client.
/// </summary>
public interface ClientContext :
    NamespaceContext
{
    /// <summary>
    /// The input address for the client/transport
    /// </summary>
    Uri InputAddress { get; }

    /// <summary>
    /// The path of the messaging entity
    /// </summary>
    string EntityPath { get; }

    /// <summary>
    /// True if the client or connection is closed or closing
    /// </summary>
    bool IsClosedOrClosing { get; }

    /// <summary>
    /// Register an message handler for the client
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="exceptionHandler"></param>
    void OnMessageAsync(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler);

    /// <summary>
    /// Register a message session handler
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="exceptionHandler"></param>
    void OnSessionAsync(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler);

    /// <summary>
    /// Starts the message/session receivers
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown the message/session receivers
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Close down the message handler on the received
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify that an exception has occurred on the client which is not transient and requires a recycle
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="entityPath"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task NotifyFaultedAsync(Exception exception, string entityPath, CancellationToken cancellationToken = default);
}
