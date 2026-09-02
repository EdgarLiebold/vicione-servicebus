using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

/// <summary>
/// Serializes every test that installs a process-wide ActivityListener, including test-monitoring
/// helpers whose listener is created indirectly.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OpenTelemetryGlobalCollection
{
    public const string Name = "Process-wide OpenTelemetry listeners";
}
