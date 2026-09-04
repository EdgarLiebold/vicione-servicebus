namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a parent consume pipe specification observer implementation.
/// </summary>
public class ParentConsumePipeSpecificationObserver :
    IConsumePipeSpecificationObserver
{
    readonly IConsumePipeSpecification _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public ParentConsumePipeSpecificationObserver(IConsumePipeSpecification specification)
    {
        _specification = specification;
    }

    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        where T : class
    {
        IMessageConsumePipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

        specification.AddParentMessageSpecification(messageSpecification);
    }
}
