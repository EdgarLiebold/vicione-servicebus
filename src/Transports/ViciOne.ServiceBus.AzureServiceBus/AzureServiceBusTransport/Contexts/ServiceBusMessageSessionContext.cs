using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Provides session state and lock metadata for an Azure Service Bus session delivery.</summary>
public class ServiceBusMessageSessionContext :
    MessageSessionContext
{
    readonly ProcessSessionMessageEventArgs _session;
    readonly CancellationToken _cancellationToken;

    /// <summary>Initializes the context from a session-processor callback.</summary>
    /// <param name="session">The callback context used for session state operations.</param>
    /// <param name="cancellationToken">The processor callback token passed to SDK operations.</param>
    public ServiceBusMessageSessionContext(ProcessSessionMessageEventArgs session, CancellationToken cancellationToken)
    {
        _session = session;
        _cancellationToken = cancellationToken;
    }

    /// <summary>Reads the current broker-side session state.</summary>
    /// <param name="cancellationToken">The token checked before the read begins.</param>
    /// <returns>A task that produces the session state, or <see langword="null"/> when no state is stored.</returns>
    public Task<BinaryData?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<BinaryData?>(cancellationToken);

        return _session.GetSessionStateAsync(_cancellationToken);
    }

    /// <summary>Replaces or clears the broker-side session state.</summary>
    /// <param name="state">The new state, or <see langword="null"/> to clear it.</param>
    /// <param name="cancellationToken">The token checked before the write begins.</param>
    /// <returns>The Azure SDK operation that persists or clears the accepted session's state.</returns>
    public Task SetStateAsync(BinaryData? state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return _session.SetSessionStateAsync(state, _cancellationToken);
    }

    /// <summary>Performs no explicit renewal because the session processor owns automatic session-lock renewal.</summary>
    /// <param name="message">The session message whose lock needs no independent renewal.</param>
    /// <param name="cancellationToken">The token checked before returning.</param>
    /// <returns>A completed task, or a canceled task when cancellation was already requested.</returns>
    public Task RenewLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>Gets the current message lock expiration in UTC.</summary>
    public DateTimeOffset LockedUntilUtc => _session.Message.LockedUntil.UtcDateTime;

    /// <summary>Gets the accepted Azure Service Bus session identifier.</summary>
    public string SessionId => _session.SessionId;
}
