using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Exposes state for event operations.</summary>
public interface EventContext :
    PipeContext
{
    /// <summary>The timestamp at which the command was sent.</summary>
    DateTimeOffset Timestamp { get; }
}


/// <summary>Exposes state for event operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface EventContext<out T> :
    EventContext
    where T : class
{
    /// <summary>The event object.</summary>
    T Event { get; }
}
