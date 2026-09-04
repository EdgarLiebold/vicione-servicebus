using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Represents an in memory inbox message key value.
/// </summary>
public readonly struct InMemoryInboxMessageKey
{
    /// <summary>
    /// Defines the message id value.
    /// </summary>
    public readonly Guid MessageId;
    /// <summary>
    /// Defines the consumer id value.
    /// </summary>
    public readonly Guid ConsumerId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageId">The message id value.</param>
    /// <param name="consumerId">The consumer id value.</param>
    public InMemoryInboxMessageKey(Guid messageId, Guid consumerId)
    {
        MessageId = messageId;
        ConsumerId = consumerId;
    }


    sealed class EqualityComparer :
        IEqualityComparer<InMemoryInboxMessageKey>
    {
        public bool Equals(InMemoryInboxMessageKey x, InMemoryInboxMessageKey y)
        {
            return x.MessageId.Equals(y.MessageId) && x.ConsumerId.Equals(y.ConsumerId);
        }

        public int GetHashCode(InMemoryInboxMessageKey obj)
        {
            unchecked
            {
                return (obj.MessageId.GetHashCode() * 397) ^ obj.ConsumerId.GetHashCode();
            }
        }
    }


    /// <summary>
    /// Gets the comparer value.
    /// </summary>
    public static IEqualityComparer<InMemoryInboxMessageKey> Comparer { get; } = new EqualityComparer();
}
