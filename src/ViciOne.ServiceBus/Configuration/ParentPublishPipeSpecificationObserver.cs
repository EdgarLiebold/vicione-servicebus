using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a parent publish pipe specification observer implementation.
/// </summary>
public class ParentPublishPipeSpecificationObserver :
    IPublishPipeSpecificationObserver
{
    readonly IPublishPipeSpecification _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public ParentPublishPipeSpecificationObserver(IPublishPipeSpecification specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessagePublishPipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

        specification.AddParentMessageSpecification(messageSpecification);
    }
}
