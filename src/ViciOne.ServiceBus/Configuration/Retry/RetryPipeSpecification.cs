using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds retry middleware for an arbitrary pipeline context.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class RetryPipeSpecification<TContext> :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly RetryObservable _observers;
    RetryPolicyFactory? _policyFactory;

    /// <summary>Creates an empty retry specification.</summary>
    public RetryPipeSpecification()
    {
        _observers = new RetryObservable();
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RetryPolicyFactory factory = _policyFactory
            ?? throw new InvalidOperationException("A retry policy must be configured before the specification is applied.");
        IRetryPolicy retryPolicy = factory(Filter)
            ?? throw new InvalidOperationException("The retry policy factory returned null.");

        builder.AddFilter(new RetryFilter<TContext>(retryPolicy, _observers));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_policyFactory == null)
            yield return this.Failure("RetryPolicy", "must not be null");
    }

    /// <summary>Sets the factory that combines exception selection with retry timing.</summary>
    /// <param name="factory">The retry-policy factory.</param>
    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        _policyFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>Connects an observer to this retry pipeline.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }
}
