// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureTable
{
    using Azure.Data.Tables;


    public interface ICloudTableProvider<in TSaga>
        where TSaga : class, ISaga
    {
        TableClient GetCloudTable();
    }
}
