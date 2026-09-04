using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Specification for configuring a receive endpoint
/// </summary>
public interface IReceiveEndpointSpecification :
    ISpecification
{
    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Configure(IReceiveEndpointBuilder builder);
}
