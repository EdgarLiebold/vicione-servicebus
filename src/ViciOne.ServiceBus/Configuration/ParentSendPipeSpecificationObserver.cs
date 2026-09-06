using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes parent send pipe specification events.</summary>
public class ParentSendPipeSpecificationObserver :
    ISendPipeSpecificationObserver
{
    readonly ISendPipeSpecification _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="specification">The specification.</param>
    public ParentSendPipeSpecificationObserver(ISendPipeSpecification specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessageSendPipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

        specification.AddParentMessageSpecification(messageSpecification);
    }
}
