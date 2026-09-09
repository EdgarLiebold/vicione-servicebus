using System;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Describes transport quality-of-service claimed for an endpoint during topology validation.</summary>
/// <param name="EndpointName">The endpoint that owns the transport settings.</param>
/// <param name="ConsumerType">The consumer associated with the declaration.</param>
/// <param name="Qos">The declared transport quality-of-service settings.</param>
/// <param name="Ownership">The configuration boundary that declared the settings.</param>
public sealed record EndpointQosDeclaration(
    string EndpointName,
    Type ConsumerType,
    EndpointTransportQos Qos,
    EndpointQosOwnership Ownership)
{
    /// <summary>Validates the endpoint identity, consumer, ownership, and transport settings.</summary>
    /// <returns>This validated declaration.</returns>
    public EndpointQosDeclaration Validate()
    {
        if (string.IsNullOrWhiteSpace(EndpointName))
            throw new ArgumentException("Endpoint name must not be empty.", nameof(EndpointName));

        ArgumentNullException.ThrowIfNull(ConsumerType);
        ArgumentNullException.ThrowIfNull(Qos);
        if (Ownership is not (EndpointQosOwnership.Endpoint or EndpointQosOwnership.ConsumerDefinition))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Ownership),
                Ownership,
                "Unknown endpoint QoS ownership value.");
        }

        Qos.Validate();
        return this;
    }
}
