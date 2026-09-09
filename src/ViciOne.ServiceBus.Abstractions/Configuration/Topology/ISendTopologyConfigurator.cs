namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures message-specific send topology and failure-queue naming.</summary>
public interface ISendTopologyConfigurator :
    ISendTopology,
    ISpecification
{
    /// <summary>
    /// Gets or sets the convention used to derive a dead-letter queue name from its receive queue.
    /// </summary>
    new IDeadLetterQueueNameFormatter DeadLetterQueueNameFormatter { get; set; }

    /// <summary>
    /// Gets or sets the convention used to derive an error queue name from its receive queue.
    /// </summary>
    new IErrorQueueNameFormatter ErrorQueueNameFormatter { get; set; }

    /// <summary>
    /// Adds a convention that is evaluated when send topology is created for a message contract.
    /// </summary>
    /// <param name="convention">The send-topology convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(ISendTopologyConvention convention);

    /// <summary>Adds send topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="topology">The send topology to add.</param>
    void AddMessageSendTopology<TMessage>(IMessageSendTopology<TMessage> topology)
        where TMessage : class;
}
