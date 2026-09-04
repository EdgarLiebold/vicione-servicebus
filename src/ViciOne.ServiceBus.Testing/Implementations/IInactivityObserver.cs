using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Defines the contract for inactivity observer.
/// </summary>
public interface IInactivityObserver
{
    /// <summary>
    /// Connects ed.
    /// </summary>
    /// <param name="source">The source value.</param>
    void Connected(IInactivityObservationSource source);

    /// <summary>
    /// Performs the no activity operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NoActivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the force inactive operation.
    /// </summary>
    void ForceInactive();
}
