using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a compensate context redelivery pipe specification implementation.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateContextRedeliveryPipeSpecification<TLog> :
    ExceptionSpecification,
    IRedeliveryConfigurator,
    IPipeSpecification<CompensateContext<TLog>>
    where TLog : class
{
    readonly RetryObservable _observers;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public CompensateContextRedeliveryPipeSpecification()
    {
        _observers = new RetryObservable();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<CompensateContext<TLog>> builder)
    {
        var retryPolicy = _policyFactory(Filter);

        var policy = new ConsumeContextRetryPolicy<CompensateContext<TLog>, RetryCompensateContext<TLog>>(retryPolicy, CancellationToken.None, Factory);

        builder.AddFilter(new RedeliveryRetryFilter<CompensateContext<TLog>, RoutingSlip>(policy, _observers));
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

    static RetryCompensateContext<TLog> Factory(CompensateContext<TLog> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
    {
        return new RetryCompensateContext<TLog>(context, retryPolicy, retryContext);
    }
}
