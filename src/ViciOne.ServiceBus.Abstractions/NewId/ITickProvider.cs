namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for tick provider.
/// </summary>
public interface ITickProvider
{
    /// <summary>
    /// Gets the ticks value.
    /// </summary>
    long Ticks { get; }
}
