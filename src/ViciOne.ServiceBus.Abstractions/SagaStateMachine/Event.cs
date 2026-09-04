using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for event.
/// </summary>
public interface Event :
    IVisitable,
    IComparable<Event>
{
    /// <summary>
    /// Gets the name value.
    /// </summary>
    string Name { get; }
}


/// <summary>
/// Defines the contract for event.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface Event<out TMessage> :
    Event
    where TMessage : class
{
}
