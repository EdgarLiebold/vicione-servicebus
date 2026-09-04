using System;

namespace ViciOne.ServiceBus;

public interface Event :
    IVisitable,
    IComparable<Event>
{
    string Name { get; }
}


public interface Event<out TMessage> :
    Event
    where TMessage : class
{
}
