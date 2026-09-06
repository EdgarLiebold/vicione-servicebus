using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates publish pipe configuration.</summary>
public class PublishPipeConfiguration :
    IPublishPipeConfiguration
{
    readonly PublishPipeSpecification _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    public PublishPipeConfiguration(IPublishTopology publishTopology)
    {
        ArgumentNullException.ThrowIfNull(publishTopology);

        _specification = new PublishPipeSpecification();
        _specification.ConnectPublishPipeSpecificationObserver(new TopologyPublishPipeSpecificationObserver(publishTopology));
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="parentSpecification">The parent specification.</param>
    public PublishPipeConfiguration(IPublishPipeSpecification parentSpecification)
    {
        ArgumentNullException.ThrowIfNull(parentSpecification);

        _specification = new PublishPipeSpecification();
        _specification.ConnectPublishPipeSpecificationObserver(new ParentPublishPipeSpecificationObserver(parentSpecification));
    }

    /// <summary>Gets the specification.</summary>
    public IPublishPipeSpecification Specification => _specification;
    /// <summary>Gets the configurator.</summary>
    public IPublishPipeConfigurator Configurator => _specification;

    /// <summary>Creates pipe.</summary>
    /// <returns>The created pipe.</returns>
    public IPublishPipe CreatePipe()
    {
        return new PublishPipe(_specification);
    }
}
