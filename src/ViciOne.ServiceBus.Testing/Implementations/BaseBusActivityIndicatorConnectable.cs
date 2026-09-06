using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Connects bus-activity observations to a test-harness condition.</summary>
public abstract class BaseBusActivityIndicatorConnectable : Connectable<IConditionObserver>,
    IObservableCondition
{
    /// <summary>Connects condition observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConditionObserver(IConditionObserver observer)
    {
        return Connect(observer);
    }

    /// <summary>Gets a value indicating whether met.</summary>
    public abstract bool IsMet { get; }

    /// <summary>Reevaluates state after a condition changes.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected Task ConditionUpdatedAsync()
    {
        return ForEachAsync(x => x.ConditionUpdatedAsync());
    }
}
