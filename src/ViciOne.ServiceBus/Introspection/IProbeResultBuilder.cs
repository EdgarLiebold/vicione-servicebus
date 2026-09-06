namespace ViciOne.ServiceBus.Introspection;

/// <summary>Builds probe result components.</summary>
public interface IProbeResultBuilder
{
    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    ProbeResult Build();
}
