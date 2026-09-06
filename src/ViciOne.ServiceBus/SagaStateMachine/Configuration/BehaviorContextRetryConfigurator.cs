using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures behavior context retry.</summary>
public class BehaviorContextRetryConfigurator :
    ExceptionSpecification,
    IRetryConfigurator
{
    readonly RetryObservable _observers;

    /// <summary>Initializes a new instance.</summary>
    public BehaviorContextRetryConfigurator()
    {
        _observers = new RetryObservable();
    }

    /// <summary>Gets or sets the policy factory.</summary>
    public RetryPolicyFactory PolicyFactory { get; private set; } = null!;
    /// <summary>Sets retry policy.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        PolicyFactory = factory;
    }

    /// <summary>Connects retry observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Gets retry policy.</summary>
    /// <returns>The retry policy.</returns>
    public IRetryPolicy GetRetryPolicy()
    {
        return PolicyFactory(Filter);
    }
}
