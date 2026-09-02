using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

/// <summary>
/// Serializes tests that install process-wide OpenTelemetry listeners.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OpenTelemetryGlobalCollection
{
    public const string Name = "Process-wide OpenTelemetry listeners";
}
