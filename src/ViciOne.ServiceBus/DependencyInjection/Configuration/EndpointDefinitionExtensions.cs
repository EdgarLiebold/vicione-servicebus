using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for endpoint definition.</summary>
public static class EndpointDefinitionExtensions
{
    /// <summary>Combines the supplied values.</summary>
    /// <param name="definitions">The definitions.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="endpointName">The endpoint name.</param>
    /// <returns>The endpoint definition produced by the operation.</returns>
    public static IEndpointDefinition? Combine(
        this IEnumerable<IEndpointDefinition> definitions,
        IRegistrationContext context,
        string endpointName)
    {
        List<IEndpointDefinition> list = definitions.ToList();
        if (list.Count == 0)
            return default;

        if (list.Count == 1)
            return list[0];

        return new CombinedEndpointDefinition(list, context, endpointName);
    }
}
