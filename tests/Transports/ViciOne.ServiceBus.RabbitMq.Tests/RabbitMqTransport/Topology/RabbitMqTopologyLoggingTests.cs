using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Topology;

public sealed class RabbitMqTopologyLoggingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "diagnostics-report-exchanges-and-exchange-bindings-with-structured-identities")]
    public void LogResult_ReportsExchangeDeclarationsAndExchangeBindingsWithExactBrokerValues()
    {
        var builder = new Builder();
        ExchangeHandle source = builder.ExchangeDeclare("orders-source", "direct", true, false, new Dictionary<string, object?>());
        ExchangeHandle destination = builder.ExchangeDeclare("audit-destination", "fanout", false, true, new Dictionary<string, object?>());
        builder.ExchangeBind(source, destination, "orders.changed", new Dictionary<string, object?>());
        builder.ExchangeBind(destination, source, "audit.replayed", new Dictionary<string, object?>());
        QueueHandle queue = builder.QueueDeclare("orders-queue", true, false, false, new Dictionary<string, object?>());
        builder.QueueBind(source, queue, "queue.route", new Dictionary<string, object?>());
        BrokerTopology topology = builder.BuildBrokerTopology();
        var logger = new RecordingLogger();
        ILogContext? previous = LogContext.Current;

        try
        {
            LogContext.Current = null;
            topology.LogResult();
            Assert.Empty(logger.Entries);

            LogContext.ConfigureCurrentLogContext(logger);
            topology.LogResult();
        }
        finally
        {
            LogContext.Current = previous;
        }

        Assert.Equal(4, logger.Entries.Count);
        Assert.All(logger.Entries, entry => Assert.Equal(LogLevel.Information, entry.Level));

        LogEntry[] exchanges = logger.Entries
            .Where(entry => entry.Template == "Exchange: {ExchangeName}, type: {ExchangeType}, durable: {Durable}, auto-delete: {AutoDelete}")
            .ToArray();
        Assert.Equal(2, exchanges.Length);
        Assert.Equal(
        [
            ("audit-destination", "fanout", false, true),
            ("orders-source", "direct", true, false),
        ], exchanges.Select(entry =>
                (Assert.IsType<string>(entry.Values["ExchangeName"]),
                    Assert.IsType<string>(entry.Values["ExchangeType"]),
                    Assert.IsType<bool>(entry.Values["Durable"]),
                    Assert.IsType<bool>(entry.Values["AutoDelete"])))
            .OrderBy(identity => identity.Item1, StringComparer.Ordinal));
        Assert.All(exchanges, entry => Assert.Equal(5, entry.Values.Count));

        LogEntry[] bindings = logger.Entries.Where(entry =>
            entry.Template == "Binding: source {Source}, destination: {Destination}, routingKey: {RoutingKey}").ToArray();
        Assert.Equal(2, bindings.Length);
        Assert.Equal(
        [
            ("audit-destination", "orders-source", "audit.replayed"),
            ("orders-source", "audit-destination", "orders.changed"),
        ], bindings.Select(entry =>
                (Assert.IsType<string>(entry.Values["Source"]),
                    Assert.IsType<string>(entry.Values["Destination"]),
                    Assert.IsType<string>(entry.Values["RoutingKey"])))
            .OrderBy(identity => identity.Item1, StringComparer.Ordinal));
        Assert.All(bindings, entry => Assert.Equal(4, entry.Values.Count));
    }

    private sealed class Builder : BrokerTopologyBuilder;

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            var values = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            Entries.Add(new LogEntry(logLevel, Assert.IsType<string>(values["{OriginalFormat}"]), values));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Template, Dictionary<string, object?> Values);
}
