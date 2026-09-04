using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>
/// Defines the contract for event context.
/// </summary>
public interface EventContext :
    PipeContext
{
    /// <summary>
    /// The timestamp at which the command was sent
    /// </summary>
    DateTimeOffset Timestamp { get; }
}


/// <summary>
/// Defines the contract for event context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface EventContext<out T> :
    EventContext
    where T : class
{
    /// <summary>
    /// The event object
    /// </summary>
    T Event { get; }
}
