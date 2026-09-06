namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Represents a boolean condition which may be observed.</summary>
public interface IObservableCondition : ICondition
{
    /// <summary>Connects condition observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConditionObserver(IConditionObserver observer);
}
