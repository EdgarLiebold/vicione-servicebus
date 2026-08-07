// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public interface IMessageInterfaceType
    {
        Type MessageType { get; }

        IConsumerMessageConnector<T> GetConsumerConnector<T>()
            where T : class;

        IInstanceMessageConnector<T> GetInstanceConnector<T>()
            where T : class;
    }
}
