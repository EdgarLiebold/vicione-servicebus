namespace ViciOne.ServiceBus.RabbitMqTransport.Tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Internals;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;


[TestFixture]
public class When_the_consumer_timeout_is_reached_waiting_for_a_batch
{
    /// <summary>
    /// A delivery the consumer holds past the broker's acknowledgement timeout must come back and be
    /// consumed, not be lost and not take effect twice.
    /// <para>
    /// The imported configuration could not express that. It set a batch time limit of 80 s with a
    /// message limit of ten and published six messages, against a delivery acknowledgement timeout of
    /// ten seconds. Six of ten never reaches the message limit, so the batch could only complete on its
    /// time limit, and that is eight times the window the broker allows. Measured: the consumer never
    /// ran once, zero batches were processed, and the broker closed the channel every ten seconds with
    /// 'PRECONDITION_FAILED - delivery acknowledgement on channel 2 timed out. Timeout value used:
    /// 10000 ms.' The messages were redelivered forever and the assertion was unreachable by
    /// construction — for any time limit above the acknowledgement window, not just for eighty seconds.
    /// </para>
    /// <para>
    /// The first correction made the batch assemble but left the assertion where it was, and that
    /// assertion — one consumed message without an exception — was already satisfied by the very first
    /// delivery, the one the broker goes on to discard. It was green without a redelivery ever having
    /// happened. What the spec is named for is therefore asserted explicitly below: the discard, the
    /// second delivery, its acknowledgement, an empty queue afterwards, and an effect that occurred
    /// exactly once.
    /// </para>
    /// </summary>
    [Test]
    public async Task Should_properly_handle_message_redelivery()
    {
        var probe = new RedeliveryProbe();

        RedeliveryConsumer.Probe = probe;

        await using var provider = new ServiceCollection()
            .ConfigureRabbitMqTestOptions(options =>
            {
                options.CleanVirtualHost = true;
                options.CreateVirtualHostIfNotExists = true;
            })
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddOptions<RabbitMqTransportOptions>()
                    .Configure(options =>
                    {
                        options.VHost = VirtualHost;
                        options.ApplyRunScopedCredentials();
                    });

                x.AddOptions<ViciOneServiceBusHostOptions>().Configure(options =>
                {
                    options.StartTimeout = TimeSpan.FromSeconds(5);
                    options.StopTimeout = TimeSpan.FromSeconds(5);
                    options.ConsumerStopTimeout = TimeSpan.FromSeconds(1);
                });

                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(300), testTimeout: TimeSpan.FromSeconds(420));
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<RedeliveryConsumer>(c => c.Options<BatchOptions>(options =>
                    {
                        // Both limits sit inside the acknowledgement window, so the batch is delivered
                        // rather than starved. Three messages are published and the message limit is
                        // three, so the batch completes at once and the acknowledgement timeout is
                        // reached by the consumer holding it — which is what the spec is named for —
                        // instead of by a batch that can never assemble.
                        options.TimeLimit = TimeSpan.FromSeconds(5);
                        options.MessageLimit = MessageCount;
                    }))
                    .Endpoint(e => e.AddConfigureEndpointCallback(cfg =>
                    {
                        if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                            rmq.SetDeliveryAcknowledgementTimeout(ms: AcknowledgementTimeoutMilliseconds);
                    }));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = await provider.StartTestHarness();

        await harness.Bus.PublishBatch(Enumerable.Range(1, MessageCount)
            .Select(index => new TextMessage { Text = $"High Priority {index}", Priority = "High" })
            .ToArray());

        // Point 3: the run is over when a delivery has been processed to the end and acknowledged, not
        // when one has merely started. The first delivery never gets here.
        // Room for two evaluation ticks plus the redelivery: the discard falls on the first tick at
        // which the delivery has been outstanding for the full window, so it lands between one and two
        // minutes after it began.
        var acknowledged = await probe.Acknowledged.Task.OrTimeout(TimeSpan.FromSeconds(210));

        // The queue is read after the harness has come to rest, or ready and unacknowledged would be
        // counted while messages are still legitimately in flight.
        await harness.Stop();

        var queue = await ReadSettledQueueState(harness, TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            // Point 1 -- the first delivery is discarded by the broker rather than completed. The
            // consumer observes it as its consume context being cancelled under it; the broker states
            // the same event in its own log, which the run collects as evidence.
            Assert.That(probe.FirstDeliveryDiscarded, Is.True,
                "the first delivery finished normally, so no acknowledgement timeout was ever reached and the "
                + "spec proves nothing about redelivery");

            // Point 2 -- a real second delivery of the same messages.
            Assert.That(probe.Deliveries, Is.GreaterThanOrEqualTo(2),
                $"only {probe.Deliveries} delivery arrived, so nothing was redelivered");
            Assert.That(probe.DeliveredTexts(2), Is.EquivalentTo(probe.DeliveredTexts(1)),
                "the second delivery did not carry the same messages as the first");

            // Point 3 -- processed and acknowledged.
            // Nothing here counts a fault, and an assertion that the probe recorded none would be green
            // by construction because nothing ever records one. A successful second delivery is asserted
            // by the two lines above and by the effect count below: had it faulted, the acknowledgement
            // would not have been signalled and the effect would not have been applied.
            Assert.That(acknowledged, Is.True);

            // Point 4 -- nothing left behind.
            Assert.That(queue.Ready, Is.Zero, "messages were left ready on the queue");
            Assert.That(queue.Unacknowledged, Is.Zero, "messages were left unacknowledged on the queue");

            // Point 5 -- the effect occurred exactly once per message, counted without deduplication:
            // the discarded delivery must not have taken effect, and the redelivery must not have taken
            // effect twice.
            Assert.That(probe.Effects, Is.EqualTo(MessageCount),
                $"the business effect occurred {probe.Effects} times for {MessageCount} messages");
            Assert.That(probe.EffectsPerMessage.Values, Is.All.EqualTo(1),
                "at least one message took effect more than once, or not at all: "
                + string.Join(", ", probe.EffectsPerMessage.Select(pair => $"{pair.Key}={pair.Value}")));
        });

        TestContext.Out.WriteLine(probe.Report(queue));
    }

    const string VirtualHost = "test-redelivery";
    const int MessageCount = 3;

    /// <summary>
    /// The acknowledgement window the endpoint asks the broker to enforce.
    /// <para>
    /// One minute, because that is the shortest window the pinned broker supports. RabbitMQ documents
    /// the rule for this setting as "Whether the timeout should be enforced is evaluated periodically,
    /// at one minute intervals" and "Values lower than one minute are not supported, and values lower
    /// than five minutes are not recommended" (rabbitmq.com/docs/consumers, pinned image
    /// rabbitmq:4.2-management).
    /// </para>
    /// <para>
    /// A shorter value does not shorten the run and does not hold. With fifteen seconds configured the
    /// fixture shows what the documentation predicts: the broker discards the delivery at its next one
    /// minute evaluation, logging "Timeout used: 15000 ms" at that tick, so the spec would assert a
    /// window the broker never applied. This case takes a minute on purpose — a real integration test
    /// that is slow, not a fast one that proves something else.
    /// </para>
    /// </summary>
    const int AcknowledgementTimeoutMilliseconds = 60000;

    /// <summary>
    /// How long the first delivery is held.
    /// <para>
    /// Far longer than the window, and deliberately so. The broker does not discard at the configured
    /// timeout but at its next evaluation tick, measured at around sixty seconds on the pinned image, so
    /// a hold anywhere near that value is a race: whichever of the two fires first decides whether the
    /// delivery is discarded at all. The hold costs no wall clock time, because the consumer is
    /// cancelled the moment the broker takes the delivery back — it only has to be long enough that the
    /// broker always wins.
    /// </para>
    /// </summary>
    static readonly TimeSpan FirstDeliveryHold = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Asks the broker how many messages are still ready and how many are still unacknowledged, until
    /// the answer settles.
    /// <para>
    /// The management API reports from collected statistics rather than live queue state, and the
    /// collection interval is several seconds. Reading once directly after the stop returned the three
    /// messages that were unacknowledged while the second delivery was still in flight — a stale answer
    /// about a queue that was already empty. The read is therefore repeated until the broker reports
    /// nothing outstanding, and the last answer before the deadline is what gets asserted, so a queue
    /// that genuinely keeps messages still fails.
    /// </para>
    /// </summary>
    static async Task<QueueState> ReadSettledQueueState(ITestHarness harness, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            var state = await ReadQueueState(harness);

            if (state.Ready == 0 && state.Unacknowledged == 0)
                return state;

            if (DateTime.UtcNow >= deadline)
                return state;

            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    static async Task<QueueState> ReadQueueState(ITestHarness harness)
    {
        var options = harness.Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqTransportOptions>>().Value;

        using var client = new HttpClient();
        var credentials = Encoding.ASCII.GetBytes($"{options.User}:{options.Pass}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

        var builder = new UriBuilder(options.UseSsl ? "https" : "http", options.Host, options.ManagementPort,
            $"api/queues/{Uri.EscapeDataString(VirtualHost)}");

        var ready = 0;
        var unacknowledged = 0;
        var names = new List<string>();

        var bytes = await client.GetByteArrayAsync(builder.Uri);

        foreach (var queue in JsonDocument.Parse(bytes).RootElement.EnumerateArray())
        {
            var name = queue.GetProperty("name").GetString();
            if (name == null || name.StartsWith("amq."))
                continue;

            names.Add(name);
            ready += queue.TryGetProperty("messages_ready", out var readyValue) ? readyValue.GetInt32() : 0;
            unacknowledged += queue.TryGetProperty("messages_unacknowledged", out var unackedValue) ? unackedValue.GetInt32() : 0;
        }

        return new QueueState(ready, unacknowledged, names);
    }


    public record QueueState(int Ready, int Unacknowledged, IReadOnlyList<string> Queues);


    public class TextMessage
    {
        public string Text { get; set; }
        public string Priority { get; set; }
    }


    /// <summary>
    /// Records what actually happened, so every one of the five points is read off a counter instead of
    /// being inferred. The consumer is resolved per delivery, so the state cannot live on it.
    /// </summary>
    public class RedeliveryProbe
    {
        readonly ConcurrentDictionary<int, List<string>> _delivered = new();
        readonly ConcurrentDictionary<string, int> _effects = new();
        int _deliveries;
        int _effectCount;

        public TaskCompletionSource<bool> Acknowledged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Deliveries => Volatile.Read(ref _deliveries);
        public int Effects => Volatile.Read(ref _effectCount);
        public bool FirstDeliveryDiscarded { get; set; }
        public IReadOnlyDictionary<string, int> EffectsPerMessage => _effects;

        public int NextDelivery()
        {
            return Interlocked.Increment(ref _deliveries);
        }

        public void Record(int delivery, IEnumerable<string> texts)
        {
            _delivered[delivery] = texts.ToList();
        }

        public IEnumerable<string> DeliveredTexts(int delivery)
        {
            return _delivered.TryGetValue(delivery, out var texts) ? texts : Array.Empty<string>();
        }

        public void Apply(string text)
        {
            _effects.AddOrUpdate(text, 1, (_, count) => count + 1);
            Interlocked.Increment(ref _effectCount);
        }

        public string Report(QueueState queue)
        {
            return $"deliveries={Deliveries} firstDiscarded={FirstDeliveryDiscarded} effects={Effects} "
                + $"perMessage=[{string.Join(", ", _effects.Select(pair => $"{pair.Key}={pair.Value}"))}] "
                + $"ready={queue.Ready} unacked={queue.Unacknowledged} queues=[{string.Join(", ", queue.Queues)}]";
        }
    }


    class RedeliveryConsumer :
        IConsumer<Batch<TextMessage>>
    {
        public static RedeliveryProbe Probe;

        public async Task Consume(ConsumeContext<Batch<TextMessage>> context)
        {
            var probe = Probe;
            var delivery = probe.NextDelivery();

            var texts = context.Message.Select(message => message.Message.Text).ToList();

            probe.Record(delivery, texts);

            LogContext.Debug?.Log("Delivery {Delivery} of {Count} messages", delivery, texts.Count);

            if (delivery == 1)
            {
                // Holding well past the acknowledgement window. The wait is bound to the consume
                // context, so when the broker takes the delivery back and the channel goes down, this
                // returns cancelled — which is the consumer's own observation of the discard, and the
                // reason the effect below is never applied for this delivery.
                try
                {
                    await Task.Delay(FirstDeliveryHold, context.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    probe.FirstDeliveryDiscarded = true;

                    LogContext.Debug?.Log("Delivery {Delivery} was taken back by the broker", delivery);

                    throw;
                }

                // Reached only if the broker never discarded the delivery. The spec then fails on point
                // one, and it must not additionally be credited with an effect it should not have had.
                return;
            }

            foreach (var text in texts)
                probe.Apply(text);

            probe.Acknowledged.TrySetResult(true);
        }
    }
}
