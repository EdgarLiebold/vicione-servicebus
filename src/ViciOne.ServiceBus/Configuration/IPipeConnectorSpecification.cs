using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for pipe connector.</summary>
public interface IPipeConnectorSpecification :
    ISpecification
{
    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="connector">The connector.</param>
    void Connect(IPipeConnector connector);
}
