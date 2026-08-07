// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


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
}
