namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for send pipe.</summary>
public interface ISendPipeSpecification :
    ISendPipeSpecificationObserverConnector,
    ISpecification
{
    /// <summary>Returns the specification for the message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message specification.</returns>
    IMessageSendPipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
