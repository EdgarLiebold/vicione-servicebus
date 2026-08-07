// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Security.Authentication;
    using RabbitMqTransport.Configuration;


    public class RabbitMqSslOptions
    {
        public string ServerName { get; set; }
        public bool Trust { get; set; }
        public string CertPath { get; set; }
        public string CertPassphrase { get; set; }
        public bool CertIdentity { get; set; }
        public SslProtocols Protocol { get; set; } = ConfigurationHostSettings.DefaultSslProtocols;
    }
}
