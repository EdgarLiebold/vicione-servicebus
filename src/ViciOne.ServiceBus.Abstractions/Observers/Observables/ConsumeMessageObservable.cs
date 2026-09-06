using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for consume message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ConsumeMessageObservable<T> :
    Connectable<IConsumeMessageObserver<T>>,
    IConsumeMessageObserver<T>
    where T : class
{
    /// <summary>Runs before consume.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreConsumeAsync(ConsumeContext<T> context)
    {
        return ForEachAsync(x => x.PreConsumeAsync(context));
    }

    /// <summary>Runs after consume.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostConsumeAsync(ConsumeContext<T> context)
    {
        return ForEachAsync(x => x.PostConsumeAsync(context));
    }

    /// <summary>Consumes fault.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeFaultAsync(ConsumeContext<T> context, Exception exception)
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, exception));
    }
}
