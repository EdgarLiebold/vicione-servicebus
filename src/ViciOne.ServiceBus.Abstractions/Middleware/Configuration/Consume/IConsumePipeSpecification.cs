using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consume pipe specification.
/// </summary>
public interface IConsumePipeSpecification :
    IConsumePipeSpecificationObserverConnector,
    ISpecification
{
    /// <summary>
    /// Returns the specification for the message type
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    IMessageConsumePipeSpecification<T> GetMessageSpecification<T>()
        where T : class;

    /// <summary>
    /// Build the consume pipe for the specification
    /// </summary>
    /// <returns></returns>
    IConsumePipe BuildConsumePipe();

    /// <summary>
    /// Creates consume pipe specification.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IConsumePipeSpecification CreateConsumePipeSpecification();
}
