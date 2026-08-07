// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class ParentSendPipeSpecificationObserver :
        ISendPipeSpecificationObserver
    {
        readonly ISendPipeSpecification _specification;

        public ParentSendPipeSpecificationObserver(ISendPipeSpecification specification)
        {
            _specification = specification;
        }

        public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
            where T : class
        {
            IMessageSendPipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

            specification.AddParentMessageSpecification(messageSpecification);
        }
    }
}
