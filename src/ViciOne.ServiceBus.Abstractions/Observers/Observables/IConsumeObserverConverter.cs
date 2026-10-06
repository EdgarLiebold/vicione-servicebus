using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Invokes typed consume observer callbacks for a supplied context object.</summary>
public interface IConsumeObserverConverter
{
    /// <summary>Runs before consume.</summary>
    /// <param name="observer">The observer whose matching consume callback is invoked.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    /// <summary>Runs after consume.</summary>
    /// <param name="observer">The observer whose matching consume callback is invoked.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    /// <summary>Consumes fault.</summary>
    /// <param name="observer">The observer whose matching consume callback is invoked.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConsumeFaultAsync(IConsumeObserver observer, object context, Exception exception, CancellationToken cancellationToken = default);
}
