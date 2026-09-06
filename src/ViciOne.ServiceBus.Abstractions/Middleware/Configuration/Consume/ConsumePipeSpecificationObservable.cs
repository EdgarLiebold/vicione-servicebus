using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for consume pipe specification.</summary>
public class ConsumePipeSpecificationObservable :
    Connectable<IConsumePipeSpecificationObserver>,
    IConsumePipeSpecificationObserver
{
    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        where T : class
    {
        ForEach(observer => observer.MessageSpecificationCreated(specification));
    }
}
