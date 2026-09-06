namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for host execute activity operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class HostExecuteActivityContext<TActivity, TArguments> :
    ExecuteContextProxy<TArguments>,
    ExecuteActivityContext<TActivity, TArguments>
    where TArguments : class
    where TActivity : class
{
    readonly TActivity _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="context">The context associated with the operation.</param>
    public HostExecuteActivityContext(TActivity activity, ExecuteContext<TArguments> context)
        : base(context)
    {
        _activity = activity;
    }

    TActivity ExecuteActivityContext<TActivity, TArguments>.Activity => _activity;
}
