using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// A context for a message consumed within a message session
/// </summary>
public interface MessageSessionContext
{
    /// <summary>
    /// The SessionId of the session
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// The session is locked until...
    /// </summary>
    DateTimeOffset LockedUntilUtc { get; }

    /// <summary>
    /// Returns the state as a stream
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<BinaryData?> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the message state from the specified stream
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SetStateAsync(BinaryData? state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews the session lock
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    Task RenewLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default);
}
