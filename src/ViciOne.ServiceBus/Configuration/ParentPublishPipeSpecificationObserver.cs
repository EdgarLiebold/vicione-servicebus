namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public class ParentPublishPipeSpecificationObserver :
        IPublishPipeSpecificationObserver
    {
        readonly IPublishPipeSpecification _specification;

        public ParentPublishPipeSpecificationObserver(IPublishPipeSpecification specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
        }

        public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(specification);

            IMessagePublishPipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

            specification.AddParentMessageSpecification(messageSpecification);
        }
    }
}
