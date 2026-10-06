using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the topology for sql bus.</summary>
public class SqlBusTopology :
    BusTopology,
    ISqlBusTopology
{
    readonly ISqlTopologyConfiguration _configuration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="configuration">The SQL topology configuration exposed by the bus topology.</param>
    public SqlBusTopology(ISqlHostConfiguration hostConfiguration, ISqlTopologyConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _configuration = configuration;
    }

    ISqlPublishTopology ISqlBusTopology.PublishTopology => _configuration.Publish;
    ISqlSendTopology ISqlBusTopology.SendTopology => _configuration.Send;

    ISqlMessagePublishTopology<T> ISqlBusTopology.Publish<T>()
    {
        return _configuration.Publish.GetMessageTopology<T>();
    }

    ISqlMessageSendTopology<T> ISqlBusTopology.Send<T>()
    {
        return _configuration.Send.GetMessageTopology<T>();
    }
}
