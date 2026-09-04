using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a behavior context retry configurator implementation.
/// </summary>
public class BehaviorContextRetryConfigurator :
    ExceptionSpecification,
    IRetryConfigurator
{
    readonly RetryObservable _observers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public BehaviorContextRetryConfigurator()
    {
        _observers = new RetryObservable();
    }

    /// <summary>
    /// Gets or sets the policy factory value.
    /// </summary>
    public RetryPolicyFactory PolicyFactory { get; private set; } = null!;
    /// <summary>
    /// Sets retry policy.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        PolicyFactory = factory;
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
    /// Gets retry policy.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IRetryPolicy GetRetryPolicy()
    {
        return PolicyFactory(Filter);
    }
}
