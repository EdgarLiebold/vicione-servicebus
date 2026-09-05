using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for activity observer.
/// </summary>
public interface IActivityObserver
{
    /// <summary>
    /// Called before a message is dispatched to any consumers
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <returns></returns>
    Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers when one or more exceptions have occurred
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
        where TActivity : class
        where TArguments : class;

    /// <summary>
    /// Called before a message is dispatched to any consumers
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <returns></returns>
    Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers when one or more exceptions have occurred
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
        where TActivity : class
        where TLog : class;
}
