using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemorySendContext<T> :
    MessageSendContext<T>,
    RoutingKeySendContext
    where T : class
{
    public InMemorySendContext(T message, CancellationToken cancellationToken = default)
        : base(message, cancellationToken)
    {
    }

    public string? RoutingKey { get; set; }

    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        RoutingKey = ReadString(properties, PropertyNames.RoutingKey);
    }

    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties[PropertyNames.RoutingKey] = RoutingKey!;
    }


    static class PropertyNames
    {
        public const string RoutingKey = "RoutingKey";
    }
}
