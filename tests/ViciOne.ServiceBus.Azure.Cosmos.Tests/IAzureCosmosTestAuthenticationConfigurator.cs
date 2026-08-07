// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Azure.Cosmos.Tests
{
    public interface IAzureCosmosTestAuthenticationConfigurator
    {
        void Configure(ICosmosSagaRepositoryConfigurator configurator);
    }
}
