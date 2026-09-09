namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the compensation log and routing-slip context for an activity.</summary>
/// <typeparam name="TLog">The compensation-log contract.</typeparam>
public interface CompensateActivityContext<out TLog> :
    CompensateContext<TLog>
    where TLog : class
{
}


/// <summary>Provides an activity instance together with its compensation log and routing-slip context.</summary>
/// <typeparam name="TActivity">The activity implementation.</typeparam>
/// <typeparam name="TLog">The compensation-log contract.</typeparam>
public interface CompensateActivityContext<out TActivity, out TLog> :
    CompensateActivityContext<TLog>
    where TLog : class
    where TActivity : class
{
    /// <summary>Gets the activity instance that performs compensation.</summary>
    TActivity Activity { get; }
}
