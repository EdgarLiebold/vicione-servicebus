using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Receives notifications about activity events.</summary>
public interface IActivityObserver
{
    /// <summary>Called before a message is dispatched to any consumers.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class;

    /// <summary>Called after the message has been dispatched to all consumers when one or more exceptions have occurred.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
        where TActivity : class
        where TArguments : class;

    /// <summary>Called before a message is dispatched to any consumers.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class;

    /// <summary>Called after the message has been dispatched to all consumers when one or more exceptions have occurred.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
        where TActivity : class
        where TLog : class;
}
