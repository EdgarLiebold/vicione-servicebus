using System.Security.Cryptography;
using System.Text.Json;
using ViciOne.ServiceBus.BenchmarkConsole;
using ViciOneServiceBusBenchmark.Latency;

string mode = args.Single();
try
{
    int checks = 0;
    if (mode == "deserialization")
    {
        foreach (int size in new[] { 0, 4096, 16384 })
        {
            var benchmark = new DeserializationBenchmark { MessageBufferSize = size };
            benchmark.Setup();
            Require(benchmark.MessagePack_Deserialize(), $"MessagePack did not materialize size {size}");
            Require(benchmark.SystemTextJson_Deserialize(), $"JSON did not materialize size {size}");
            checks += 2;
        }
    }
    else if (mode == "mediator-batches")
    {
        foreach (int size in new[] { 1, 20 })
        {
            var benchmark = new MediatorBatchBenchmark { BatchSize = size };
            benchmark.Setup();
            await benchmark.SendBatchAsync();
            await benchmark.RequestBatchAsync();
            checks += 2;
        }
        var control = new MediatorBenchmark();
        control.Setup();
        await control.CallingHandlerDirectlyAsync();
        await control.CallingHandlerWithViciOneServiceBusMediatorAsync();
        checks += 2;
    }
    else if (mode is "unique" or "duplicate" or "concurrent-duplicate" or "concurrent-unique" or "duplicate-send-observer")
    {
        int count = mode == "concurrent-unique" ? 32 : 2;
        Guid[] ids = Enumerable.Range(1, count)
            .Select(value => new Guid(value, 6209, 26, new byte[] { 128, 0, 0, 0, 0, 0, 0, 1 })).ToArray();
        var capture = new MessageMetricCapture(count);
        var report = (IReportConsumerMetric)capture;
        foreach (Guid id in ids)
        {
            if (mode == "duplicate-send-observer")
                await capture.SentAsync(id, async () =>
                {
                    await capture.PostSendAsync(id);
                    await capture.PostSendAsync(id);
                    Require(!capture.SendCompleted.IsCompleted, "Observer completed the send total before its delegate returned");
                }, true);
            else
                await capture.SentAsync(id, () => Task.CompletedTask);
        }
        Require(capture.SendCompleted.IsCompletedSuccessfully, "Confirmed sends did not complete");
        Require(!capture.ConsumeCompleted.IsCompleted, "Consume completed without a callback");

        if (mode == "concurrent-unique")
            await Concurrently(ids.Select(id => (Func<Task>)(() => report.ConsumedAsync<object>(id))));
        else
        {
            await report.ConsumedAsync<object>(ids[0]);
            MessageMetric first = capture.GetMessageMetrics().Single();
            Require(first.MessageId == ids[0], "First metric belongs to another message");
            Require(!capture.ConsumeCompleted.IsCompleted, "First of two messages completed the consume total");
            if (mode == "duplicate")
                for (int index = 0; index < 8; index++) await report.ConsumedAsync<object>(ids[0]);
            if (mode == "concurrent-duplicate")
                await Concurrently(Enumerable.Range(0, 64).Select(_ => (Func<Task>)(() => report.ConsumedAsync<object>(ids[0]))));
            Require(!capture.ConsumeCompleted.IsCompleted, "Duplicate callbacks completed the unique-message total prematurely");
            MessageMetric[] afterDuplicates = capture.GetMessageMetrics();
            Require(afterDuplicates.Length == 1, "Duplicate callbacks created additional metric rows");
            Require(afterDuplicates[0].ConsumeLatency == first.ConsumeLatency, "Duplicate callback overwrote the first consume timestamp");
            Require(afterDuplicates[0].SendCompletionLatency == first.SendCompletionLatency, "Consume callback changed the send timestamp");
            await report.ConsumedAsync<object>(ids[1]);
        }

        Require(capture.ConsumeCompleted.IsCompletedSuccessfully, "All unique messages did not complete the consume total");
        MessageMetric[] metrics = capture.GetMessageMetrics();
        Require(metrics.Length == count, "Metric count differs from the number of unique messages");
        Require(metrics.Select(value => value.MessageId).Order().SequenceEqual(ids.Order()), "Final metrics do not cover the exact sent identities");
        Require(metrics.GroupBy(value => value.MessageId).All(group => group.Count() == 1), "A sent identity has multiple metrics");
        Require(metrics.All(value => value.SendCompletionLatency >= 0 && value.ConsumeLatency >= value.SendCompletionLatency),
            "Monotonic send/consume ordering was lost");
        checks = count;
    }
    else throw new ArgumentException("Unknown contract mode", nameof(mode));

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        mode, passed = true, checks,
        benchmarkDll = Hash(typeof(MessageMetricCapture).Assembly.Location),
        consoleDll = Hash(typeof(DeserializationBenchmark).Assembly.Location),
        coreDll = Hash(typeof(ViciOne.ServiceBus.IBus).Assembly.Location)
    }));
    return 0;
}
catch (Exception error)
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        mode, passed = false, error = error.GetType().FullName, error.Message,
        parameter = (error as ArgumentException)?.ParamName
    }));
    return 2;
}

static void Require(bool condition, string reason)
{
    if (!condition) throw new InvalidOperationException("Contract assertion: " + reason);
}

static string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();

static async Task Concurrently(IEnumerable<Func<Task>> callbacks)
{
    var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Task[] tasks = callbacks.Select(callback => Task.Run(async () =>
    {
        await start.Task;
        await callback();
    })).ToArray();
    start.SetResult();
    await Task.WhenAll(tasks);
}
