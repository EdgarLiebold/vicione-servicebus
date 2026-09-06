namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for publish pipe.</summary>
public interface IPublishPipeSpecification :
    IPublishPipeSpecificationObserverConnector,
    ISpecification
{
    /// <summary>Returns the specification for the message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message specification.</returns>
    IMessagePublishPipeSpecification<T> GetMessageSpecification<T>()
        where T : class;
}
