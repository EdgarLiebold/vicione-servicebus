namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consume pipe configuration implementation.
/// </summary>
public class ConsumePipeConfiguration :
    IConsumePipeConfiguration
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumeTopology">The consume topology value.</param>
    public ConsumePipeConfiguration(IConsumeTopology consumeTopology)
    {
        Specification = new ConsumePipeSpecification();
        Specification.ConnectConsumePipeSpecificationObserver(new TopologyConsumePipeSpecificationObserver(consumeTopology));
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="parentSpecification">The parent specification value.</param>
    public ConsumePipeConfiguration(IConsumePipeSpecification parentSpecification)
    {
        Specification = parentSpecification.CreateConsumePipeSpecification();
        Specification.ConnectConsumePipeSpecificationObserver(new ParentConsumePipeSpecificationObserver(parentSpecification));
    }

    /// <summary>
    /// Gets the specification value.
    /// </summary>
    public IConsumePipeSpecification Specification { get; }

    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    public IConsumePipeConfigurator Configurator => Specification as IConsumePipeConfigurator
        ?? throw new InvalidOperationException("The consume pipe specification does not expose a configurator.");
}
