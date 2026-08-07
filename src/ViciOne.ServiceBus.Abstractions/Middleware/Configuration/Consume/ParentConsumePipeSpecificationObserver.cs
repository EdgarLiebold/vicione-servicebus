// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class ParentConsumePipeSpecificationObserver :
        IConsumePipeSpecificationObserver
    {
        readonly IConsumePipeSpecification _specification;

        public ParentConsumePipeSpecificationObserver(IConsumePipeSpecification specification)
        {
            _specification = specification;
        }

        public void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
            where T : class
        {
            IMessageConsumePipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

            specification.AddParentMessageSpecification(messageSpecification);
        }
    }
}
