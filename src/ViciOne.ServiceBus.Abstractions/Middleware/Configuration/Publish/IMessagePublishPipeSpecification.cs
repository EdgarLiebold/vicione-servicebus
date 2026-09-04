namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message publish pipe specification.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessagePublishPipeSpecification<TMessage> :
    IPipeConfigurator<PublishContext<TMessage>>,
    ISpecificationPipeSpecification<PublishContext<TMessage>>
    where TMessage : class
{
    /// <summary>
    /// Adds parent message specification to the configuration.
    /// </summary>
    /// <param name="implementedMessageTypeSpecification">The implemented message type specification value.</param>
    void AddParentMessageSpecification(ISpecificationPipeSpecification<PublishContext<TMessage>> implementedMessageTypeSpecification);

    /// <summary>
    /// Build the pipe for the specification
    /// </summary>
    /// <returns></returns>
    IPipe<PublishContext<TMessage>> BuildMessagePipe();
}


/// <summary>
/// Defines the contract for message publish pipe specification.
/// </summary>
public interface IMessagePublishPipeSpecification :
    IPipeConfigurator<PublishContext>,
    ISpecification
{
    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IMessagePublishPipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
