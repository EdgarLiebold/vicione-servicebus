using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides routing-slip state and result factories while an activity is compensated.</summary>
public interface CompensateContext :
    ActivityContext
{
    /// <summary>Gets or sets the result; the value is unset until the activity completes.</summary>
    CompensationResult? Result { get; set; }

    /// <summary>Creates a successful compensation result.</summary>
    /// <returns>A result that continues compensation.</returns>
    CompensationResult Compensated();

    /// <summary>Creates a successful compensation result with routing-slip variable updates.</summary>
    /// <param name="values">The variables to be updated on the routing slip.</param>
    /// <returns>A result that updates the variables and continues compensation.</returns>
    CompensationResult Compensated(object values);

    /// <summary>Creates a successful compensation result with routing-slip variable updates.</summary>
    /// <param name="variables">The variables to be updated on the routing slip.</param>
    /// <returns>A result that updates the variables and continues compensation.</returns>
    CompensationResult Compensated(IDictionary<string, object> variables);

    /// <summary>Creates a failed compensation result without an explicit exception.</summary>
    /// <returns>A result that faults compensation.</returns>
    CompensationResult Failed();

    /// <summary>Creates a failed compensation result with the specified exception.</summary>
    /// <param name="exception">The failure that stopped compensation.</param>
    /// <returns>A result that faults compensation.</returns>
    CompensationResult Failed(Exception exception);
}


/// <summary>Provides a typed compensation log together with its routing-slip context.</summary>
/// <typeparam name="TLog">The compensation-log contract.</typeparam>
public interface CompensateContext<out TLog> :
    CompensateContext
    where TLog : class
{
    /// <summary>Gets the log recorded when the activity completed execution.</summary>
    TLog Log { get; }

    /// <summary>Associates an activity instance with this compensation context.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <param name="activity">The activity instance that performs compensation.</param>
    /// <returns>The combined activity and compensation context.</returns>
    CompensateActivityContext<TActivity, TLog> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class;
}
