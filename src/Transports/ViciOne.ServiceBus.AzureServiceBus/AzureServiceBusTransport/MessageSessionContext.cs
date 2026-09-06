using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes Azure Service Bus session state and lock operations during message consumption.</summary>
public interface MessageSessionContext
{
    /// <summary>Gets the session identifier.</summary>
    string SessionId { get; }

    /// <summary>Gets the UTC instant at which the current session lock expires.</summary>
    DateTimeOffset LockedUntilUtc { get; }

    /// <summary>Reads the opaque state stored for the session.</summary>
    /// <param name="cancellationToken">Cancels the broker read.</param>
    /// <returns>A task that produces the state, or <see langword="null"/> when the session has no state.</returns>
    Task<BinaryData?> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces or clears the opaque state stored for the session.</summary>
    /// <param name="state">The new state, or <see langword="null"/> to clear it.</param>
    /// <param name="cancellationToken">Cancels the broker write.</param>
    /// <returns>A task that completes when Azure Service Bus stores the state.</returns>
    Task SetStateAsync(BinaryData? state, CancellationToken cancellationToken = default);

    /// <summary>Requests continued lock ownership while processing a session message.</summary>
    /// <param name="message">The message whose session remains active.</param>
    /// <param name="cancellationToken">Cancels the renewal request.</param>
    /// <returns>The renewal request for the session that owns <paramref name="message"/>.</returns>
    Task RenewLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default);
}
