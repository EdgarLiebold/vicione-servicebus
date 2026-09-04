using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public class SendPipeConfiguration :
    ISendPipeConfiguration
{
    readonly SendPipeSpecification _specification;

    public SendPipeConfiguration(ISendTopology sendTopology)
    {
        ArgumentNullException.ThrowIfNull(sendTopology);

        _specification = new SendPipeSpecification();
        _specification.ConnectSendPipeSpecificationObserver(new TopologySendPipeSpecificationObserver(sendTopology));
    }

    public SendPipeConfiguration(ISendPipeSpecification parentSpecification)
    {
        ArgumentNullException.ThrowIfNull(parentSpecification);

        _specification = new SendPipeSpecification();
        _specification.ConnectSendPipeSpecificationObserver(new ParentSendPipeSpecificationObserver(parentSpecification));
    }

    public ISendPipeSpecification Specification => _specification;
    public ISendPipeConfigurator Configurator => _specification;

    public ISendPipe CreatePipe()
    {
        return new SendPipe(_specification);
    }
}
