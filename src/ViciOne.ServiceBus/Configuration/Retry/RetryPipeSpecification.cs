using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

public class RetryPipeSpecification<TContext> :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    public RetryPipeSpecification()
    {
        _observers = new RetryObservable();
    }

    public void Apply(IPipeBuilder<TContext> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RetryPolicyFactory factory = _policyFactory
            ?? throw new InvalidOperationException("A retry policy must be configured before the specification is applied.");
        IRetryPolicy retryPolicy = factory(Filter)
            ?? throw new InvalidOperationException("The retry policy factory returned null.");

        builder.AddFilter(new RetryFilter<TContext>(retryPolicy, _observers));
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (_policyFactory == null)
            yield return this.Failure("RetryPolicy", "must not be null");
    }

    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        _policyFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }
}
