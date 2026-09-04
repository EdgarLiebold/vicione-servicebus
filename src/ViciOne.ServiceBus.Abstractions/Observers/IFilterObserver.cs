using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for filter observer.
/// </summary>
public interface IFilterObserver
{
    /// <summary>
    /// Called before a message is dispatched to any consumers
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <returns></returns>
    Task PreSendAsync<T>(T context)
        where T : class, PipeContext;

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PostSendAsync<T>(T context)
        where T : class, PipeContext;

    /// <summary>
    /// Called after the message has been dispatched to all consumers when one or more exceptions have occurred
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    Task SendFaultAsync<T>(T context, Exception exception)
        where T : class, PipeContext;
}


/// <summary>
/// Defines the contract for filter observer.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IFilterObserver<in TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Called before a message is dispatched to any consumers
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <returns></returns>
    Task PreSendAsync(TContext context);

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PostSendAsync(TContext context);

    /// <summary>
    /// Called after the message has been dispatched to all consumers when one or more exceptions have occurred
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    Task SendFaultAsync(TContext context, Exception exception);
}
