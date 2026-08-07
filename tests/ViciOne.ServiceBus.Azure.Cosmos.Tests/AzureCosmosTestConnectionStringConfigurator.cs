// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Azure.Cosmos.Tests
{
    public class AzureCosmosTestConnectionStringConfigurator :
        IAzureCosmosTestAuthenticationConfigurator
    {
        public void Configure(ICosmosSagaRepositoryConfigurator configurator)
        {
            configurator.ConnectionString = Configuration.ConnectionString;
        }
    }
}
