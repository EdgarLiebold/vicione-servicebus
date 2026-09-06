namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Configures the transport message that hosts an optional activity capability.
/// </summary>
/// <typeparam name="TMessage">The activity transport-message type.</typeparam>
public interface IActivityMessageConfigurator<TMessage> :
    IConsumeConfigurator,
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class;
