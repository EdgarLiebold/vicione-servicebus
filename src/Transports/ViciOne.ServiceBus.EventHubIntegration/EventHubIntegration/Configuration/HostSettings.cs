// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Configuration
{
    using Azure.Core;


    public class HostSettings :
        IHostSettings
    {
        public string ConnectionString { get; set; }
        public string FullyQualifiedNamespace { get; set; }
        public TokenCredential TokenCredential { get; set; }
    }
}
