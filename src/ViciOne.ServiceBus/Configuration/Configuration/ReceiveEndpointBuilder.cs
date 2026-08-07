// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class ReceiveEndpointBuilder :
        IReceiveEndpointBuilder
    {
        readonly IReceiveEndpointConfiguration _configuration;

        public ReceiveEndpointBuilder(IReceiveEndpointConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            return ConnectConsumePipe(pipe, ConnectPipeOptions.ConfigureConsumeTopology);
        }

        public virtual ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class
        {
            return _configuration.ConsumePipe.ConnectConsumePipe(pipe);
        }
    }
}
