using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusTopologyLoggingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "diagnostics-report-every-topic-and-consumer-subscription-with-structured-identities")]
    public void LogResult_ReportsEachDeclaredTopicAndSubscriptionWithItsBrokerIdentity()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle orders = builder.CreateTopic(new CreateTopicOptions("orders-topic"));
        TopicHandle invoices = builder.CreateTopic(new CreateTopicOptions("invoices-topic"));
        builder.CreateSubscription(orders, new CreateSubscriptionOptions("orders-topic", "payments-worker"), null, null);
        builder.CreateSubscription(invoices, new CreateSubscriptionOptions("invoices-topic", "ledger-worker"), null, null);
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

        LogEntry[] topics = logger.Entries.Where(entry => entry.Template == "Topic: {Topic}").ToArray();
        Assert.Equal(2, topics.Length);
        Assert.Equal(["invoices-topic", "orders-topic"],
            topics.Select(entry => Assert.IsType<string>(entry.Values["Topic"]))
                .OrderBy(name => name, StringComparer.Ordinal));
        Assert.All(topics, entry => Assert.Equal(2, entry.Values.Count));

        LogEntry[] subscriptions = logger.Entries
            .Where(entry => entry.Template == "Subscription: {Subscription}, topic: {Topic}")
            .ToArray();
        Assert.Equal(2, subscriptions.Length);
        Assert.Equal(
        [
            ("ledger-worker", "invoices-topic"),
            ("payments-worker", "orders-topic"),
        ], subscriptions.Select(entry =>
                (Assert.IsType<string>(entry.Values["Subscription"]), Assert.IsType<string>(entry.Values["Topic"])))
            .OrderBy(identity => identity.Item1, StringComparer.Ordinal));
        Assert.All(subscriptions, entry => Assert.Equal(3, entry.Values.Count));
    }

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
