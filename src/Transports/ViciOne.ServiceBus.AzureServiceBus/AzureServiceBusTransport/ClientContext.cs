using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Controls the Azure Service Bus processor associated with a queue or subscription.</summary>
public interface ClientContext :
    NamespaceContext
{
    /// <summary>Gets the transport input address represented by this context.</summary>
    Uri InputAddress { get; }

    /// <summary>Gets the Azure Service Bus entity path.</summary>
    string EntityPath { get; }

    /// <summary>Gets whether the processor or its connection is closing or closed.</summary>
    bool IsClosedOrClosing { get; }

    /// <summary>Registers the asynchronous message and error callbacks used by a non-session processor.</summary>
    /// <param name="callback">The callback that processes each received message.</param>
    /// <param name="exceptionHandler">The callback that processes processor errors.</param>
    void OnMessageAsync(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler);

    /// <summary>Registers the asynchronous message and error callbacks used by a session processor.</summary>
    /// <param name="callback">The callback that processes each received session message.</param>
    /// <param name="exceptionHandler">The callback that processes processor errors.</param>
    void OnSessionAsync(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
        Func<ProcessErrorEventArgs, Task> exceptionHandler);

    /// <summary>Starts the configured message or session processor.</summary>
    /// <param name="cancellationToken">The token that cancels processor startup.</param>
    /// <returns>A task that completes when the processor has started.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops and disposes the configured message or session processor.</summary>
    /// <param name="cancellationToken">The token that cancels processor shutdown.</param>
    /// <returns>A task that completes when the processor has stopped and its resources have been released.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the configured message or session processor from receiving new deliveries.</summary>
    /// <param name="cancellationToken">The token that cancels the stop request.</param>
    /// <returns>A task that completes when the processor has stopped.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>Reports a non-transient client failure so the supervised context can be recycled.</summary>
    /// <param name="exception">The failure reported by the Azure Service Bus client.</param>
    /// <param name="entityPath">The path of the affected entity.</param>
    /// <param name="cancellationToken">The token that cancels fault notification.</param>
    /// <returns>A task that completes when the fault has been reported.</returns>
    Task NotifyFaultedAsync(Exception exception, string entityPath, CancellationToken cancellationToken = default);
}
