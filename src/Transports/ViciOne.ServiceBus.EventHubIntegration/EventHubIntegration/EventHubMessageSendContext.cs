using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class EventHubMessageSendContext<T> :
    MessageSendContext<T>,
    EventHubSendContext<T>
    where T : class
{
    public EventHubMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    public string PartitionId { get; set; }
    public string PartitionKey { get; set; }
}
