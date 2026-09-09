namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Exposes an execute-activity pipeline to core middleware without coupling it to the Courier application API.</summary>
/// <typeparam name="TActivity">The activity implementation type.</typeparam>
/// <typeparam name="TArguments">The activity-arguments contract type.</typeparam>
public interface IExecuteActivityPipeConfigurator<TActivity, TArguments> :
    IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>>,
    IConsumeConfigurator,
    IActivityObserverConnector
    where TActivity : class
    where TArguments : class
{
    /// <summary>Gets the Courier transport-message type that activates execution.</summary>
    Type MessageType { get; }

    /// <summary>Configures the arguments pipeline before the activity instance is invoked.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure);

    /// <summary>Configures the activity transport-message pipeline without exposing a capability-specific message type to the core.</summary>
    /// <typeparam name="TMessage">The activity transport-message contract type.</typeparam>
    /// <param name="configure">The callback that configures the transport-message consume pipeline.</param>
    void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class;
}
