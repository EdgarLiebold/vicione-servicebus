using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Receives notifications about inactivity events.</summary>
public interface IInactivityObserver
{
    /// <summary>Connects ed.</summary>
    /// <param name="source">The source value.</param>
    void Connected(IInactivityObservationSource source);

    /// <summary>Selects a transition without an activity.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NoActivityAsync(CancellationToken cancellationToken = default);

    /// <summary>Forces inactive.</summary>
    void ForceInactive();
}
