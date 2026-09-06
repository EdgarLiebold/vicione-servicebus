namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for message send pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageSendPipeSpecification<TMessage> :
    IPipeConfigurator<SendContext<TMessage>>,
    ISpecificationPipeSpecification<SendContext<TMessage>>
    where TMessage : class
{
    /// <summary>Adds parent message specification to the configuration.</summary>
    /// <param name="parentSpecification">The parent specification.</param>
    void AddParentMessageSpecification(ISpecificationPipeSpecification<SendContext<TMessage>> parentSpecification);

    /// <summary>Build the pipe for the specification.</summary>
    /// <returns>The configured message pipe.</returns>
    IPipe<SendContext<TMessage>> BuildMessagePipe();
}


/// <summary>Describes requirements for message send pipe.</summary>
public interface IMessageSendPipeSpecification :
    IPipeConfigurator<SendContext>,
    ISpecification
{
    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    IMessageSendPipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
