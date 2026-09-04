using System.ComponentModel;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus;

public interface ISendPipeConfigurator :
    IPipeConfigurator<SendContext>,
    ISendPipeSpecificationObserverConnector
{
    /// <summary>
    /// Adds a type-specific pipe specification to the consume pipe
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="specification"></param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void AddPipeSpecification<T>(IPipeSpecification<SendContext<T>> specification)
        where T : class;
}
