using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the receive pipeline applied to routing-slip messages.</summary>
internal sealed class RoutingSlipConfigurator :
    IRoutingSlipConfigurator,
    IBuildPipeConfigurator<ConsumeContext<RoutingSlip>>
{
    readonly IBuildPipeConfigurator<ConsumeContext<RoutingSlip>> _configurator;

    /// <summary>Creates an empty routing-slip receive-pipeline configurator.</summary>
    public RoutingSlipConfigurator()
    {
        _configurator = new PipeConfigurator<ConsumeContext<RoutingSlip>>();
    }

    /// <summary>Builds the routing-slip receive pipeline from its specifications.</summary>
    /// <returns>The configured routing-slip receive pipeline.</returns>
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

    /// <summary>Adds middleware to the routing-slip receive pipeline.</summary>
    /// <param name="specification">The pipeline specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<RoutingSlip>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
