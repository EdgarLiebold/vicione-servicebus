using System.Collections.Concurrent;
using System.Data;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerConcurrencyTests(ITestOutputHelper output)
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0097", "sqlserver-native-owner")]
    public async Task PartitionedReceive_ThirtyMessagesPreserveOrderWithinBothKeysAtConcurrencyTenAsync()
    {
        const int messageCount = 30;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "partitioned-order",
            cancellationToken);
        string queueName = fixture.Name("partitioned-input");
        var received = new ConcurrentQueue<(string Key, int Index)>();
        var completed = NewObservation();
        int remaining = messageCount;
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = 10;
                endpoint.ConcurrentMessageLimit = 10;
                endpoint.SetReceiveMode(SqlReceiveMode.PartitionedOrdered);
                endpoint.Handler<PartitionedMessage>(context =>
                {
                    string key = context.Advanced().GetPartitionKey()
                        ?? throw new InvalidOperationException("The SQL Server delivery lost its partition key.");
                    received.Enqueue((key, context.Message.Index));
                    if (Interlocked.Decrement(ref remaining) == 0)
                        completed.TrySetResult();
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 1; index <= messageCount; index++)
            {
                string key = (index % 2).ToString();
                await endpoint.SendAsync(
                        new PartitionedMessage(index),
                        context => context.SetPartitionKey(key),
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }

            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(messageCount, received.Count);
        Assert.Equal(Enumerable.Range(1, 15).Select(value => value * 2),
            received.Where(item => item.Key == "0").Select(item => item.Index));
        Assert.Equal(Enumerable.Range(0, 15).Select(value => value * 2 + 1),
            received.Where(item => item.Key == "1").Select(item => item.Index));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0099", "sqlserver-native-owner")]
    public async Task ParallelPublish_OneThousandMessagesFromTenPublishersArriveExactlyOnceAsync()
    {
        const int messageCount = 1000;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "parallel-publish",
            cancellationToken);
        string queueName = fixture.Name("publish-input");
        using var diagnostics = new DiagnosticLogScope();
        Guid[] expected = Enumerable.Range(0, messageCount).Select(_ => Guid.NewGuid()).ToArray();
        var counts = new ConcurrentDictionary<Guid, int>();
        var completed = NewObservation();
        int unique = 0;
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = 30;
                endpoint.Handler<ParallelPublishMessage>(context =>
                {
                    int count = counts.AddOrUpdate(context.Message.Id, 1, static (_, value) => value + 1);
                    if (count == 1 && Interlocked.Increment(ref unique) == messageCount)
                        completed.TrySetResult();
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await Parallel.ForEachAsync(
                expected,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = 10,
                },
                async (id, token) =>
                    await bus.PublishAsync(new ParallelPublishMessage(id), token)
                        .WaitAsync(fixture.OperationTimeout, token));
            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            KeyValuePair<Guid, int>[] snapshot = counts.ToArray();
            Guid[] missing = expected.Except(snapshot.Select(entry => entry.Key)).ToArray();
            output.WriteLine($"Parallel publish timed out: received={snapshot.Length}/{messageCount}, missing={missing.Length}, duplicates={snapshot.Sum(entry => entry.Value - 1)}");
            output.WriteLine($"Missing application IDs: {string.Join(",", missing)}");
            foreach (string entry in diagnostics.Entries)
                output.WriteLine(entry);
            await WriteQueueSnapshotAsync(fixture, queueName);
            throw;
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(expected.Order(), counts.Keys.Order());
        Assert.All(counts, entry => Assert.Equal(1, entry.Value));
    }

    private async Task WriteQueueSnapshotAsync(SqlServerTestDatabase fixture, string queueName)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var connection = fixture.CreateConnection();
            await connection.OpenAsync(timeout.Token);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 5;
            string schema = fixture.Schema.Replace("]", "]]", StringComparison.Ordinal);
            command.CommandText = $"""
                SELECT SYSUTCDATETIME() AS ServerUtc, q.Name, q.Type,
                       COUNT_BIG(d.MessageDeliveryId) AS Deliveries,
                       COALESCE(SUM(CASE WHEN d.LockId IS NOT NULL THEN 1 ELSE 0 END), 0) AS RowsWithLockId,
                       MIN(d.EnqueueTime) AS EarliestEnqueueOrLeaseExpiry,
                       MAX(d.EnqueueTime) AS LatestEnqueueOrLeaseExpiry,
                       MAX(d.DeliveryCount) AS MaximumDeliveryCount
                FROM [{schema}].[Queue] q WITH (READUNCOMMITTED)
                LEFT JOIN [{schema}].[MessageDelivery] d WITH (READUNCOMMITTED) ON d.QueueId = q.Id
                WHERE q.Name = @queueName
                GROUP BY q.Name, q.Type
                ORDER BY q.Type;
                """;
            command.Parameters.Add("@queueName", SqlDbType.NVarChar, 256).Value = queueName;
            await using var reader = await command.ExecuteReaderAsync(timeout.Token);
            output.WriteLine("Queue snapshot (dirty, potentially inconsistent diagnostic read; types 1=normal, 2=error, 3=dead-letter):");
            while (await reader.ReadAsync(timeout.Token))
            {
                output.WriteLine(string.Join("; ", Enumerable.Range(0, reader.FieldCount)
                    .Select(index => $"{reader.GetName(index)}={reader.GetValue(index)}")));
            }
        }
        catch (Exception exception)
        {
            output.WriteLine($"Queue snapshot failed without replacing the original timeout: {exception}");
        }
    }

    private sealed class DiagnosticLogScope : ILogger, IDisposable
    {
        private readonly ILogContext? _previous = LogContext.Current;
        private readonly ConcurrentQueue<string> _entries = new();

        public DiagnosticLogScope() => LogContext.ConfigureCurrentLogContext(this);
        public string[] Entries => _entries.ToArray();
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning && logLevel != LogLevel.None;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            _entries.Enqueue($"{logLevel}: {formatter(state, exception)} {exception}");
            while (_entries.Count > 100)
                _entries.TryDequeue(out _);
        }
        public void Dispose() => LogContext.Current = _previous;
    }

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record PartitionedMessage(int Index);
    private sealed record ParallelPublishMessage(Guid Id);
}
