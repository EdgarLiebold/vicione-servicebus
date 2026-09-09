namespace ViciOne.ServiceBus.Configuration;

/// <summary>Validates and applies one transport-specific receive-endpoint configuration concern.</summary>
public interface IReceiveEndpointSpecification :
    ISpecification
{
    /// <summary>Applies the specification to a receive-endpoint builder.</summary>
    /// <param name="builder">The builder that materializes the receive endpoint.</param>
    void Configure(IReceiveEndpointBuilder builder);
}
