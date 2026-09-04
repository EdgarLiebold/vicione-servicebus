using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMqTransport.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class RabbitMqErrorTransport :
    RabbitMqMoveTransport<ErrorSettings>,
    IErrorTransport
{
    public RabbitMqErrorTransport(string exchange, ConfigureRabbitMqTopologyFilter<ErrorSettings> topologyFilter)
        : base(exchange, topologyFilter)
    {
    }

    public Task Send(ExceptionReceiveContext context)
    {
        void PreSend(BasicProperties message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);

            message.ClearExpiration();
        }

        return Move(context, PreSend);
    }
}
