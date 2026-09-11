using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides transport data and completion tracking for a received message.</summary>
public interface ReceiveContext :
    PipeContext
{
    /// <summary>Gets the time elapsed since the transport read the message.</summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>Gets the endpoint address on which the message was received.</summary>
    Uri InputAddress { get; }

    /// <summary>Gets the content type determined from the transport metadata.</summary>
    ContentType ContentType { get; }

    /// <summary>Gets whether the transport marked the message as a redelivery.</summary>
    bool Redelivered { get; }

    /// <summary>Gets the transport-specific headers.</summary>
    Headers TransportHeaders { get; }

    /// <summary>Gets a task that completes after every registered receive task completes.</summary>
    Task ReceiveCompleted { get; }

    /// <summary>Gets whether at least one consumer completed successfully.</summary>
    bool IsDelivered { get; }

    /// <summary>Gets whether message delivery faulted.</summary>
    bool IsFaulted { get; }

    /// <summary>Gets the send endpoint provider associated with the receiving transport.</summary>
    ISendEndpointProvider SendEndpointProvider { get; }

    /// <summary>Gets the publish endpoint provider associated with the receiving transport.</summary>
    IPublishEndpointProvider PublishEndpointProvider { get; }

    /// <summary>Gets whether faults are published when neither a response nor fault address is available.</summary>
    bool PublishFaults { get; }

    /// <summary>Gets the body descriptor supplied by the receive source.</summary>
    MessageBody Body { get; }

    /// <summary>Records a successful consumer delivery.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The consume context of the message.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the successful delivery has been recorded and consume observers have been notified.</returns>
    Task NotifyConsumedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Records a consumer delivery fault.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The consume context of the message.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <param name="exception">The consumer exception.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the consumer fault has been recorded and consume observers have been notified.</returns>
    Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
        Exception exception, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Records a receive-pipeline fault that occurred outside a consumer.</summary>
    /// <param name="exception">The receive exception.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the receive fault has been recorded and receive observers have been notified.</returns>
    Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>Registers work that must finish before receive processing is complete.</summary>
    /// <param name="task">The pending receive task.</param>
    void AddReceiveTask(Task task);
}
