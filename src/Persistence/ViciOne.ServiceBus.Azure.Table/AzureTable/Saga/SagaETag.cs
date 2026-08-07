// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureTable.Saga
{
    public class SagaETag
    {
        public SagaETag(string eTag)
        {
            ETag = eTag;
        }

        public string ETag { get; }
    }
}
