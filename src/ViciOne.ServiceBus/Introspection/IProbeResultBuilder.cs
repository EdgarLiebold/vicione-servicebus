namespace ViciOne.ServiceBus.Introspection;

/// <summary>
/// Defines the contract for probe result builder.
/// </summary>
public interface IProbeResultBuilder
{
    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ProbeResult Build();
}
