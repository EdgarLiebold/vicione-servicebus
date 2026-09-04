namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send pipe specification observer.
/// </summary>
public interface ISendPipeSpecificationObserver
{
    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class;
}
