using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates dependency-injection bus instances backed by an in-memory test harness.</summary>
internal sealed class InMemoryTestHarnessRegistrationBusFactory :
    IRegistrationBusFactory
{
    readonly string? _virtualHost;

    /// <summary>Creates a factory for the optionally isolated in-memory virtual host.</summary>
    /// <param name="virtualHost">An optional transport path segment that isolates the harness.</param>
    public InMemoryTestHarnessRegistrationBusFactory(string? virtualHost = null)
    {
        _virtualHost = virtualHost;
    }

    /// <summary>Creates the configured test-harness bus instance.</summary>
    /// <param name="context">The registration context used to resolve services and configure endpoints.</param>
    /// <param name="specifications">The specifications applied while building the bus.</param>
    /// <param name="busName">The logical registration name of the bus.</param>
    /// <returns>A bus instance owned by the test harness.</returns>
    public IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specifications);
        ArgumentException.ThrowIfNullOrWhiteSpace(busName);

        var timeProvider = context.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        var inMemoryTestHarness = new InMemoryTestHarness(_virtualHost, specifications, timeProvider);

        inMemoryTestHarness.InMemoryBusConfiguring += _ =>
        {
            LogContext.ConfigureCurrentLogContextIfNull(context);
        };
        inMemoryTestHarness.InMemoryBusConfigured += configurator =>
        {
            configurator.ConfigureEndpoints(context);
        };

        return new InMemoryTestHarnessBusInstance(inMemoryTestHarness, context);
    }
}
