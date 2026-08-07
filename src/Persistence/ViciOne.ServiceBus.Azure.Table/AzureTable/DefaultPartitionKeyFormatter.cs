// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureTable
{
    public class DefaultPartitionKeyFormatter :
        IPartitionKeyFormatter
    {
        public string Format<T>(AuditRecord record)
            where T : class
        {
            return record.ContextType;
        }
    }
}
