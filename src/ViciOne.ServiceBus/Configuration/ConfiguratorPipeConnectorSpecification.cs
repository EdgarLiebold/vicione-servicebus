using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a configurator pipe connector specification implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class ConfiguratorPipeConnectorSpecification<TContext> :
    IPipeConfigurator<TContext>,
    IPipeConnectorSpecification
    where TContext : class, PipeContext
{
    readonly IBuildPipeConfigurator<TContext> _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConfiguratorPipeConnectorSpecification()
    {
        _configurator = new PipeConfigurator<TContext>();
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<TContext> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="connector">The connector value.</param>
    public void Connect(IPipeConnector connector)
    {
        IPipe<TContext> pipe = _configurator.Build();

        connector.ConnectPipe(pipe);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _configurator.Validate();
    }
}
