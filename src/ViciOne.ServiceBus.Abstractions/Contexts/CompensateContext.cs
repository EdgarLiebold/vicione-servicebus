using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for compensate context.
/// </summary>
public interface CompensateContext :
    CourierContext
{
    /// <summary>
    /// Set the compensation result, which completes the activity
    /// </summary>
    CompensationResult Result { get; set; }

    /// <summary>
    /// The compensation was successful
    /// </summary>
    /// <returns></returns>
    CompensationResult Compensated();

    /// <summary>
    /// The compensation was successful
    /// </summary>
    /// <param name="values">The variables to be updated on the routing slip</param>
    /// <returns></returns>
    CompensationResult Compensated(object values);

    /// <summary>
    /// The compensation was successful
    /// </summary>
    /// <param name="variables">The variables to be updated on the routing slip</param>
    /// <returns></returns>
    CompensationResult Compensated(IDictionary<string, object> variables);

    /// <summary>
    /// The compensation failed
    /// </summary>
    /// <returns></returns>
    CompensationResult Failed();

    /// <summary>
    /// The compensation failed with the specified exception
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    CompensationResult Failed(Exception exception);
}


/// <summary>
/// Defines the contract for compensate context.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface CompensateContext<out TLog> :
    CompensateContext
    where TLog : class
{
    /// <summary>
    /// The execution log from the activity execution
    /// </summary>
    TLog Log { get; }

    /// <summary>
    /// Creates activity context.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <param name="activity">The activity value.</param>
    /// <returns>The result of the operation.</returns>
    CompensateActivityContext<TActivity, TLog> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class, ICompensateActivity<TLog>;
}
