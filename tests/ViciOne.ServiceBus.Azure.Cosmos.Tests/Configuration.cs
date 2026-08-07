// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Azure.Cosmos.Tests
{
    using System;
    using NUnit.Framework;


    public static class Configuration
    {
        public static string AccountEndpoint =>
            TestContext.Parameters.Exists("CosmosEndpoint")
                ? TestContext.Parameters.Get("CosmosEndpoint")
                : Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_COSMOS_ENDPOINT")
                ?? AzureCosmosEmulatorConstants.AccountEndpoint;

        public static string AccountKey =>
            TestContext.Parameters.Exists("CosmosKey")
                ? TestContext.Parameters.Get("CosmosKey")
                : Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_COSMOS_KEY")
                ?? AzureCosmosEmulatorConstants.AccountKey;

        public static string ConnectionString => $"AccountEndpoint={AccountEndpoint};AccountKey={AccountKey}";
    }
}
