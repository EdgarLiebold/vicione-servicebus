using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures send pipe.</summary>
public interface ISendPipeConfigurator :
    IPipeConfigurator<SendContext>,
    ISendPipeSpecificationObserverConnector
{
    /// <summary>Adds a type-specific pipe specification to the consume pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="specification">The specification.</param>
    void AddPipeSpecification<T>(IPipeSpecification<SendContext<T>> specification)
        where T : class;
}
