namespace ViciOne.ServiceBus
{
    using Azure;


    public interface ISharedAccessSignatureTokenProviderConfigurator
    {
        AzureSasCredential SasCredential { set; }
    }
}
