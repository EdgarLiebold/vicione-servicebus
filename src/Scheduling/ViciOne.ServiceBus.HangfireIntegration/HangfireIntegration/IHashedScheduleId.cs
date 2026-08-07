// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.HangfireIntegration
{
    public interface IHashedScheduleId
    {
        string? HashId { get; }
    }
}
