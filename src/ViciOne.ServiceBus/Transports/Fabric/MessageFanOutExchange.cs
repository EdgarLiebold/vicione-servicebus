using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

public class MessageFanOutExchange<T> :
    IMessageExchange<T>
    where T : class
{
    readonly Connectable<IMessageSink<T>> _sinks;

    public MessageFanOutExchange(string name)
    {
        Name = name;

        _sinks = new Connectable<IMessageSink<T>>();
    }

    public IEnumerable<IMessageSink<T>> Sinks
    {
        get
        {
            var sinks = new List<IMessageSink<T>>();
            _sinks.ForEach(s => sinks.Add(s));

            return sinks;
        }
    }

    public string Name { get; }

    public Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default)
    {
        return _sinks.ForEachAsync(async sink =>
        {
            if (context.WasAlreadyDelivered(sink))
                return;

            await sink.DeliverAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

            context.Delivered(sink);
        }, cancellationToken: cancellationToken);
    }

    public ConnectHandle Connect(IMessageSink<T> sink, string? routingKey)
    {
        return _sinks.Connect(sink);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "fanOut");

        var sinkScope = scope.CreateScope("sinks");

        _sinks.ForEach(s => s.Probe(sinkScope));
    }

    public override string ToString()
    {
        return $"Exchange({Name})";
    }
}
