namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for message publish pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessagePublishPipeSpecification<TMessage> :
    IPipeConfigurator<PublishContext<TMessage>>,
    ISpecificationPipeSpecification<PublishContext<TMessage>>
    where TMessage : class
{
    /// <summary>Adds parent message specification to the configuration.</summary>
    /// <param name="implementedMessageTypeSpecification">The implemented message type specification.</param>
    void AddParentMessageSpecification(ISpecificationPipeSpecification<PublishContext<TMessage>> implementedMessageTypeSpecification);

    /// <summary>Build the pipe for the specification.</summary>
    /// <returns>The configured message pipe.</returns>
    IPipe<PublishContext<TMessage>> BuildMessagePipe();
}


/// <summary>Describes requirements for message publish pipe.</summary>
public interface IMessagePublishPipeSpecification :
    IPipeConfigurator<PublishContext>,
    ISpecification
{
    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    IMessagePublishPipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
