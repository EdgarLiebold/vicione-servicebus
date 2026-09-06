using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for configurator pipe connector.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ConfiguratorPipeConnectorSpecification<TContext> :
    IPipeConfigurator<TContext>,
    IPipeConnectorSpecification
    where TContext : class, PipeContext
{
    readonly IBuildPipeConfigurator<TContext> _configurator;

    /// <summary>Initializes a new instance.</summary>
    public ConfiguratorPipeConnectorSpecification()
    {
        _configurator = new PipeConfigurator<TContext>();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<TContext> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="connector">The connector.</param>
    public void Connect(IPipeConnector connector)
    {
        IPipe<TContext> pipe = _configurator.Build();

        connector.ConnectPipe(pipe);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _configurator.Validate();
    }
}
