// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport
{
    using Configuration;
    using Transports;


    public class InMemoryBusTopology :
        BusTopology,
        IInMemoryBusTopology
    {
        readonly IInMemoryTopologyConfiguration _configuration;

        public InMemoryBusTopology(IInMemoryHostConfiguration hostConfiguration, IInMemoryTopologyConfiguration configuration)
            : base(hostConfiguration, configuration)
        {
            _configuration = configuration;
        }

        public new IInMemoryMessagePublishTopology<T> Publish<T>()
            where T : class
        {
            return _configuration.Publish.GetMessageTopology<T>();
        }
    }
}
