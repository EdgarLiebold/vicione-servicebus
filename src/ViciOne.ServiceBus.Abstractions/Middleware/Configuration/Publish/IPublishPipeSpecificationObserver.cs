namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for publish pipe specification observer.
/// </summary>
public interface IPublishPipeSpecificationObserver
{
    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class;
}
