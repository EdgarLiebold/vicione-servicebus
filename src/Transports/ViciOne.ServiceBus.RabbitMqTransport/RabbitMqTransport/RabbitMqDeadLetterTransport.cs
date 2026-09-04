using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMqTransport.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class RabbitMqDeadLetterTransport :
    RabbitMqMoveTransport<DeadLetterSettings>,
    IDeadLetterTransport
{
    public RabbitMqDeadLetterTransport(string exchange, ConfigureRabbitMqTopologyFilter<DeadLetterSettings> topologyFilter)
        : base(exchange, topologyFilter)
    {
    }

    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(BasicProperties message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
