// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    /// <summary>
    /// Represents a boolean condition
    /// </summary>
    public interface ICondition
    {
        bool IsMet { get; }
    }
}
