using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Intercepts the ConsumeContext.</summary>
public interface IConsumeObserver
{
    /// <summary>Called before a message is dispatched to any consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class;

    /// <summary>Called after consumer dispatch completes successfully. Dispatch failures are reported to ConsumeFaultAsync.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class;

    /// <summary>Called when the observed consumer dispatch pipeline fails.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class;
}
