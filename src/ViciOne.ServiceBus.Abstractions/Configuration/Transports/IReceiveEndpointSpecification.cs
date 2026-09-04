using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus;

/// <summary>
/// Specification for configuring a receive endpoint
/// </summary>
public interface IReceiveEndpointSpecification :
    ISpecification
{
    void Configure(IReceiveEndpointBuilder builder);
}
