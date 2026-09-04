using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public class InMemoryTestHarnessRegistrationBusFactory :
    IRegistrationBusFactory
{
    readonly string? _virtualHost;

    public InMemoryTestHarnessRegistrationBusFactory(string? virtualHost = null)
    {
        _virtualHost = virtualHost;
    }

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
