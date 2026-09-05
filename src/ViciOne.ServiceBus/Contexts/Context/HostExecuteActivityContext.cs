namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a host execute activity context implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class HostExecuteActivityContext<TActivity, TArguments> :
    ExecuteContextProxy<TArguments>,
    ExecuteActivityContext<TActivity, TArguments>
    where TArguments : class
    where TActivity : class
{
    readonly TActivity _activity;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <param name="context">The operation context.</param>
    public HostExecuteActivityContext(TActivity activity, ExecuteContext<TArguments> context)
        : base(context)
    {
        _activity = activity;
    }

    TActivity ExecuteActivityContext<TActivity, TArguments>.Activity => _activity;
}
