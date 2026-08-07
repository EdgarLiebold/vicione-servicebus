// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System.Threading.Tasks;


    /// <summary>
    /// Represents an observer on a change in boolean condition state.
    /// </summary>
    public interface IConditionObserver
    {
        Task ConditionUpdated();
    }
}
