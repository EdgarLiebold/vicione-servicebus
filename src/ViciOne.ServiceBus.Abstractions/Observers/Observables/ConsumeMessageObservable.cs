using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a consume message observable implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ConsumeMessageObservable<T> :
    Connectable<IConsumeMessageObserver<T>>,
    IConsumeMessageObserver<T>
    where T : class
{
    /// <summary>
    /// Performs the pre consume operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreConsumeAsync(ConsumeContext<T> context)
    {
        return ForEachAsync(x => x.PreConsumeAsync(context));
    }

    /// <summary>
    /// Performs the post consume operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostConsumeAsync(ConsumeContext<T> context)
    {
        return ForEachAsync(x => x.PostConsumeAsync(context));
    }

    /// <summary>
    /// Consumes fault.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeFaultAsync(ConsumeContext<T> context, Exception exception)
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, exception));
    }
}
