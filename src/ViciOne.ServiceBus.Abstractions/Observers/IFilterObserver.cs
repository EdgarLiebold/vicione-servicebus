using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Receives notifications about filter events.</summary>
public interface IFilterObserver
{
    /// <summary>Called before a message is dispatched to any consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreSendAsync<T>(T context)
        where T : class, PipeContext;

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostSendAsync<T>(T context)
        where T : class, PipeContext;

    /// <summary>Called after the message has been dispatched to all consumers when one or more exceptions have occurred.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendFaultAsync<T>(T context, Exception exception)
        where T : class, PipeContext;
}


/// <summary>Receives notifications about filter events.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IFilterObserver<in TContext>
    where TContext : class, PipeContext
{
    /// <summary>Called before a message is dispatched to any consumers.</summary>
    /// <param name="context">The consume context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreSendAsync(TContext context);

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead.
    /// </summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostSendAsync(TContext context);

    /// <summary>Called after the message has been dispatched to all consumers when one or more exceptions have occurred.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendFaultAsync(TContext context, Exception exception);
}
