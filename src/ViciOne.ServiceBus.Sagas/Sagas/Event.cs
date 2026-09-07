using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by event.</summary>
public interface Event :
    IVisitable,
    IComparable<Event>
{
    /// <summary>Gets the name.</summary>
    string Name { get; }
}


/// <summary>Defines the operations required by event.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface Event<out TMessage> :
    Event
    where TMessage : class
{
}
