using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for retry pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class RetryPipeSpecification<TContext> :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>Initializes a new instance.</summary>
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

    /// <summary>Sets retry policy.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        _policyFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>Connects retry observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }
}
