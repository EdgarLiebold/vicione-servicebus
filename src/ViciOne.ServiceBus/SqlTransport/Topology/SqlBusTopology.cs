using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a sql bus topology implementation.
/// </summary>
public class SqlBusTopology :
    BusTopology,
    ISqlBusTopology
{
    readonly ISqlTopologyConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
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
