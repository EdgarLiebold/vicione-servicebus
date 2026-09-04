using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

public interface IMessageSource<T>
    where T : class
{
    IEnumerable<IMessageSink<T>> Sinks { get; }

    ConnectHandle Connect(IMessageSink<T> sink, string? routingKey);
}
