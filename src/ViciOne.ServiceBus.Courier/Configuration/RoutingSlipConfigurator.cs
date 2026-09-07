using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the receive pipeline applied to routing-slip messages.</summary>
internal sealed class RoutingSlipConfigurator :
    IRoutingSlipConfigurator,
    IBuildPipeConfigurator<ConsumeContext<RoutingSlip>>
{
    readonly IBuildPipeConfigurator<ConsumeContext<RoutingSlip>> _configurator;

    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipConfigurator()
    {
        _configurator = new PipeConfigurator<ConsumeContext<RoutingSlip>>();
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    public IPipe<ConsumeContext<RoutingSlip>> Build()
    {
        return _configurator.Build();
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _configurator.Validate();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<RoutingSlip>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
