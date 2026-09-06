namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes parent consume pipe specification events.</summary>
public class ParentConsumePipeSpecificationObserver :
    IConsumePipeSpecificationObserver
{
    readonly IConsumePipeSpecification _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="specification">The specification.</param>
    public ParentConsumePipeSpecificationObserver(IConsumePipeSpecification specification)
    {
        _specification = specification;
    }

    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        where T : class
    {
        IMessageConsumePipeSpecification<T> messageSpecification = _specification.GetMessageSpecification<T>();

        specification.AddParentMessageSpecification(messageSpecification);
    }
}
