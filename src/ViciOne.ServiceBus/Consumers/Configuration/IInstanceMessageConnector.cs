using System;

namespace ViciOne.ServiceBus.Configuration;

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
