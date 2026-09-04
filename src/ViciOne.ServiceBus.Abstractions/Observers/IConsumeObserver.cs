using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Intercepts the ConsumeContext
/// </summary>
public interface IConsumeObserver
{
    /// <summary>
    /// Called before a message is dispatched to any consumers
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <returns></returns>
    Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers - note that in the case of an exception
    /// this method is not called, and the DispatchFaulted method is called instead
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class;

    /// <summary>
    /// Called after the message has been dispatched to all consumers when one or more exceptions have occurred
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class;
}
