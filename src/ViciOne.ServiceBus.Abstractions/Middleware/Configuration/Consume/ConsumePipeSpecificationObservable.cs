using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

public class ConsumePipeSpecificationObservable :
    Connectable<IConsumePipeSpecificationObserver>,
    IConsumePipeSpecificationObserver
{
    public void MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        where T : class
    {
        ForEach(observer => observer.MessageSpecificationCreated(specification));
    }
}
