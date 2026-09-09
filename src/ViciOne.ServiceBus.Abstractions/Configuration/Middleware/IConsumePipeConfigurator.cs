namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware shared by all messages delivered through a consume pipeline.</summary>
public interface IConsumePipeConfigurator :
    IPipeConfigurator<ConsumeContext>,
    IConsumerConfigurationObserverConnector,
    ISagaConfigurationObserverConnector,
    IHandlerConfigurationObserverConnector,
    IActivityConfigurationObserverConnector,
    IConsumerConfigurationObserver,
    ISagaConfigurationObserver,
    IHandlerConfigurationObserver,
    IActivityConfigurationObserver
{
    /// <summary>Sets whether the transport starts before a consumer pipeline is connected.</summary>
    bool AutoStart { set; }

    /// <summary>Adds middleware for a specific consumed message contract.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPipeSpecification<TMessage>(IPipeSpecification<ConsumeContext<TMessage>> specification)
        where TMessage : class;

    /// <summary>
    /// Adds middleware before message-type routing so one instance processes every consumed contract.
    /// </summary>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPrePipeSpecification(IPipeSpecification<ConsumeContext> specification);
}
