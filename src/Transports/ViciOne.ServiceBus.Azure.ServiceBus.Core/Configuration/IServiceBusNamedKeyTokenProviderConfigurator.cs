namespace ViciOne.ServiceBus
{
    using Azure;


    public interface IServiceBusNamedKeyTokenProviderConfigurator
    {
        AzureNamedKeyCredential NamedKeyCredential { set; }
    }
}
