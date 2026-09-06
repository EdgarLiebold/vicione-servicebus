namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Defines the operations required by inactivity observation source.</summary>
public interface IInactivityObservationSource
{
    /// <summary>True if the inactivity source is currently inactive.</summary>
    bool IsInactive { get; }

    /// <summary>Connects inactivity observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectInactivityObserver(IInactivityObserver observer);
}
