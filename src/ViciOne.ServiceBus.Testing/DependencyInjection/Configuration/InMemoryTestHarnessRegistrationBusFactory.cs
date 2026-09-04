using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an in memory test harness registration bus factory implementation.
/// </summary>
public class InMemoryTestHarnessRegistrationBusFactory :
    IRegistrationBusFactory
{
    readonly string? _virtualHost;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="virtualHost">The virtual host value.</param>
    public InMemoryTestHarnessRegistrationBusFactory(string? virtualHost = null)
    {
        _virtualHost = virtualHost;
    }

    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="specifications">The specifications value.</param>
    /// <param name="busName">The bus name value.</param>
    /// <returns>The result of the operation.</returns>
    public IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var timeProvider = context.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        var inMemoryTestHarness = new InMemoryTestHarness(_virtualHost, specifications, timeProvider);

        inMemoryTestHarness.OnConfigureInMemoryBus += configurator =>
        {
            LogContext.ConfigureCurrentLogContextIfNull(context);
        };
        inMemoryTestHarness.OnInMemoryBusConfigured += configurator =>
        {
            configurator.ConfigureEndpoints(context);
        };

        return new InMemoryTestHarnessBusInstance(inMemoryTestHarness, context);
    }
}
