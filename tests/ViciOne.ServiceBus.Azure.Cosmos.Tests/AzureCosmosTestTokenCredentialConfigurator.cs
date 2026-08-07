// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Azure.Cosmos.Tests
{
    using global::Azure.Identity;


    public class AzureCosmosTestTokenCredentialConfigurator :
        IAzureCosmosTestAuthenticationConfigurator
    {
        public void Configure(ICosmosSagaRepositoryConfigurator configurator)
        {
            configurator.AccountEndpoint = Configuration.AccountEndpoint;
            configurator.TokenCredential = new DefaultAzureCredential();
        }
    }
}
