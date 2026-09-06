using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Creates sql registration bus instances.</summary>
public class SqlRegistrationBusFactory :
    TransportRegistrationBusFactory<ISqlReceiveEndpointConfigurator>
{
    readonly SqlBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? _configure;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public SqlRegistrationBusFactory(Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure)
        : this(new SqlBusConfiguration(new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology())), configure)
    {
    }

    SqlRegistrationBusFactory(SqlBusConfiguration busConfiguration, Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure)
        : base(busConfiguration.HostConfiguration)
    {
        _configure = configure;

        _busConfiguration = busConfiguration;
    }

    /// <summary>Creates bus.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="specifications">The specifications.</param>
    /// <param name="busName">The bus name.</param>
    /// <returns>The created bus.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new SqlBusFactoryConfigurator(_busConfiguration);

        configurator.UseRawJsonSerializer(RawSerializerOptions.CopyHeaders, true);

        return CreateBus(configurator, context, _configure, specifications);
    }
}
