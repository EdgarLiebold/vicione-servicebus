using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Represents an observer on a change in boolean condition state.</summary>
public interface IConditionObserver
{
    /// <summary>Reevaluates state after a condition changes.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConditionUpdatedAsync(CancellationToken cancellationToken = default);
}
