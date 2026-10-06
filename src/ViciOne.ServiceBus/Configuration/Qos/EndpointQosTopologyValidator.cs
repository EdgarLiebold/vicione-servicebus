using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;


namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Validates endpoint transport-QoS ownership after consumer topology discovery and before receive endpoints are
/// materialized.
/// </summary>
public sealed class EndpointQosTopologyValidator
{
    /// <summary>Validates the current configuration.</summary>
    /// <param name="declarations">The declarations.</param>
    /// <returns>The effective transport QoS for endpoints with specified QoS, keyed by endpoint name.</returns>
    /// <exception cref="EndpointQosConfigurationException">The declarations contain conflicting QoS or ownership.</exception>
    public FrozenDictionary<string, EndpointTransportQos> Validate(IEnumerable<EndpointQosDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);

        EndpointQosDeclaration[] snapshot = declarations.Select(static declaration =>
        {
            ArgumentNullException.ThrowIfNull(declaration);
            return declaration.Validate();
        }).ToArray();
        Dictionary<string, EndpointTransportQos> result = new(StringComparer.Ordinal);
        List<string> failures = [];

        IEnumerable<IGrouping<string, EndpointQosDeclaration>> endpoints = snapshot
            .GroupBy(static declaration => declaration.EndpointName, StringComparer.Ordinal)
            .OrderBy(static endpoint => endpoint.Key, StringComparer.Ordinal);

        foreach (IGrouping<string, EndpointQosDeclaration> endpoint in endpoints)
        {
            EndpointTransportQos? canonical = ResolveEndpoint(endpoint, failures);

            if (canonical is not null)
                result.Add(endpoint.Key, canonical);
        }

        if (failures.Count > 0)
            throw new EndpointQosConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Aggregate(failures));

        return result.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static EndpointTransportQos? ResolveEndpoint(
        IGrouping<string, EndpointQosDeclaration> endpoint,
        List<string> failures)
    {
        EndpointQosDeclaration[] items = endpoint.ToArray();
        Type[] consumers = items
            .Select(static declaration => declaration.ConsumerType)
            .Distinct()
            .ToArray();
        EndpointQosDeclaration[] endpointOwned = items
            .Where(static declaration => declaration.Ownership == EndpointQosOwnership.Endpoint && declaration.Qos.IsSpecified)
            .ToArray();
        EndpointQosDeclaration[] consumerOwned = items
            .Where(static declaration => declaration.Ownership == EndpointQosOwnership.ConsumerDefinition && declaration.Qos.IsSpecified)
            .ToArray();

        EndpointTransportQos? canonical = TryGetCanonical(endpoint.Key, endpointOwned, failures);

        if (consumerOwned.Length > 0)
        {
            if (consumers.Length > 1)
            {
                string offenders = string.Join(", ", consumerOwned
                    .Select(static declaration => declaration.ConsumerType.FullName ?? declaration.ConsumerType.Name)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal));
                failures.Add(
                    $"Endpoint '{endpoint.Key}' is shared by {consumers.Length} consumers, but endpoint transport QoS was declared "
                    + $"from consumer definition(s): {offenders}. Configure endpoint QoS at the endpoint boundary instead.");
            }
            else
            {
                EndpointTransportQos? legacyCanonical = TryGetCanonical(endpoint.Key, consumerOwned, failures);
                if (canonical is not null && legacyCanonical is not null && canonical != legacyCanonical)
                {
                    failures.Add(
                        $"Endpoint '{endpoint.Key}' has conflicting endpoint-owned and consumer-owned transport QoS declarations.");
                }
                else
                    canonical ??= legacyCanonical;
            }
        }

        return canonical;
    }

    private static EndpointTransportQos? TryGetCanonical(
        string endpointName,
        EndpointQosDeclaration[] declarations,
        List<string> failures)
    {
        if (declarations.Length == 0)
            return null;

        EndpointTransportQos canonical = declarations[0].Qos;
        if (declarations.Any(declaration => declaration.Qos != canonical))
        {
            failures.Add(
                $"Endpoint '{endpointName}' has conflicting transport QoS declarations. Endpoint-level QoS must have exactly one effective value.");
            return null;
        }

        return canonical;
    }
}
