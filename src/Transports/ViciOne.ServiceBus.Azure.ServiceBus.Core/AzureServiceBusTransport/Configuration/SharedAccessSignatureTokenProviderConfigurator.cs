namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using Azure;


    public class SharedAccessSignatureTokenProviderConfigurator :
        ISharedAccessSignatureTokenProviderConfigurator
    {
        public AzureSasCredential SasCredential { get; set; }
    }
}
