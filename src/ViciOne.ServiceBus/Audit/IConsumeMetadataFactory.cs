// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Audit
{
    public interface IConsumeMetadataFactory
    {
        MessageAuditMetadata CreateAuditMetadata<T>(ConsumeContext<T> context)
            where T : class;
    }
}
