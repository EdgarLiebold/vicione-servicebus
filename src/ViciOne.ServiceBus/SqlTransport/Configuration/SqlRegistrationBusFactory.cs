using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql registration bus factory implementation.
/// </summary>
public class SqlRegistrationBusFactory :
    TransportRegistrationBusFactory<ISqlReceiveEndpointConfigurator>
{
    readonly SqlBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="specifications">The specifications value.</param>
    /// <param name="busName">The bus name value.</param>
    /// <returns>The result of the operation.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new SqlBusFactoryConfigurator(_busConfiguration);

        configurator.UseRawJsonSerializer(RawSerializerOptions.CopyHeaders, true);

        // var options = context.GetRequiredService<IOptionsMonitor<DbTransportOptions>>().Get(busName);
        //
        // configurator.Host(options.Host, options.Port, options.VHost, h =>
        // {
        //     if (!string.IsNullOrWhiteSpace(options.User))
        //         h.Username(options.User);
        //
        //     if (!string.IsNullOrWhiteSpace(options.Pass))
        //         h.Password(options.Pass);
        // });

        return CreateBus(configurator, context, _configure, specifications);
    }
}
