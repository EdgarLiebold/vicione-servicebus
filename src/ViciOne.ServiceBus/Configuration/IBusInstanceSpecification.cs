using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for bus instance specification.
/// </summary>
public interface IBusInstanceSpecification :
    ISpecification
{
    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="busInstance">The bus instance value.</param>
    void Configure(IBusInstance busInstance);
}
