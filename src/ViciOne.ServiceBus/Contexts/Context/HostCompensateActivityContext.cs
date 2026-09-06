namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for host compensate activity operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class HostCompensateActivityContext<TActivity, TLog> :
    CompensateContextProxy<TLog>,
    CompensateActivityContext<TActivity, TLog>
    where TActivity : class
    where TLog : class
{
    readonly TActivity _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="context">The context associated with the operation.</param>
    public HostCompensateActivityContext(TActivity activity, CompensateContext<TLog> context)
        : base(context)
    {
        _activity = activity;
    }

    TActivity CompensateActivityContext<TActivity, TLog>.Activity => _activity;
}
