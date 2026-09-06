using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for bus instance.</summary>
public interface IBusInstanceSpecification :
    ISpecification
{
    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="busInstance">The bus instance.</param>
    void Configure(IBusInstance busInstance);
}
