namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Provides diagnostic logging for RabbitMQ broker topology.</summary>
public static class TopologyLayoutExtensions
{
    /// <summary>Logs each exchange declaration and exchange-to-exchange binding in a topology.</summary>
    /// <param name="layout">The broker topology to log.</param>
    public static void LogResult(this BrokerTopology layout)
    {
        foreach (var exchange in layout.Exchanges)
        {
            LogContext.Info?.Log("Exchange: {ExchangeName}, type: {ExchangeType}, durable: {Durable}, auto-delete: {AutoDelete}", exchange.ExchangeName,
                exchange.ExchangeType, exchange.Durable, exchange.AutoDelete);
        }

        foreach (var binding in layout.ExchangeBindings)
        {
            LogContext.Info?.Log("Binding: source {Source}, destination: {Destination}, routingKey: {RoutingKey}", binding.Source.ExchangeName,
                binding.Destination.ExchangeName, binding.RoutingKey);
        }
    }
}
