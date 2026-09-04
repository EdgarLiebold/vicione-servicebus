using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a consume observable implementation.
/// </summary>
public class ConsumeObservable :
    Connectable<IConsumeObserver>,
    IConsumeObserver
{
    /// <summary>
    /// Performs the pre consume operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PreConsumeAsync(context));
    }

    /// <summary>
    /// Performs the post consume operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostConsumeAsync(context));
    }

    /// <summary>
    /// Consumes fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, exception));
    }
}
