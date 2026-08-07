// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Tests
{
    static class Configuration
    {
        public static string ConsumerGroup = "cg1";

        public static string EventHubNamespace =>
            "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        public static string StorageAccount => "UseDevelopmentStorage=true";
    }
}
