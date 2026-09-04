using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a parent send pipe specification observer implementation.
/// </summary>
public class ParentSendPipeSpecificationObserver :
    ISendPipeSpecificationObserver
{
    readonly ISendPipeSpecification _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public ParentSendPipeSpecificationObserver(ISendPipeSpecification specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessageSendPipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

        specification.AddParentMessageSpecification(messageSpecification);
    }
}
