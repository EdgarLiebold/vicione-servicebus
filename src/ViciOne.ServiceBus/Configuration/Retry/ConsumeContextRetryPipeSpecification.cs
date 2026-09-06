using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consume context retry pipe.</summary>
public class ConsumeContextRetryPipeSpecification :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<ConsumeContext>
{
    readonly CancellationToken _cancellationToken;
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ConsumeContextRetryPipeSpecification(CancellationToken cancellationToken = default)
    {
        _observers = new RetryObservable();

        _cancellationToken = cancellationToken;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RetryPolicyFactory factory = _policyFactory
            ?? throw new InvalidOperationException("A retry policy must be configured before the specification is applied.");
        IRetryPolicy retryPolicy = factory(Filter)
            ?? throw new InvalidOperationException("The retry policy factory returned null.");

        var contextRetryPolicy = new ConsumeContextRetryPolicy(retryPolicy, _cancellationToken);

        builder.AddFilter(new RetryFilter<ConsumeContext>(contextRetryPolicy, _observers));
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
}


/// <summary>Describes requirements for consume context retry pipe.</summary>
/// <typeparam name="TFilter">The filter type.</typeparam>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ConsumeContextRetryPipeSpecification<TFilter, TContext> :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<TFilter>
    where TFilter : class, PipeContext
    where TContext : RetryConsumeContext, TFilter
{
    readonly CancellationToken _cancellationToken;
    readonly Func<TFilter, IRetryPolicy, RetryContext?, TContext> _contextFactory;
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="contextFactory">The context factory.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ConsumeContextRetryPipeSpecification(Func<TFilter, IRetryPolicy, RetryContext?, TContext> contextFactory,
        CancellationToken cancellationToken = default)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        _observers = new RetryObservable();
        _cancellationToken = cancellationToken;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TFilter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RetryPolicyFactory factory = _policyFactory
            ?? throw new InvalidOperationException("A retry policy must be configured before the specification is applied.");
        IRetryPolicy retryPolicy = factory(Filter)
            ?? throw new InvalidOperationException("The retry policy factory returned null.");

        var contextRetryPolicy = new ConsumeContextRetryPolicy<TFilter, TContext>(retryPolicy, _cancellationToken, _contextFactory);

        builder.AddFilter(new RetryFilter<TFilter>(contextRetryPolicy, _observers));
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
}
