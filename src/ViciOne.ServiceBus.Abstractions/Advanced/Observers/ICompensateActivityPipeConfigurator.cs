namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Exposes a compensate-activity pipeline to core middleware without coupling it to the Courier application API.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateActivityPipeConfigurator<TActivity, TLog> :
    IPipeConfigurator<CompensateActivityContext<TActivity, TLog>>,
    IConsumeConfigurator,
    IActivityObserverConnector
    where TActivity : class
    where TLog : class
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }

    /// <summary>Configures the compensation-log pipeline before the activity instance is invoked.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Log(Action<ICompensateLogConfigurator<TLog>> configure);

    /// <summary>Configures the activity transport-message pipeline without exposing a capability-specific message type to the core.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class;
}
