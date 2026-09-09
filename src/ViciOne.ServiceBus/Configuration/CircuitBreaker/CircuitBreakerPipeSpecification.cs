using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.CircuitBreaker;

namespace ViciOne.ServiceBus.Configuration;

internal sealed class CircuitBreakerPipeSpecification<T> : IPipeSpecification<T>
    where T : class, PipeContext
{
    private readonly CircuitBreakerSettings _settings;

    public CircuitBreakerPipeSpecification(CircuitBreakerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Apply(IPipeBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFilter(new CircuitBreakerFilter<T>(_settings));
    }

    public IEnumerable<ValidationResult> Validate() => _settings.Validate();
}
