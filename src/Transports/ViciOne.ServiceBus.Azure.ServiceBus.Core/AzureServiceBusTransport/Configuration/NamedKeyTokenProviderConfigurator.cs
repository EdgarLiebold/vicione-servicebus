namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using Azure;


    public class NamedKeyTokenProviderConfigurator :
        IServiceBusNamedKeyTokenProviderConfigurator
    {
        public AzureNamedKeyCredential NamedKeyCredential { get; set; }
    }
}
