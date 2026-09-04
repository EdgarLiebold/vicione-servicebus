using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for endpoint definition.
/// </summary>
public static class EndpointDefinitionExtensions
{
    /// <summary>
    /// Performs the combine operation.
    /// </summary>
    /// <param name="definitions">The definitions value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="endpointName">The endpoint name value.</param>
    /// <returns>The result of the operation.</returns>
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
