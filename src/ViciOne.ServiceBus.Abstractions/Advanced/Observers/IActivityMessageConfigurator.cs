namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Configures the transport message that hosts an optional activity capability.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IActivityMessageConfigurator<TMessage> :
    IConsumeConfigurator,
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class;
