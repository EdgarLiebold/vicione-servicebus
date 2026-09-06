namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for compensate activity operations.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public interface CompensateActivityContext<out TLog> :
    CompensateContext<TLog>
    where TLog : class
{
}


/// <summary>Exposes state for compensate activity operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface CompensateActivityContext<out TActivity, out TLog> :
    CompensateActivityContext<TLog>
    where TLog : class
    where TActivity : class
{
    /// <summary>The activity that was created/used for this compensation.</summary>
    TActivity Activity { get; }
}
