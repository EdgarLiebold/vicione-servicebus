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

/// <summary>
/// Exposes a compensate-activity pipeline to core middleware without coupling it to the Courier application API.
/// </summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The compensation-log type.</typeparam>
public interface ICompensateActivityPipeConfigurator<TActivity, TLog> :
    IPipeConfigurator<CompensateActivityContext<TActivity, TLog>>,
    IConsumeConfigurator,
    IActivityObserverConnector
    where TActivity : class
    where TLog : class
{
    /// <summary>
    /// Gets the transport message type used to host the compensation pipeline.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Configures the compensation-log pipeline before the activity instance is invoked.
    /// </summary>
    /// <param name="configure">The compensation-log callback.</param>
    void Log(Action<ICompensateLogConfigurator<TLog>> configure);

    /// <summary>
    /// Configures the activity transport-message pipeline without exposing a capability-specific message type to the core.
    /// </summary>
    /// <typeparam name="TMessage">The activity transport-message type.</typeparam>
    /// <param name="configure">The message-pipeline callback.</param>
    void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class;
}

/// <summary>
/// Configures the transport message that hosts an optional activity capability.
/// </summary>
/// <typeparam name="TMessage">The activity transport-message type.</typeparam>
public interface IActivityMessageConfigurator<TMessage> :
    IConsumeConfigurator,
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class;
