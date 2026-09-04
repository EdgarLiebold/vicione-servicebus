using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a routing slip configurator implementation.
/// </summary>
public class RoutingSlipConfigurator :
    IRoutingSlipConfigurator,
    IBuildPipeConfigurator<ConsumeContext<RoutingSlip>>
{
    readonly IBuildPipeConfigurator<ConsumeContext<RoutingSlip>> _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipConfigurator()
    {
        _configurator = new PipeConfigurator<ConsumeContext<RoutingSlip>>();
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<RoutingSlip>> Build()
    {
        return _configurator.Build();
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _configurator.Validate();
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<RoutingSlip>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
