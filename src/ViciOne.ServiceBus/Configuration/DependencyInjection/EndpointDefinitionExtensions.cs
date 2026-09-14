using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Combines endpoint definitions that share a receive endpoint.</summary>
internal static class EndpointDefinitionExtensions
{
    /// <summary>Returns the only definition unchanged or merges multiple definitions into one effective definition.</summary>
    /// <param name="definitions">The definitions assigned to the endpoint.</param>
    /// <param name="context">The registration context used when applying the merged definition.</param>
    /// <param name="endpointName">The endpoint name used to validate combined transport quality-of-service settings.</param>
    /// <returns>The effective definition, or <see langword="null" /> when the sequence is empty.</returns>
    public static IEndpointDefinition? Combine(
        this IEnumerable<IEndpointDefinition> definitions,
        IRegistrationContext context,
        string endpointName)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException("Endpoint name must not be empty.", nameof(endpointName));

        List<IEndpointDefinition> list = definitions.ToList();
        if (list.Any(definition => definition is null))
            throw new ArgumentException("Endpoint definitions must not contain null values.", nameof(definitions));

        if (list.Count == 0)
            return default;

        if (list.Count == 1)
            return list[0];

        return new CombinedEndpointDefinition(list, context, endpointName);
    }
}
