namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for message consume pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageConsumePipeSpecification<TMessage> :
    IMessageConsumePipeConfigurator<TMessage>,
    ISpecificationPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    /// <summary>Adds parent message specification to the configuration.</summary>
    /// <param name="parentSpecification">The parent specification.</param>
    void AddParentMessageSpecification(ISpecificationPipeSpecification<ConsumeContext<TMessage>> parentSpecification);

    /// <summary>Builds message pipe.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>The configured message pipe.</returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(IPipe<ConsumeContext<TMessage>> pipe);
}


/// <summary>Describes requirements for message consume pipe.</summary>
public interface IMessageConsumePipeSpecification :
    IPipeConfigurator<ConsumeContext>,
    ISpecification
{
    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    IMessageConsumePipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
