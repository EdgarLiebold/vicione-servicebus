using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a redelivery retry pipe specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RedeliveryRetryPipeSpecification<TMessage> :
    ExceptionSpecification,
    IRedeliveryConfigurator,
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RetryObservable _observers;
    readonly IRedeliveryPipeSpecification _redeliveryPipeSpecification;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="redeliveryPipeSpecification">The redelivery pipe specification value.</param>
    public RedeliveryRetryPipeSpecification(IRedeliveryPipeSpecification redeliveryPipeSpecification)
    {
        _redeliveryPipeSpecification = redeliveryPipeSpecification;
        _observers = new RetryObservable();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        var retryPolicy = _policyFactory(Filter);

        var policy = new ConsumeContextRetryPolicy<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(retryPolicy, CancellationToken.None, Factory);

        builder.AddFilter(new RedeliveryRetryFilter<ConsumeContext<TMessage>, TMessage>(policy, _observers));
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

    /// <summary>
    /// Connects retry observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Gets or sets the replace message id value.
    /// </summary>
    public bool ReplaceMessageId
    {
        set
        {
            if (value)
                _redeliveryPipeSpecification.Options |= RedeliveryOptions.ReplaceMessageId;
            else
                _redeliveryPipeSpecification.Options &= ~RedeliveryOptions.ReplaceMessageId;
        }
    }

    static RetryConsumeContext<TMessage> Factory(ConsumeContext<TMessage> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
    {
        return new RedeliveryRetryConsumeContext<TMessage>(context, retryPolicy, retryContext);
    }
}
