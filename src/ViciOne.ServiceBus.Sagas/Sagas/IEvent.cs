using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Identifies a named event in a state-machine definition.</summary>
public interface IEvent :
    IVisitable,
    IComparable<IEvent>
{
    /// <summary>Gets the event name.</summary>
    string Name { get; }
}


/// <summary>Identifies a state-machine event that carries a message.</summary>
/// <typeparam name="TMessage">The event message type.</typeparam>
public interface IEvent<out TMessage> :
    IEvent
    where TMessage : class
{
}
