using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Intercepts the ConsumeContext.</summary>
/// <typeparam name="T">The message type.</typeparam>
public interface IConsumeMessageObserver<in T>
    where T : class
{
    /// <summary>Called before a message is dispatched to any consumers.</summary>
    /// <param name="context">The consume context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreConsumeAsync(ConsumeContext<T> context);

    /// <summary>Called after consumer dispatch completes successfully. Dispatch failures are reported to ConsumeFaultAsync.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostConsumeAsync(ConsumeContext<T> context);

    /// <summary>Called when the observed consumer dispatch pipeline fails.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConsumeFaultAsync(ConsumeContext<T> context, Exception exception);
}
