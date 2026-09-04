using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

public class ExecuteContextRetryPipeSpecification<TArguments> :
    ExceptionSpecification,
    IRetryConfigurator,
    IPipeSpecification<ExecuteContext<TArguments>>
    where TArguments : class
{
    readonly CancellationToken _cancellationToken;
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    public ExecuteContextRetryPipeSpecification(CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        _observers = new RetryObservable();
    }

    public void Apply(IPipeBuilder<ExecuteContext<TArguments>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RetryPolicyFactory factory = _policyFactory
            ?? throw new InvalidOperationException("A retry policy must be configured before the specification is applied.");
        IRetryPolicy retryPolicy = factory(Filter)
            ?? throw new InvalidOperationException("The retry policy factory returned null.");

        var policy = new ConsumeContextRetryPolicy<ExecuteContext<TArguments>, RetryExecuteContext<TArguments>>(retryPolicy, _cancellationToken, Factory);

        builder.AddFilter(new RetryFilter<ExecuteContext<TArguments>>(policy, _observers));
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

    ConnectHandle IRetryObserverConnector.ConnectRetryObserver(IRetryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    static RetryExecuteContext<TArguments> Factory(ExecuteContext<TArguments> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
    {
        return new RetryExecuteContext<TArguments>(context, retryPolicy, retryContext);
    }
}
