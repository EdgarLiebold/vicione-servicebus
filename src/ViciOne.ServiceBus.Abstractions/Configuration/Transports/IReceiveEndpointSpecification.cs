using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Specification for configuring a receive endpoint.</summary>
public interface IReceiveEndpointSpecification :
    ISpecification
{
    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Configure(IReceiveEndpointBuilder builder);
}
