using Azure.Core;

namespace ViciOne.ServiceBus.EventHubIntegration.Configuration;

public class HostSettings :
    IHostSettings
{
    public string ConnectionString { get; set; }
    public string FullyQualifiedNamespace { get; set; }
    public TokenCredential TokenCredential { get; set; }
}
