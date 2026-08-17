namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using ViciOne.ServiceBus.Logging;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;


    /// <summary>
    /// What the bus reports to OpenTelemetry, read back through an in-process ActivityListener.
    /// <para>
    /// The listener is the whole point. A test that publishes a message and then asserts that it was
    /// consumed proves the transport, not the telemetry: it would pass unchanged if every activity,
    /// tag and baggage item disappeared. Here the activities are the subject, and nothing leaves the
    /// process, so there is no exporter, no collector and no port to wait on.
    /// </para>
    /// <para>
    /// One message produces three activities on the product's own source: a producer activity for the
    /// send, and two consumer activities, one for receiving the message on the endpoint and one for
    /// handing it to the consumer.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_telemetry_the_bus_produces
    {
        const string CallerSource = "ViciOne.ServiceBus.Tests.TelemetryCaller";
        const string BaggageKey = "vicione-test-baggage";
        const string BaggageValue = "carried-across-the-message";

        [SetUp]
        public void Listen()
        {
            _recorded = new ConcurrentQueue<Activity>();

            // The caller source is listened to as well, or StartActivity returns null, no activity is
            // current, and the baggage the test sets would never leave the test method.
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DiagnosticHeaders.DefaultListenerName || source.Name == CallerSource,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _recorded.Enqueue(activity)
            };

            ActivitySource.AddActivityListener(_listener);
        }

        [TearDown]
        public void StopListening()
        {
            _listener?.Dispose();
        }

        ConcurrentQueue<Activity> _recorded;
        ActivityListener _listener;

        [Test]
        public async Task Should_report_the_send_and_both_consume_steps_from_its_own_source()
        {
            await Run();

            var send = Single("send");
            var receive = Single("receive");
            var process = Single("process");

            Assert.Multiple(() =>
            {
                Assert.That(send.Source.Name, Is.EqualTo(DiagnosticHeaders.DefaultListenerName),
                    "the send activity does not come from the product's own source");
                Assert.That(receive.Source.Name, Is.EqualTo(DiagnosticHeaders.DefaultListenerName),
                    "the receive activity does not come from the product's own source");
                Assert.That(process.Source.Name, Is.EqualTo(DiagnosticHeaders.DefaultListenerName),
                    "the process activity does not come from the product's own source");

                Assert.That(send.Kind, Is.EqualTo(ActivityKind.Producer), "the send activity is not a producer");
                Assert.That(receive.Kind, Is.EqualTo(ActivityKind.Consumer), "the receive activity is not a consumer");
                Assert.That(process.Kind, Is.EqualTo(ActivityKind.Consumer), "the process activity is not a consumer");
            });
        }

        [Test]
        public async Task Should_keep_the_whole_message_on_the_trace_the_caller_started()
        {
            await Run();

            var caller = _recorded.Single(activity => activity.Source.Name == CallerSource);
            var send = Single("send");
            var receive = Single("receive");
            var process = Single("process");

            Assert.Multiple(() =>
            {
                Assert.That(send.TraceId, Is.EqualTo(caller.TraceId),
                    "the send opened its own trace instead of continuing the caller's");
                Assert.That(receive.TraceId, Is.EqualTo(caller.TraceId),
                    "the trace context did not survive the message");
                Assert.That(process.TraceId, Is.EqualTo(caller.TraceId),
                    "the trace context did not reach the consumer");

                Assert.That(send.ParentSpanId, Is.EqualTo(caller.SpanId), "the send is not a child of the caller");
                Assert.That(receive.ParentSpanId, Is.EqualTo(send.SpanId), "the receive is not a child of the send");
                Assert.That(process.ParentSpanId, Is.EqualTo(receive.SpanId), "the process is not a child of the receive");
            });
        }

        [Test]
        public async Task Should_tag_the_destination_the_transport_and_the_consumer()
        {
            await Run();

            var send = Single("send");
            var receive = Single("receive");
            var process = Single("process");

            Assert.Multiple(() =>
            {
                Assert.That(Tag(send, DiagnosticHeaders.Messaging.System), Is.EqualTo("in-memory"),
                    "the send activity does not name the transport it used");
                // The exact string is the transport's destination, which depends on the topology the
                // endpoint configuration produced and was measured as "bus" here, so only its presence
                // is asserted. Pinning the string would test the endpoint naming, not the telemetry.
                Assert.That(Tag(send, DiagnosticHeaders.Messaging.DestinationName), Is.Not.Null.And.Not.Empty,
                    "the send activity names no destination");
                Assert.That(Tag(receive, DiagnosticHeaders.Messaging.DestinationName), Is.Not.Null.And.Not.Empty,
                    "the receive activity does not name the endpoint that took the message");
                Assert.That(Tag(process, DiagnosticHeaders.ConsumerType), Does.Contain(nameof(TelemetryConsumer)),
                    "the process activity does not name the consumer that handled the message");
            });
        }

        [Test]
        public async Task Should_carry_the_baggage_of_the_caller_into_the_consumer()
        {
            var observed = await Run();

            var process = Single("process");

            Assert.Multiple(() =>
            {
                Assert.That(observed.Baggage, Is.EqualTo(BaggageValue),
                    "the consumer did not see the baggage the caller set");
                Assert.That(process.Baggage.FirstOrDefault(item => item.Key == BaggageKey).Value, Is.EqualTo(BaggageValue),
                    "the process activity does not carry the caller's baggage");
            });
        }

        /// <summary>
        /// Publishes one message inside an activity that carries a baggage item, and returns what the
        /// consumer saw. The outer activity is what gives the send a parent, so the trace the
        /// assertions read is the one an application would produce.
        /// </summary>
        static async Task<Observation> Run()
        {
            var observation = new Observation();

            await using var provider = new ServiceCollection()
                .AddSingleton(observation)
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<TelemetryConsumer>();
                    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                })
                .BuildServiceProvider(true);

            var harness = await provider.StartTestHarness();
            try
            {
                using var source = new ActivitySource(CallerSource);
                using var caller = source.StartActivity("caller", ActivityKind.Internal);

                Assert.That(caller, Is.Not.Null, "the caller activity was not started, so nothing below is decidable");

                caller.AddBaggage(BaggageKey, BaggageValue);

                await harness.Bus.Publish(new TelemetryMessage());

                Assert.That(await harness.Consumed.Any<TelemetryMessage>(), Is.True, "the message was never consumed");
            }
            finally
            {
                await harness.Stop();
            }

            return observation;
        }

        Activity Single(string operation)
        {
            List<Activity> matching = _recorded
                .Where(activity => Tag(activity, DiagnosticHeaders.Messaging.Operation) == operation)
                .ToList();

            Assert.That(matching, Has.Count.EqualTo(1),
                $"expected exactly one activity with messaging operation '{operation}', found {matching.Count}");

            return matching[0];
        }

        static string Tag(Activity activity, string name)
        {
            return activity.TagObjects.FirstOrDefault(tag => tag.Key == name).Value?.ToString();
        }


        public class Observation
        {
            public string Baggage { get; set; }
        }


        public class TelemetryMessage
        {
        }


        class TelemetryConsumer :
            IConsumer<TelemetryMessage>
        {
            readonly Observation _observation;

            public TelemetryConsumer(Observation observation)
            {
                _observation = observation;
            }

            public Task Consume(ConsumeContext<TelemetryMessage> context)
            {
                _observation.Baggage = Activity.Current?.GetBaggageItem(BaggageKey);

                return Task.CompletedTask;
            }
        }
    }
}
