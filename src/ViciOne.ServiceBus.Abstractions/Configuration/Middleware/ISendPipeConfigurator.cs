namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware applied while messages are sent to an endpoint.</summary>
public interface ISendPipeConfigurator :
    IPipeConfigurator<SendContext>,
    ISendPipeSpecificationObserverConnector
{
    /// <summary>Adds send middleware for a message contract.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPipeSpecification<TMessage>(IPipeSpecification<SendContext<TMessage>> specification)
        where TMessage : class;
}
