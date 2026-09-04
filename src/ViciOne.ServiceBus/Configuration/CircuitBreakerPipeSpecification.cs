using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.CircuitBreaker;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

internal sealed class CircuitBreakerPipeSpecification<T>(CircuitBreakerSettings settings) : IPipeSpecification<T>
    where T : class, PipeContext
{
    public void Apply(IPipeBuilder<T> builder) => builder.AddFilter(new CircuitBreakerFilter<T>(settings));

    public IEnumerable<ValidationResult> Validate() => settings.Validate();
}
