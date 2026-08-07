// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Tests.Contracts
{
    public interface BatchEventHubMessage
    {
        int Index { get; }
    }
}
