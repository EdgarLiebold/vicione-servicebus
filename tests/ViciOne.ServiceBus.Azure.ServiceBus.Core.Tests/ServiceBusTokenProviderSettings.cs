// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests
{
    using global::Azure;
    using global::Azure.Core;


    public interface ServiceBusTokenProviderSettings
    {
        AzureNamedKeyCredential NamedKeyCredential { get; }
        AzureSasCredential SasCredential { get; }
        TokenCredential TokenCredential { get; }
    }
}
