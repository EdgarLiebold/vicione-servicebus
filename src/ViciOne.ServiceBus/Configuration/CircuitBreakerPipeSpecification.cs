#nullable enable
namespace ViciOne.ServiceBus.Configuration;

using System.Collections.Generic;
using Middleware;
using Middleware.CircuitBreaker;

internal sealed class CircuitBreakerPipeSpecification<T>(CircuitBreakerSettings settings) : IPipeSpecification<T>
    where T : class, PipeContext
{
    public void Apply(IPipeBuilder<T> builder) => builder.AddFilter(new CircuitBreakerFilter<T>(settings));

    public IEnumerable<ValidationResult> Validate() => settings.Validate();
}
