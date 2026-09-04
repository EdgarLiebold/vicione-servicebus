namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for compensate activity context.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface CompensateActivityContext<out TLog> :
    CompensateContext<TLog>
    where TLog : class
{
}


/// <summary>
/// Defines the contract for compensate activity context.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface CompensateActivityContext<out TActivity, out TLog> :
    CompensateActivityContext<TLog>
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    /// <summary>
    /// The activity that was created/used for this compensation
    /// </summary>
    TActivity Activity { get; }
}
