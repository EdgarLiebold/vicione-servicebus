using System;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for host compensate activity operations.</summary>
/// <typeparam name="TActivity">The compensation activity type.</typeparam>
/// <typeparam name="TLog">The compensation log contract.</typeparam>
public class HostCompensateActivityContext<TActivity, TLog> :
    CompensateContextProxy<TLog>,
    CompensateActivityContext<TActivity, TLog>
    where TActivity : class
    where TLog : class
{
    readonly TActivity _activity;

    /// <summary>Creates a compensation context bound to its activity instance.</summary>
    /// <param name="activity">The compensation activity instance.</param>
    /// <param name="context">The compensation context to wrap.</param>
    public HostCompensateActivityContext(TActivity activity, CompensateContext<TLog> context)
        : base(context)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    TActivity CompensateActivityContext<TActivity, TLog>.Activity => _activity;
}
