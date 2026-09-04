using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Calls the generic version of the IPublishEndpoint.Send method with the object's type
/// </summary>
public interface IConsumeObserverConverter
{
    /// <summary>
    /// Performs the pre consume operation.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PreConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the post consume operation.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PostConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes fault.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ConsumeFaultAsync(IConsumeObserver observer, object context, Exception exception, CancellationToken cancellationToken = default);
}
