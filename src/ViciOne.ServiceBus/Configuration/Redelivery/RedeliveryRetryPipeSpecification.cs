using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for redelivery retry pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class RedeliveryRetryPipeSpecification<TMessage> :
    ExceptionSpecification,
    IRedeliveryConfigurator,
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RetryObservable _observers;
    readonly IRedeliveryPipeSpecification _redeliveryPipeSpecification;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="redeliveryPipeSpecification">The redelivery pipe specification.</param>
    public RedeliveryRetryPipeSpecification(IRedeliveryPipeSpecification redeliveryPipeSpecification)
    {
        _redeliveryPipeSpecification = redeliveryPipeSpecification;
        _observers = new RetryObservable();
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        var retryPolicy = _policyFactory(Filter);

        var policy = new ConsumeContextRetryPolicy<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(retryPolicy, CancellationToken.None, Factory);

        builder.AddFilter(new RedeliveryRetryFilter<ConsumeContext<TMessage>, TMessage>(policy, _observers));
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
        _policyFactory = factory;
    }

    /// <summary>Connects retry observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Sets whether redelivery replaces the original message identifier.</summary>
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
