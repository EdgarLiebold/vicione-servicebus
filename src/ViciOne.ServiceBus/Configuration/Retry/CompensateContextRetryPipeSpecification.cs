using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for compensate context retry pipe.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class CompensateContextRetryPipeSpecification<TLog> :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<CompensateContext<TLog>>
    where TLog : class
{
    readonly CancellationToken _cancellationToken;
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public CompensateContextRetryPipeSpecification(CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        _observers = new RetryObservable();
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<CompensateContext<TLog>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RetryPolicyFactory factory = _policyFactory
            ?? throw new InvalidOperationException("A retry policy must be configured before the specification is applied.");
        IRetryPolicy retryPolicy = factory(Filter)
            ?? throw new InvalidOperationException("The retry policy factory returned null.");

        var policy = new ConsumeContextRetryPolicy<CompensateContext<TLog>, RetryCompensateContext<TLog>>(retryPolicy, _cancellationToken, Factory);

        builder.AddFilter(new RetryFilter<CompensateContext<TLog>>(policy, _observers));
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

    ConnectHandle IRetryObserverConnector.ConnectRetryObserver(IRetryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    static RetryCompensateContext<TLog> Factory(CompensateContext<TLog> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
    {
        return new RetryCompensateContext<TLog>(context, retryPolicy, retryContext);
    }
}
