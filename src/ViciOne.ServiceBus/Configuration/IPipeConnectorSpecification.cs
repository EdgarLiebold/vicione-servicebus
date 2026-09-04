using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for pipe connector specification.
/// </summary>
public interface IPipeConnectorSpecification :
    ISpecification
{
    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="connector">The connector value.</param>
    void Connect(IPipeConnector connector);
}
