using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// A discovered endpoint QoS declaration used during topology validation.
/// </summary>
public sealed record EndpointQosDeclaration(
    string EndpointName,
    Type ConsumerType,
    EndpointTransportQos Qos,
    EndpointQosOwnership Ownership)
{
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
