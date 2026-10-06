using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Settles a received Azure Service Bus message while its delivery lock is held.</summary>
public interface MessageLockContext
{
    /// <summary>Completes the message so it is removed from the entity.</summary>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>A task that completes when the broker accepts settlement.</returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>Abandons the message so Azure Service Bus can make it available for redelivery.</summary>
    /// <param name="exception">The processing failure recorded with the delivery.</param>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>A task that completes when the broker accepts settlement.</returns>
    Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default);
    /// <summary>Moves the message to the entity's dead-letter subqueue.</summary>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>A task that completes when the broker accepts settlement.</returns>
    Task DeadLetterAsync(CancellationToken cancellationToken = default);
    /// <summary>Moves the message to the entity's dead-letter subqueue with failure details.</summary>
    /// <param name="exception">The processing failure recorded as the dead-letter reason.</param>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>A task that completes when the broker accepts settlement.</returns>
    Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default);
}
