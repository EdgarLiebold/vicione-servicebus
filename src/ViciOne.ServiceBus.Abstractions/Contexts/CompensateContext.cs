using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for compensate operations.</summary>
public interface CompensateContext :
    ActivityContext
{
    /// <summary>Set the compensation result, which completes the activity.</summary>
    CompensationResult Result { get; set; }

    /// <summary>The compensation was successful.</summary>
    /// <returns>The compensation result produced by the operation.</returns>
    CompensationResult Compensated();

    /// <summary>The compensation was successful.</summary>
    /// <param name="values">The variables to be updated on the routing slip.</param>
    /// <returns>The compensation result produced by the operation.</returns>
    CompensationResult Compensated(object values);

    /// <summary>The compensation was successful.</summary>
    /// <param name="variables">The variables to be updated on the routing slip.</param>
    /// <returns>The compensation result produced by the operation.</returns>
    CompensationResult Compensated(IDictionary<string, object> variables);

    /// <summary>The compensation failed.</summary>
    /// <returns>The compensation result produced by the operation.</returns>
    CompensationResult Failed();

    /// <summary>The compensation failed with the specified exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The compensation result produced by the operation.</returns>
    CompensationResult Failed(Exception exception);
}


/// <summary>Exposes state for compensate operations.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public interface CompensateContext<out TLog> :
    CompensateContext
    where TLog : class
{
    /// <summary>The execution log from the activity execution.</summary>
    TLog Log { get; }

    /// <summary>Creates activity context.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="activity">The activity.</param>
    /// <returns>The created activity context.</returns>
    CompensateActivityContext<TActivity, TLog> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class;
}
