using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an execute context redelivery pipe specification implementation.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteContextRedeliveryPipeSpecification<TArguments> :
    ExceptionSpecification,
    IRedeliveryConfigurator,
    IPipeSpecification<ExecuteContext<TArguments>>
    where TArguments : class
{
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ExecuteContextRedeliveryPipeSpecification()
    {
        _observers = new RetryObservable();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ExecuteContext<TArguments>> builder)
    {
        var retryPolicy = _policyFactory(Filter);

        var policy = new ConsumeContextRetryPolicy<ExecuteContext<TArguments>, RetryExecuteContext<TArguments>>(retryPolicy, CancellationToken.None,
            Factory);

        builder.AddFilter(new RedeliveryRetryFilter<ExecuteContext<TArguments>, RoutingSlip>(policy, _observers));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_policyFactory == null)
            yield return this.Failure("RetryPolicy", "must not be null");
    }

    /// <summary>
    /// Sets retry policy.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        _policyFactory = factory;
    }

    ConnectHandle IRetryObserverConnector.ConnectRetryObserver(IRetryObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Gets or sets the replace message id value.
    /// </summary>
    public bool ReplaceMessageId { get; set; }

    static RetryExecuteContext<TArguments> Factory(ExecuteContext<TArguments> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
    {
        return new RetryExecuteContext<TArguments>(context, retryPolicy, retryContext);
    }
}
