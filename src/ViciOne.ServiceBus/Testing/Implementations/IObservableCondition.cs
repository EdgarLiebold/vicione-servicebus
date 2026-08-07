// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    /// <summary>
    /// Represents a boolean condition which may be observed.
    /// </summary>
    public interface IObservableCondition : ICondition
    {
        ConnectHandle ConnectConditionObserver(IConditionObserver observer);
    }
}
