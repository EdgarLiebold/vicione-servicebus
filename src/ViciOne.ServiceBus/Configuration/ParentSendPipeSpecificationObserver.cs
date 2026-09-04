using System;

namespace ViciOne.ServiceBus.Configuration;

public class ParentSendPipeSpecificationObserver :
    ISendPipeSpecificationObserver
{
    readonly ISendPipeSpecification _specification;

    public ParentSendPipeSpecificationObserver(ISendPipeSpecification specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessageSendPipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

        specification.AddParentMessageSpecification(messageSpecification);
    }
}
