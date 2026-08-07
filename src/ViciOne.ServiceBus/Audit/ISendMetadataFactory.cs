// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Audit
{
    public interface ISendMetadataFactory
    {
        MessageAuditMetadata CreateAuditMetadata<T>(SendContext<T> context)
            where T : class;

        MessageAuditMetadata CreateAuditMetadata<T>(PublishContext<T> context)
            where T : class;
    }
}
