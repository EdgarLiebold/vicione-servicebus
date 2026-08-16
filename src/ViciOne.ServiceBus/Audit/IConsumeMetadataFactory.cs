namespace ViciOne.ServiceBus.Audit
{
    public interface IConsumeMetadataFactory
    {
        MessageAuditMetadata CreateAuditMetadata<T>(ConsumeContext<T> context)
            where T : class;
    }
}
