using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Represents an observer on a change in boolean condition state.
/// </summary>
public interface IConditionObserver
{
    Task ConditionUpdated();
}
