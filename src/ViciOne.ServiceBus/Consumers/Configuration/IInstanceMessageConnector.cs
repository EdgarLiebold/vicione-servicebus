// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public interface IInstanceMessageConnector
    {
        Type MessageType { get; }
    }


    public interface IInstanceMessageConnector<TInstance> :
        IInstanceMessageConnector
        where TInstance : class
    {
        IConsumerMessageSpecification<TInstance> CreateConsumerMessageSpecification();

        ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, TInstance instance, IConsumerSpecification<TInstance> specification);
    }
}
