namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Defines the contract for inactivity observation source.
/// </summary>
public interface IInactivityObservationSource
{
    /// <summary>
    /// True if the inactivity source is currently inactive
    /// </summary>
    bool IsInactive { get; }

    /// <summary>
    /// Connects inactivity observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectInactivityObserver(IInactivityObserver observer);
}
