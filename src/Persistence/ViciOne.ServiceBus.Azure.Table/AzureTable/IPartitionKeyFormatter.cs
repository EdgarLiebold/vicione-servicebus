namespace ViciOne.ServiceBus.AzureTable
{
    public interface IPartitionKeyFormatter
    {
        string Format<T>(AuditRecord record)
            where T : class;
    }
}
