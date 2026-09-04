using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for publish pipe configurator.
/// </summary>
public interface IPublishPipeConfigurator :
    IPipeConfigurator<PublishContext>,
    IPublishPipeSpecificationObserverConnector
{
    /// <summary>
    /// Adds a type-specific pipe specification to the consume pipe
    /// </summary>
    /// <param name="specification"></param>
    void AddPipeSpecification(IPipeSpecification<SendContext> specification);

    /// <summary>
    /// Adds a type-specific pipe specification to the consume pipe
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="specification"></param>
    void AddPipeSpecification<T>(IPipeSpecification<SendContext<T>> specification)
        where T : class;

    /// <summary>
    /// Adds a type-specific pipe specification to the consume pipe
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="specification"></param>
    void AddPipeSpecification<T>(IPipeSpecification<PublishContext<T>> specification)
        where T : class;
}
