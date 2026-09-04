namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consume pipe specification observer.
/// </summary>
public interface IConsumePipeSpecificationObserver
{
    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        where T : class;
}
