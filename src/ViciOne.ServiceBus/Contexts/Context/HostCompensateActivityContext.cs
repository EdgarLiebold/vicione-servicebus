namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a host compensate activity context implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class HostCompensateActivityContext<TActivity, TLog> :
    CompensateContextProxy<TLog>,
    CompensateActivityContext<TActivity, TLog>
    where TActivity : class
    where TLog : class
{
    readonly TActivity _activity;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <param name="context">The operation context.</param>
    public HostCompensateActivityContext(TActivity activity, CompensateContext<TLog> context)
        : base(context)
    {
        _activity = activity;
    }

    TActivity CompensateActivityContext<TActivity, TLog>.Activity => _activity;
}
