using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a publish pipe specification observable implementation.
/// </summary>
public class PublishPipeSpecificationObservable :
    Connectable<IPublishPipeSpecificationObserver>,
    IPublishPipeSpecificationObserver
{
    /// <summary>
    /// Performs the message specification created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class
    {
        ForEach(observer => observer.MessageSpecificationCreated(specification));
    }
}
