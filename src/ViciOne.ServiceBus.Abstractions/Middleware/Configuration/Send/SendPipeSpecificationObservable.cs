using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for send pipe specification.</summary>
public class SendPipeSpecificationObservable :
    Connectable<ISendPipeSpecificationObserver>,
    ISendPipeSpecificationObserver
{
    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class
    {
        ForEach(observer => observer.MessageSpecificationCreated(specification));
    }
}
