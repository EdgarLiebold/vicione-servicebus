using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the Latest filter.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class LatestPipeSpecification<T> :
    IPipeSpecification<T>,
    ILatestConfigurator<T>
    where T : class, PipeContext
{
    LatestFilterCreated<T> _created = null!;

    LatestFilterCreated<T> ILatestConfigurator<T>.Created
    {
        set => _created = value;
    }

    void IPipeSpecification<T>.Apply(IPipeBuilder<T> builder)
    {
        var filter = new LatestFilter<T>();
        builder.AddFilter(filter);

        _created?.Invoke(filter);
    }

    IEnumerable<ValidationResult> ISpecification.Validate()
    {
        yield break;
    }
}
