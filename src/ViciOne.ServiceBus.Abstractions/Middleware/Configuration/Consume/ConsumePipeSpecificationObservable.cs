using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consume pipe specification observable implementation.
/// </summary>
public class ConsumePipeSpecificationObservable :
    Connectable<IConsumePipeSpecificationObserver>,
    IConsumePipeSpecificationObserver
{
    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        where T : class
    {
        ForEach(observer => observer.MessageSpecificationCreated(specification));
    }
}
