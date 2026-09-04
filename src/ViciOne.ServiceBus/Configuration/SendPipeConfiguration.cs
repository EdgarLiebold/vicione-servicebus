using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a send pipe configuration implementation.
/// </summary>
public class SendPipeConfiguration :
    ISendPipeConfiguration
{
    readonly SendPipeSpecification _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sendTopology">The send topology value.</param>
    public SendPipeConfiguration(ISendTopology sendTopology)
    {
        ArgumentNullException.ThrowIfNull(sendTopology);

        _specification = new SendPipeSpecification();
        _specification.ConnectSendPipeSpecificationObserver(new TopologySendPipeSpecificationObserver(sendTopology));
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="parentSpecification">The parent specification value.</param>
    public SendPipeConfiguration(ISendPipeSpecification parentSpecification)
    {
        ArgumentNullException.ThrowIfNull(parentSpecification);

        _specification = new SendPipeSpecification();
        _specification.ConnectSendPipeSpecificationObserver(new ParentSendPipeSpecificationObserver(parentSpecification));
    }

    /// <summary>
    /// Gets the specification value.
    /// </summary>
    public ISendPipeSpecification Specification => _specification;
    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    public ISendPipeConfigurator Configurator => _specification;

    /// <summary>
    /// Creates pipe.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ISendPipe CreatePipe()
    {
        return new SendPipe(_specification);
    }
}
