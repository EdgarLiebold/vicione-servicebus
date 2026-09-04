using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a base bus activity indicator connectable implementation.
/// </summary>
public abstract class BaseBusActivityIndicatorConnectable : Connectable<IConditionObserver>,
    IObservableCondition
{
    /// <summary>
    /// Connects condition observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConditionObserver(IConditionObserver observer)
    {
        return Connect(observer);
    }

    /// <summary>
    /// Gets the is met value.
    /// </summary>
    public abstract bool IsMet { get; }

    /// <summary>
    /// Performs the condition updated operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected Task ConditionUpdatedAsync()
    {
        return ForEachAsync(x => x.ConditionUpdatedAsync());
    }
}
