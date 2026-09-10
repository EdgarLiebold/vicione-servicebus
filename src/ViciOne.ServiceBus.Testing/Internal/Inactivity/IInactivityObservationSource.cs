namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Exposes the current activity state and accepts an inactivity observer.</summary>
internal interface IInactivityObservationSource
{
    /// <summary>Gets whether the source is currently inactive.</summary>
    bool IsInactive { get; }

    /// <summary>Connects an observer to this source.</summary>
    /// <param name="observer">The observer to notify when an inactivity interval elapses.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    ConnectHandle ConnectInactivityObserver(IInactivityObserver observer);
}
