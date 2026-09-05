using System;

namespace ViciOne.ServiceBus.Topology;

sealed class InterfaceMessageCorrelationId<TMessage> :
    IMessageCorrelationId<TMessage>
    where TMessage : class, IMessageCorrelation<Guid>
{
    public bool TryGetCorrelationId(TMessage message, out Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(message);

        correlationId = message.CorrelationId;
        return correlationId != Guid.Empty;
    }
}
