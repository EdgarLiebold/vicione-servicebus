namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send pipe specification.
/// </summary>
public interface ISendPipeSpecification :
    ISendPipeSpecificationObserverConnector,
    ISpecification
{
    /// <summary>
    /// Returns the specification for the message type
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    IMessageSendPipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
