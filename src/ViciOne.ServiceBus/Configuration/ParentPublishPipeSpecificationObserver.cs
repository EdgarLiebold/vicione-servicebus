using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes parent publish pipe specification events.</summary>
public class ParentPublishPipeSpecificationObserver :
    IPublishPipeSpecificationObserver
{
    readonly IPublishPipeSpecification _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="specification">The specification.</param>
    public ParentPublishPipeSpecificationObserver(IPublishPipeSpecification specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessagePublishPipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

        specification.AddParentMessageSpecification(messageSpecification);
    }
}
