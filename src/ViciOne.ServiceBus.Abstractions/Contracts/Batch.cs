using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the ordered consume contexts delivered to a batch consumer as one unit.</summary>
/// <typeparam name="T">The message contract type.</typeparam>
public interface Batch<out T> :
    IEnumerable<ConsumeContext<T>>
    where T : class
{
    /// <summary>Gets the condition that completed the batch.</summary>
    BatchCompletionMode Mode { get; }

    /// <summary>Gets when the first message in the batch was received.</summary>
    DateTimeOffset FirstMessageReceived { get; }

    /// <summary>Gets when the last message in the batch was received.</summary>
    DateTimeOffset LastMessageReceived { get; }

    /// <summary>Gets the consume context at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the consume context.</param>
    ConsumeContext<T> this[int index] { get; }

    /// <summary>Gets the number of consume contexts in the batch.</summary>
    int Length { get; }
}
