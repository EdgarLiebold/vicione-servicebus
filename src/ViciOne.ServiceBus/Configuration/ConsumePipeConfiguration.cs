namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates consume pipe configuration.</summary>
public class ConsumePipeConfiguration :
    IConsumePipeConfiguration
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumeTopology">The consume topology.</param>
    public ConsumePipeConfiguration(IConsumeTopology consumeTopology)
    {
        Specification = new ConsumePipeSpecification();
        Specification.ConnectConsumePipeSpecificationObserver(new TopologyConsumePipeSpecificationObserver(consumeTopology));
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="parentSpecification">The parent specification.</param>
    public ConsumePipeConfiguration(IConsumePipeSpecification parentSpecification)
    {
        Specification = parentSpecification.CreateConsumePipeSpecification();
        Specification.ConnectConsumePipeSpecificationObserver(new ParentConsumePipeSpecificationObserver(parentSpecification));
    }

    /// <summary>Gets the specification.</summary>
    public IConsumePipeSpecification Specification { get; }

    /// <summary>Gets the configurator.</summary>
    public IConsumePipeConfigurator Configurator => Specification as IConsumePipeConfigurator
        ?? throw new InvalidOperationException("The consume pipe specification does not expose a configurator.");
}
