namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides tick services.</summary>
public interface ITickProvider
{
    /// <summary>Gets the ticks.</summary>
    long Ticks { get; }
}
