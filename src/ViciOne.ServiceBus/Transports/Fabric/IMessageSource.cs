// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric
{
    using System.Collections.Generic;


    public interface IMessageSource<T>
        where T : class
    {
        IEnumerable<IMessageSink<T>> Sinks { get; }

        ConnectHandle Connect(IMessageSink<T> sink, string? routingKey);
    }
}
