namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using ViciOne.ServiceBus.Monitoring;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;


    /// <summary>
    /// Which metric instruments the bus actually emits, read back through an in-process MeterListener.
    /// <para>
    /// The instruments are the subject here, not the delivery. What the product emits was measured
    /// rather than assumed, and the assertions state the measurement: publishing a message increments
    /// the send counter, and no instrument carries a publish specific name. That second fact is why
    /// InstrumentationOptions no longer offers PublishTotal and PublishFaultTotal: no counter was ever
    /// created from them, so setting them configured nothing.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_metrics_the_bus_emits
    {
        [SetUp]
        public void Listen()
        {
            _recorded = new ConcurrentQueue<Measurement>();
            _created = new ConcurrentQueue<string>();

            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name != InstrumentationOptions.MeterName)
                        return;

                    _created.Enqueue(instrument.Name);
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
                _recorded.Enqueue(new Measurement
                {
                    Instrument = instrument.Name,
                    Value = value,
                    Tags = tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value?.ToString())
                }));
            _listener.Start();
        }

        [TearDown]
        public void StopListening()
        {
            _listener?.Dispose();
        }

        ConcurrentQueue<Measurement> _recorded;
        ConcurrentQueue<string> _created;
        MeterListener _listener;

        [Test]
        public async Task Should_count_a_published_message_on_the_send_instrument()
        {
            await Run();

            var send = Single("messaging.vicione-servicebus.send");

            Assert.Multiple(() =>
            {
                Assert.That(send.Value, Is.EqualTo(1), "the send counter did not move by one");
                Assert.That(send.Tags["messaging.vicione-servicebus.message_type"], Is.EqualTo(nameof(MetricMessage)),
                    "the send measurement does not name the message type");
                Assert.That(send.Tags["messaging.vicione-servicebus.destination"], Does.Contain(nameof(MetricMessage)),
                    "the send measurement does not name the destination");
            });
        }

        [Test]
        public async Task Should_count_the_receive_and_the_consume_with_the_endpoint_and_the_consumer()
        {
            await Run();

            var receive = Single("messaging.vicione-servicebus.receive");
            var consume = Single("messaging.vicione-servicebus.consume");

            Assert.Multiple(() =>
            {
                Assert.That(receive.Tags["messaging.vicione-servicebus.destination"], Is.EqualTo("Metric"),
                    "the receive measurement does not name the endpoint that took the message");
                Assert.That(consume.Tags["messaging.vicione-servicebus.consumer_type"], Is.EqualTo(nameof(MetricConsumer)),
                    "the consume measurement does not name the consumer that handled the message");
                Assert.That(consume.Tags["messaging.vicione-servicebus.message_type"], Is.EqualTo(nameof(MetricMessage)),
                    "the consume measurement does not name the message type");
            });
        }

        [Test]
        public async Task Should_not_emit_an_instrument_of_its_own_for_publishing()
        {
            await Run();

            // every instrument the meter creates, not only the ones that fired, so a counter that is
            // merely wired up is enough to fail this
            string[] created = _created.Distinct().ToArray();

            Assert.That(created, Is.Not.Empty, "the meter created no instrument at all, so nothing below is decidable");
            Assert.That(created.Where(name => name.Contains("publish", StringComparison.OrdinalIgnoreCase)), Is.Empty,
                "a publish named instrument exists after all, so a publish metric option would configure something");
        }

        static async Task Run()
        {
            await using var provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<MetricConsumer>();
                    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                })
                .BuildServiceProvider(true);

            var harness = await provider.StartTestHarness();
            try
            {
                await harness.Bus.Publish(new MetricMessage());

                Assert.That(await harness.Consumed.Any<MetricMessage>(), Is.True, "the message was never consumed");
            }
            finally
            {
                await harness.Stop();
            }
        }

        Measurement Single(string instrument)
        {
            List<Measurement> matching = _recorded.Where(measurement => measurement.Instrument == instrument).ToList();

            Assert.That(matching, Is.Not.Empty, $"the instrument '{instrument}' never fired");

            return matching[0];
        }


        class Measurement
        {
            public string Instrument { get; init; }
            public long Value { get; init; }
            public Dictionary<string, string> Tags { get; init; }
        }


        public class MetricMessage
        {
        }


        class MetricConsumer :
            IConsumer<MetricMessage>
        {
            public Task Consume(ConsumeContext<MetricMessage> context)
            {
                return Task.CompletedTask;
            }
        }
    }
}
