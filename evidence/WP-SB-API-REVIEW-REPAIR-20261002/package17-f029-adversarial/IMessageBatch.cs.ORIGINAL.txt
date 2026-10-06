using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the ordered consume contexts delivered to a batch consumer as one unit.</summary>
/// <typeparam name="TMessage">The message contract contained in the batch.</typeparam>
public interface IMessageBatch<out TMessage> :
    IReadOnlyList<ConsumeContext<TMessage>>
    where TMessage : class
{
    /// <summary>Gets the condition that completed the batch.</summary>
    BatchCompletionMode Mode { get; }

    /// <summary>Gets when the first message entered the batch.</summary>
    DateTimeOffset FirstMessageReceived { get; }

    /// <summary>Gets when the last message entered the batch.</summary>
    DateTimeOffset LastMessageReceived { get; }
}
