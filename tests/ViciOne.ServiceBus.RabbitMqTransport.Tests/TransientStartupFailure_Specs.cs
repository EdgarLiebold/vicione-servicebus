// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-11.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// A startup failure the transport can still recover from stays inside the transport.
    /// <para>
    /// This is the counterweight to the exclusivity conflict. That one ends the attempt and reaches the
    /// caller; this one must not. Passing the first fault of any kind to whoever is waiting would turn
    /// every recoverable hiccup during startup into a failed connect — and worse, into one that is
    /// reported as failed while the endpoint goes on to come up perfectly well a second later. An API
    /// call cannot be both failed and, without being called again, successful.
    /// </para>
    /// <para>
    /// The recoverable fault here is a declare that disagrees with an existing queue: reply code 406,
    /// the same class the delivery acknowledgement timeout produces. The queue is removed while the
    /// transport is retrying, so the next attempt succeeds — which is precisely the bounded retry and
    /// recovery behaviour that had to stay untouched.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Recovering_from_a_transient_startup_failure
    {
        [Test]
        public async Task Should_not_report_a_recoverable_startup_fault_to_the_caller()
        {
            var faults = new FaultCounter();

            // A regular receive endpoint, not the bus endpoint: the bus endpoint declares its queue
            // exclusively, so a name that already exists is answered with the exclusivity conflict --
            // the terminal case, which is the opposite of what this spec is about.
            var harness = OnOwnVirtualHost(new RabbitMqTestHarness(QueueName));
            harness.OnConfigureRabbitMqReceiveEndpoint += configurator => configurator.ConnectReceiveEndpointObserver(faults);

            await harness.RecreateVirtualHost();

            // The queue exists with an argument the endpoint does not ask for, so the first declare is
            // refused with PRECONDITION_FAILED -- recoverable, and the same class the delivery
            // acknowledgement timeout produces.
            await DeclareConflictingQueue(harness);

            // Removes the obstacle once the transport has really run into it, so a later retry finds a
            // queue it can declare. Started before the start below, because that start blocks.
            var clearing = Task.Run(async () =>
            {
                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
                while (faults.Count == 0 && DateTime.UtcNow < deadline)
                    await Task.Delay(100);

                await DeleteQueue(harness);
            });

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Assert.DoesNotThrowAsync(async () => await harness.Start(),
                    "a startup fault the transport recovers from was reported to the caller as a failed start");

                stopwatch.Stop();

                await clearing;

                Assert.Multiple(() =>
                {
                    Assert.That(faults.Count, Is.GreaterThan(0),
                        "no startup fault occurred at all, so this run says nothing about what happens when one does");
                    Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(55)),
                        $"the endpoint took {stopwatch.Elapsed} to become ready, which is a timeout rather than a recovery");
                });

                TestContext.Out.WriteLine($"recovered after {faults.Count} fault(s) in {stopwatch.Elapsed}");
            }
            finally
            {
                await harness.Stop();
            }
        }

        /// <summary>
        /// The same rule on the path where the caller actually waits.
        /// <para>
        /// The bus endpoint is materialised on demand, and connecting a consumer is what waits for it.
        /// That wait is the only place a startup fault can reach a caller at all, so a spec that only
        /// covers the ordinary endpoint start leaves the rule untested where it matters: an earlier
        /// revision of this fixture did exactly that, and a sabotage that handed every fault to the
        /// caller passed it unnoticed.
        /// </para>
        /// <para>
        /// The obstacle is an exchange of the endpoint's name declared with the wrong type. The queue is
        /// deliberately left alone — the bus endpoint claims its queue exclusively, so a pre-existing
        /// one would produce the terminal conflict instead of a recoverable disagreement.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_not_report_a_recoverable_bus_endpoint_fault_to_the_caller()
        {
            var faults = new FaultCounter();

            var harness = OnOwnVirtualHost(new RabbitMqTestHarness());
            harness.OnConfigureRabbitMqBus += configurator => configurator.OverrideDefaultBusEndpointQueueName(BusEndpointName);

            await harness.RecreateVirtualHost();
            await DeclareConflictingExchange(harness);

            await harness.Start();

            using var observer = harness.Bus.ConnectReceiveEndpointObserver(faults);

            var clearing = Task.Run(async () =>
            {
                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
                while (faults.Count == 0 && DateTime.UtcNow < deadline)
                    await Task.Delay(100);

                await DeleteExchange(harness);
            });

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Assert.DoesNotThrow(() => harness.SubscribeHandler<TestFramework.Messages.PingMessage>(),
                    "a bus endpoint fault the transport recovers from was reported to the caller as a failed connect");

                stopwatch.Stop();

                await clearing;

                Assert.Multiple(() =>
                {
                    Assert.That(faults.Count, Is.GreaterThan(0),
                        "no startup fault occurred at all, so this run says nothing about what happens when one does");
                    Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(55)),
                        $"connecting took {stopwatch.Elapsed}, which is the readiness limit rather than a recovery");
                });

                TestContext.Out.WriteLine($"bus endpoint recovered after {faults.Count} fault(s) in {stopwatch.Elapsed}");
            }
            finally
            {
                await harness.Stop();
            }
        }

        /// <summary>
        /// Each spec here recreates its virtual host, and a recreation removes everything in it. On the
        /// virtual host the rest of the suite uses, that took out buses other fixtures had already
        /// started: measured, five unrelated specs timed out waiting for messages whose queues had been
        /// deleted underneath them. A name of this spec's own keeps the demolition where it belongs.
        /// </summary>
        static RabbitMqTestHarness OnOwnVirtualHost(RabbitMqTestHarness harness)
        {
            var name = $"test-transient-{TestContext.CurrentContext.Test.MethodName}".ToLowerInvariant().Replace("_", "-");

            harness.HostAddress = new UriBuilder(harness.HostAddress) { Path = $"/{name}/" }.Uri;

            return harness;
        }

        const string QueueName = "transient-conflict";
        const string BusEndpointName = "transient-bus-endpoint";

        static async Task DeclareConflictingExchange(RabbitMqTestHarness harness)
        {
            await using var connection = await Connect(harness);
            await using var channel = await connection.CreateChannelAsync();

            // The endpoint declares this name as a fanout exchange; declaring it direct first makes the
            // broker refuse with PRECONDITION_FAILED.
            await channel.ExchangeDeclareAsync(BusEndpointName, ExchangeType.Direct, true, false);
        }

        static async Task DeleteExchange(RabbitMqTestHarness harness)
        {
            await using var connection = await Connect(harness);
            await using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeleteAsync(BusEndpointName);
        }

        static async Task DeclareConflictingQueue(RabbitMqTestHarness harness)
        {
            await using var connection = await Connect(harness);
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(QueueName, true, false, false,
                new Dictionary<string, object> { { "x-message-ttl", 60000 } });
        }

        static async Task DeleteQueue(RabbitMqTestHarness harness)
        {
            await using var connection = await Connect(harness);
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeleteAsync(QueueName);
        }

        static Task<IConnection> Connect(RabbitMqTestHarness harness)
        {
            var factory = new ConnectionFactory
            {
                HostName = harness.HostAddress.Host,
                Port = harness.HostAddress.Port,
                VirtualHost = harness.HostAddress.AbsolutePath.Trim('/'),
                UserName = harness.Username,
                Password = harness.Password
            };

            return factory.CreateConnectionAsync();
        }


        class FaultCounter :
            IReceiveEndpointObserver
        {
            int _count;

            public int Count => Volatile.Read(ref _count);

            public Task Ready(ReceiveEndpointReady ready)
            {
                return Task.CompletedTask;
            }

            public Task Stopping(ReceiveEndpointStopping stopping)
            {
                return Task.CompletedTask;
            }

            public Task Completed(ReceiveEndpointCompleted completed)
            {
                return Task.CompletedTask;
            }

            public Task Faulted(ReceiveEndpointFaulted faulted)
            {
                Interlocked.Increment(ref _count);

                return Task.CompletedTask;
            }
        }
    }
}
