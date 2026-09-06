namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about consume pipe specification events.</summary>
public interface IConsumePipeSpecificationObserver
{
    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        where T : class;
}
