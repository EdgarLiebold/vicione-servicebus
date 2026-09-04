using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

public class InMemoryRegistrationBusFactory :
    TransportRegistrationBusFactory<IInMemoryReceiveEndpointConfigurator>
{
    readonly InMemoryBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? _configure = null!;

    public InMemoryRegistrationBusFactory(Uri? baseAddress, Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure)
        : this(new InMemoryBusConfiguration(new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology()), baseAddress), configure)
    {
    }

    InMemoryRegistrationBusFactory(InMemoryBusConfiguration busConfiguration,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure)
        : base(busConfiguration.HostConfiguration)
    {
        _configure = configure;

        _busConfiguration = busConfiguration;
    }

    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new InMemoryBusFactoryConfigurator(_busConfiguration);

        return CreateBus(configurator, context, _configure, specifications);
    }

    protected override IBusInstance CreateBusInstance(IBusControl bus, IHost<IInMemoryReceiveEndpointConfigurator> host,
        IHostConfiguration hostConfiguration, IBusRegistrationContext context)
    {
        return new InMemoryBusInstance(bus, host, hostConfiguration, context);
    }
}
