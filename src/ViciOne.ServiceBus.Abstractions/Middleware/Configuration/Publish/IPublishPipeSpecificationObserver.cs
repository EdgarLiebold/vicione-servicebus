namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about publish pipe specification events.</summary>
public interface IPublishPipeSpecificationObserver
{
    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class;
}
