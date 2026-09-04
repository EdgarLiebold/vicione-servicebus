namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message send pipe specification.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageSendPipeSpecification<TMessage> :
    IPipeConfigurator<SendContext<TMessage>>,
    ISpecificationPipeSpecification<SendContext<TMessage>>
    where TMessage : class
{
    /// <summary>
    /// Adds parent message specification to the configuration.
    /// </summary>
    /// <param name="parentSpecification">The parent specification value.</param>
    void AddParentMessageSpecification(ISpecificationPipeSpecification<SendContext<TMessage>> parentSpecification);

    /// <summary>
    /// Build the pipe for the specification
    /// </summary>
    /// <returns></returns>
    IPipe<SendContext<TMessage>> BuildMessagePipe();
}


/// <summary>
/// Defines the contract for message send pipe specification.
/// </summary>
public interface IMessageSendPipeSpecification :
    IPipeConfigurator<SendContext>,
    ISpecification
{
    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IMessageSendPipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
