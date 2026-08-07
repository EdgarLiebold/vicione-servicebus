// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface ISendPipeSpecificationObserver
    {
        void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
            where T : class;
    }
}
