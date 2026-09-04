using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Transports.Fabric;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemoryDeliveryContext :
    DeliveryContext<InMemoryTransportMessage>
{
    readonly HashSet<IMessageSink<InMemoryTransportMessage>> _delivered;
    readonly DateTime? _enqueueTime;

    public InMemoryDeliveryContext(InMemoryTransportMessage message, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        Message = message;
        CancellationToken = cancellationToken;
        _enqueueTime = message.Delay.HasValue ? utcNow.UtcDateTime + message.Delay.Value : null;

        _delivered = new HashSet<IMessageSink<InMemoryTransportMessage>>();
    }

    public CancellationToken CancellationToken { get; }

    public InMemoryTransportMessage Message { get; }
    public string? RoutingKey => Message.RoutingKey;
    public DateTime? EnqueueTime => _enqueueTime;
    public long? ReceiverId => default;

    public bool WasAlreadyDelivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        return _delivered.Contains(sink);
    }

    public void Delivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        _delivered.Add(sink);
    }
}
