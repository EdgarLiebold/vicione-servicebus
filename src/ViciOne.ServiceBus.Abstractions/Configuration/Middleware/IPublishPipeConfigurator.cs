namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware applied while messages are published.</summary>
public interface IPublishPipeConfigurator :
    IPipeConfigurator<PublishContext>,
    IPublishPipeSpecificationObserverConnector
{
    /// <summary>Adds middleware shared by published messages at the send-context level.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPipeSpecification(IPipeSpecification<SendContext> specification);

    /// <summary>Adds send-context middleware for a published message contract.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPipeSpecification<TMessage>(IPipeSpecification<SendContext<TMessage>> specification)
        where TMessage : class;

    /// <summary>Adds publish-context middleware for a message contract.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPipeSpecification<TMessage>(IPipeSpecification<PublishContext<TMessage>> specification)
        where TMessage : class;
}
