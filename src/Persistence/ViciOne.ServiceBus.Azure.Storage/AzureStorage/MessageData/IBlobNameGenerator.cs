// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureStorage.MessageData
{
    public interface IBlobNameGenerator
    {
        string GenerateBlobName();
    }
}
