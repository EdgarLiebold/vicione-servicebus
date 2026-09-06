namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about send pipe specification events.</summary>
public interface ISendPipeSpecificationObserver
{
    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class;
}
