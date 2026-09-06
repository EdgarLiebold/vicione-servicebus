namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Exposes an execute-activity pipeline to core middleware without coupling it to the Courier application API.
/// </summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The argument type.</typeparam>
public interface IExecuteActivityPipeConfigurator<TActivity, TArguments> :
    IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>>,
    IConsumeConfigurator,
    IActivityObserverConnector
    where TActivity : class
    where TArguments : class
{
    /// <summary>
    /// Gets the transport message type used to host the activity pipeline.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Configures the arguments pipeline before the activity instance is invoked.
    /// </summary>
    /// <param name="configure">The arguments-pipeline callback.</param>
    void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure);

    /// <summary>
    /// Configures the activity transport-message pipeline without exposing a capability-specific message type to the core.
    /// </summary>
    /// <typeparam name="TMessage">The activity transport-message type.</typeparam>
    /// <param name="configure">The message-pipeline callback.</param>
    void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class;
}
