namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Exposes an execute-activity pipeline to core middleware without coupling it to the Courier application API.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityPipeConfigurator<TActivity, TArguments> :
    IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>>,
    IConsumeConfigurator,
    IActivityObserverConnector
    where TActivity : class
    where TArguments : class
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }

    /// <summary>Configures the arguments pipeline before the activity instance is invoked.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure);

    /// <summary>Configures the activity transport-message pipeline without exposing a capability-specific message type to the core.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class;
}
