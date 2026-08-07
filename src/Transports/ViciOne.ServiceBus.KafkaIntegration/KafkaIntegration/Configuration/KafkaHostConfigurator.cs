// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Configuration
{
    using System;
    using Confluent.Kafka;


    public class KafkaHostConfigurator :
        IKafkaHostConfigurator
    {
        readonly ClientConfig _clientConfig;

        public KafkaHostConfigurator(ClientConfig clientConfig)
        {
            _clientConfig = clientConfig;
        }

        public void UseSsl(Action<IKafkaSslConfigurator> configure)
        {
            var configurator = new KafkaSslConfigurator(_clientConfig);
            configure?.Invoke(configurator);
        }

        public void UseSasl(Action<IKafkaSaslConfigurator> configure)
        {
            var configurator = new KafkaSaslConfigurator(_clientConfig);
            configure?.Invoke(configurator);
        }

        public void CancellationDelay(TimeSpan timeSpan)
        {
            _clientConfig.CancellationDelayMaxMs = (int)timeSpan.TotalMilliseconds;
        }
    }
}
