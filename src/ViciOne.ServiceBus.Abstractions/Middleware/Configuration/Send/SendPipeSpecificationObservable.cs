using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

public class SendPipeSpecificationObservable :
    Connectable<ISendPipeSpecificationObserver>,
    ISendPipeSpecificationObserver
{
    public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class
    {
        ForEach(observer => observer.MessageSpecificationCreated(specification));
    }
}
