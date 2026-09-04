using System;

namespace ViciOne.ServiceBus.Configuration;

public interface IMessageInterfaceType
{
    Type MessageType { get; }

    IConsumerMessageConnector<T> GetConsumerConnector<T>()
        where T : class;

    IInstanceMessageConnector<T> GetInstanceConnector<T>()
        where T : class;
}
