using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Coordinates inactivity notifications from one or more observation sources.</summary>
internal interface IInactivityObserver
{
    /// <summary>Registers a newly connected observation source.</summary>
    /// <param name="source">The source to include in inactivity decisions.</param>
    void RegisterSource(IInactivityObservationSource source);

    /// <summary>Handles notification that a source has reached its inactivity interval.</summary>
    /// <param name="cancellationToken">The token used to cancel the notification.</param>
    /// <returns>A task that completes after the inactivity state has been evaluated.</returns>
    Task EvaluateInactivityAsync(CancellationToken cancellationToken = default);

    /// <summary>Completes inactivity observation immediately.</summary>
    void ForceInactive();
}
