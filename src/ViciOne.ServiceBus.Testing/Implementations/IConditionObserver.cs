using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Represents an observer on a change in boolean condition state.
/// </summary>
public interface IConditionObserver
{
    /// <summary>
    /// Performs the condition updated operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ConditionUpdatedAsync(CancellationToken cancellationToken = default);
}
