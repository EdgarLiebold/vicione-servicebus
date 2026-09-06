using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Calls the generic version of the IPublishEndpoint.Send method with the object's type.</summary>
public interface IConsumeObserverConverter
{
    /// <summary>Runs before consume.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    /// <summary>Runs after consume.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    /// <summary>Consumes fault.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConsumeFaultAsync(IConsumeObserver observer, object context, Exception exception, CancellationToken cancellationToken = default);
}
