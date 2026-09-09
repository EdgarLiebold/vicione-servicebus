using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Describes a timestamped event traveling through an event pipeline.</summary>
public interface EventContext :
    PipeContext
{
    /// <summary>Gets the UTC time at which the event context was created.</summary>
    DateTimeOffset Timestamp { get; }
}


/// <summary>Provides the typed payload of a published event.</summary>
/// <typeparam name="TEvent">The event contract type.</typeparam>
public interface EventContext<out TEvent> :
    EventContext
    where TEvent : class
{
    /// <summary>Gets the published event.</summary>
    TEvent Event { get; }
}
