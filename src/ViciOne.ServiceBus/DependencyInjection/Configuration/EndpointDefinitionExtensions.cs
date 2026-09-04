using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

public static class EndpointDefinitionExtensions
{
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
