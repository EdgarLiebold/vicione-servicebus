// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using System;
    using System.Threading.Tasks;
    using Transports;


    public class RabbitMqSendTransportProvider :
        ISendTransportProvider
    {
        readonly IConnectionContextSupervisor _connectionContextSupervisor;
        readonly IChannelContextSupervisor _channelContextSupervisor;
        readonly RabbitMqReceiveEndpointContext _receiveEndpointContext;

        public RabbitMqSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, RabbitMqReceiveEndpointContext receiveEndpointContext)
        {
            _connectionContextSupervisor = connectionContextSupervisor;
            _channelContextSupervisor = receiveEndpointContext.ChannelContextSupervisor;
            _receiveEndpointContext = receiveEndpointContext;
        }

        public Uri NormalizeAddress(Uri address)
        {
            return _connectionContextSupervisor.NormalizeAddress(address);
        }

        public Task<ISendTransport> GetSendTransport(Uri address)
        {
            return _connectionContextSupervisor.CreateSendTransport(_receiveEndpointContext,_channelContextSupervisor, address);
        }
    }
}
