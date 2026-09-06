using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates in memory test harness registration bus instances.</summary>
public class InMemoryTestHarnessRegistrationBusFactory :
    IRegistrationBusFactory
{
    readonly string? _virtualHost;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="virtualHost">The virtual host.</param>
    public InMemoryTestHarnessRegistrationBusFactory(string? virtualHost = null)
    {
        _virtualHost = virtualHost;
    }

    /// <summary>Creates bus.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="specifications">The specifications.</param>
    /// <param name="busName">The bus name.</param>
    /// <returns>The created bus.</returns>
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
