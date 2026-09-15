using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds retry middleware for the untyped consume pipeline.</summary>
internal sealed class ConsumeContextRetryPipeSpecification :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<ConsumeContext>
{
    readonly CancellationToken _cancellationToken;
    readonly RetryObservable _observers;
    RetryPolicyFactory? _policyFactory;

    /// <summary>Creates a consume retry specification.</summary>
    /// <param name="cancellationToken">The token that cancels pending retry delays.</param>
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

    /// <summary>Sets the factory that combines exception selection with retry timing.</summary>
    /// <param name="factory">The retry-policy factory.</param>
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


/// <summary>Builds consume-aware retry middleware for a specialized pipeline contract.</summary>
/// <typeparam name="TFilter">The pipeline contract exposed to the filter.</typeparam>
/// <typeparam name="TContext">The consume-retry context implementation.</typeparam>
internal sealed class ConsumeContextRetryPipeSpecification<TFilter, TContext> :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<TFilter>
    where TFilter : class, PipeContext
    where TContext : RetryConsumeContext, TFilter
{
    readonly CancellationToken _cancellationToken;
    readonly Func<TFilter, IRetryPolicy, RetryContext?, TContext> _contextFactory;
    readonly RetryObservable _observers;
    RetryPolicyFactory? _policyFactory;

    /// <summary>Creates a consume retry specification with its retry-context projection.</summary>
    /// <param name="contextFactory">Creates consume-aware context state for each attempt.</param>
    /// <param name="cancellationToken">The token that cancels pending retry delays.</param>
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

    /// <summary>Sets the factory that combines exception selection with retry timing.</summary>
    /// <param name="factory">The retry-policy factory.</param>
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
