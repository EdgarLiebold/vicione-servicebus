using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates send pipe configuration.</summary>
public class SendPipeConfiguration :
    ISendPipeConfiguration
{
    readonly SendPipeSpecification _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sendTopology">The send topology.</param>
    public SendPipeConfiguration(ISendTopology sendTopology)
    {
        ArgumentNullException.ThrowIfNull(sendTopology);

        _specification = new SendPipeSpecification();
        _specification.ConnectSendPipeSpecificationObserver(new TopologySendPipeSpecificationObserver(sendTopology));
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="parentSpecification">The parent specification.</param>
    public SendPipeConfiguration(ISendPipeSpecification parentSpecification)
    {
        ArgumentNullException.ThrowIfNull(parentSpecification);

        _specification = new SendPipeSpecification();
        _specification.ConnectSendPipeSpecificationObserver(new ParentSendPipeSpecificationObserver(parentSpecification));
    }

    /// <summary>Gets the specification.</summary>
    public ISendPipeSpecification Specification => _specification;
    /// <summary>Gets the configurator.</summary>
    public ISendPipeConfigurator Configurator => _specification;

    /// <summary>Creates pipe.</summary>
    /// <returns>The created pipe.</returns>
    public ISendPipe CreatePipe()
    {
        return new SendPipe(_specification);
    }
}
