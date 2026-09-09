namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Exposes a compensate-activity pipeline to core middleware without coupling it to the Courier application API.</summary>
/// <typeparam name="TActivity">The activity implementation type.</typeparam>
/// <typeparam name="TLog">The compensation-log contract type.</typeparam>
public interface ICompensateActivityPipeConfigurator<TActivity, TLog> :
    IPipeConfigurator<CompensateActivityContext<TActivity, TLog>>,
    IConsumeConfigurator,
    IActivityObserverConnector
    where TActivity : class
    where TLog : class
{
    /// <summary>Gets the Courier transport-message type that activates compensation.</summary>
    Type MessageType { get; }

    /// <summary>Configures the compensation-log pipeline before the activity instance is invoked.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Log(Action<ICompensateLogConfigurator<TLog>> configure);

    /// <summary>Configures the activity transport-message pipeline without exposing a capability-specific message type to the core.</summary>
    /// <typeparam name="TMessage">The activity transport-message contract type.</typeparam>
    /// <param name="configure">The callback that configures the transport-message consume pipeline.</param>
    void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class;
}
