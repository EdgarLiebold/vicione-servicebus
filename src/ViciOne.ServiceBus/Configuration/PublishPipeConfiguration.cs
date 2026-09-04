using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public class PublishPipeConfiguration :
    IPublishPipeConfiguration
{
    readonly PublishPipeSpecification _specification;

    public PublishPipeConfiguration(IPublishTopology publishTopology)
    {
        ArgumentNullException.ThrowIfNull(publishTopology);

        _specification = new PublishPipeSpecification();
        _specification.ConnectPublishPipeSpecificationObserver(new TopologyPublishPipeSpecificationObserver(publishTopology));
    }

    public PublishPipeConfiguration(IPublishPipeSpecification parentSpecification)
    {
        ArgumentNullException.ThrowIfNull(parentSpecification);

        _specification = new PublishPipeSpecification();
        _specification.ConnectPublishPipeSpecificationObserver(new ParentPublishPipeSpecificationObserver(parentSpecification));
    }

    public IPublishPipeSpecification Specification => _specification;
    public IPublishPipeConfigurator Configurator => _specification;

    public IPublishPipe CreatePipe()
    {
        return new PublishPipe(_specification);
    }
}
