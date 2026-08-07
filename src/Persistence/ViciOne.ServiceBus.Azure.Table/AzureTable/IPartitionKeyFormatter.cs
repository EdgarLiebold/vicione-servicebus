// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureTable
{
    public interface IPartitionKeyFormatter
    {
        string Format<T>(AuditRecord record)
            where T : class;
    }
}
