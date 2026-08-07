// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    using System.Collections.Generic;
    using Topology;


    public class InvalidActiveMqConsumeTopologySpecification :
        IActiveMqConsumeTopologySpecification
    {
        readonly string _key;
        readonly string _message;

        public InvalidActiveMqConsumeTopologySpecification(string key, string message)
        {
            _key = key;
            _message = message;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield return this.Failure(_key, _message);
        }

        public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
        {
        }
    }
}
