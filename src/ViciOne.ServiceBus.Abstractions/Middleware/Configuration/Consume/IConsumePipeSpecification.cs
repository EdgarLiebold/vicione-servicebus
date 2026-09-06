using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consume pipe.</summary>
public interface IConsumePipeSpecification :
    IConsumePipeSpecificationObserverConnector,
    ISpecification
{
    /// <summary>Returns the specification for the message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message specification.</returns>
    IMessageConsumePipeSpecification<T> GetMessageSpecification<T>()
        where T : class;

    /// <summary>Build the consume pipe for the specification.</summary>
    /// <returns>The configured consume pipe.</returns>
    IConsumePipe BuildConsumePipe();

    /// <summary>Creates consume pipe specification.</summary>
    /// <returns>The created consume pipe specification.</returns>
    IConsumePipeSpecification CreateConsumePipeSpecification();
}
