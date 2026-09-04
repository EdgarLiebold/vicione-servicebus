using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>
/// A batch of messages which are delivered to a consumer all at once
/// </summary>
/// <typeparam name="T"></typeparam>
public interface Batch<out T> :
    IEnumerable<ConsumeContext<T>>
    where T : class
{
    BatchCompletionMode Mode { get; }

    /// <summary>
    /// When the first message in this batch was received
    /// </summary>
    DateTimeOffset FirstMessageReceived { get; }

    /// <summary>
    /// When the last message in this batch was received
    /// </summary>
    DateTimeOffset LastMessageReceived { get; }

    /// <summary>
    /// Returns the message at the specified index
    /// </summary>
    /// <param name="index"></param>
    ConsumeContext<T> this[int index] { get; }

    /// <summary>
    /// The number of messages in this batch
    /// </summary>
    int Length { get; }
}
