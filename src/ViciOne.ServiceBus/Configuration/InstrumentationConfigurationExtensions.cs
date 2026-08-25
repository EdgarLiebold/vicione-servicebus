#nullable enable
namespace ViciOne.ServiceBus;

using System;
using Logging;

public static class InstrumentationConfigurationExtensions
{
    /// <summary>
    /// Enables the built-in <see cref="System.Diagnostics.Metrics.Meter" /> instrumentation for
    /// configurations that do not use dependency injection. Applications choose their exporter
    /// through OpenTelemetry; the service bus exposes one stable, bounded telemetry schema.
    /// </summary>
    public static void UseInstrumentation(this IBusFactoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        LogContextInstrumentationExtensions.TryConfigure();
    }
}
