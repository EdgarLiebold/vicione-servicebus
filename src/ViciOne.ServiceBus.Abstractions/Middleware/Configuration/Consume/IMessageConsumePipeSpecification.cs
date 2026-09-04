namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message consume pipe specification.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageConsumePipeSpecification<TMessage> :
    IMessageConsumePipeConfigurator<TMessage>,
    ISpecificationPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    /// <summary>
    /// Adds parent message specification to the configuration.
    /// </summary>
    /// <param name="parentSpecification">The parent specification value.</param>
    void AddParentMessageSpecification(ISpecificationPipeSpecification<ConsumeContext<TMessage>> parentSpecification);

    /// <summary>
    /// Performs the build message pipe operation.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(IPipe<ConsumeContext<TMessage>> pipe);
}


/// <summary>
/// Defines the contract for message consume pipe specification.
/// </summary>
public interface IMessageConsumePipeSpecification :
    IPipeConfigurator<ConsumeContext>,
    ISpecification
{
    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IMessageConsumePipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
