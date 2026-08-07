// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public static class AzureCosmosEmulatorConstants
    {
        [Obsolete("Use AccountEndpoint")] public const string EndpointUri = AccountEndpoint;

        [Obsolete("Use AccountKey")] public const string Key = AccountKey;

        public const string AccountEndpoint = "https://localhost:8081/";
        public const string AccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    }
}
