using System;

namespace ViciOne.ServiceBus.Context;

/// <summary>Binds an activity instance to the execution context for its argument contract.</summary>
/// <typeparam name="TActivity">The execute activity type.</typeparam>
/// <typeparam name="TArguments">The activity argument contract.</typeparam>
internal sealed class HostExecuteActivityContext<TActivity, TArguments> :
    ExecuteContextProxy<TArguments>,
    ExecuteActivityContext<TActivity, TArguments>
    where TArguments : class
    where TActivity : class
{
    readonly TActivity _activity;

    /// <summary>Creates an execute context bound to its activity instance.</summary>
    /// <param name="activity">The execute activity instance.</param>
    /// <param name="context">The execute context to wrap.</param>
    public HostExecuteActivityContext(TActivity activity, ExecuteContext<TArguments> context)
        : base(context)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    TActivity ExecuteActivityContext<TActivity, TArguments>.Activity => _activity;
}
