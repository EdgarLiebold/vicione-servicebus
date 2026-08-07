// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IConsumePipeSpecificationObserver
    {
        void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
            where T : class;
    }
}
