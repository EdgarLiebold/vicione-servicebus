namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Represents a boolean condition which may be observed.
/// </summary>
public interface IObservableCondition : ICondition
{
    /// <summary>
    /// Connects condition observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConditionObserver(IConditionObserver observer);
}
