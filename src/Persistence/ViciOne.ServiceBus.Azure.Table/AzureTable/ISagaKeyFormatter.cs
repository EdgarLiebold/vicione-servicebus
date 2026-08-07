// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureTable
{
    using System;


    public interface ISagaKeyFormatter<in TSaga>
        where TSaga : class, ISaga
    {
        (string partitionKey, string rowKey) Format(Guid correlationId);
    }
}
