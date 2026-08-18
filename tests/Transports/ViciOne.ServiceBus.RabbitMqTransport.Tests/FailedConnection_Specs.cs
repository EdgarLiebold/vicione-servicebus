namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;
    using TestFramework;


    [TestFixture]
    public class Failing_to_connect_to_rabbitmq :
        AsyncTestFixture
    {
        [Test]
        public async Task Should_fault_nicely()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                x.Host(new Uri("rabbitmq://unknownhost:32787"), h =>
                {
                    h.Username("whocares");
                    h.Password("Ohcrud");
                    h.RequestedConnectionTimeout(2000);
                });

                x.AutoStart = true;
            });

            Assert.ThrowsAsync<RabbitMqConnectionException>(async () =>
            {
                BusHandle handle;
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    handle = await busControl.StartAsync(timeout.Token).OrCanceled(TestCancellationToken);
                }

                await handle.StopAsync(CancellationToken.None);
            });
        }

        [Test]
        public async Task Should_fault_when_credentials_are_bad()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                // Against the pinned fixture with a deliberately wrong secret. Addressing
                // rabbitmq://localhost/ meant addressing whatever held the default port, and the
                // exception could just as well have come from nothing listening there at all.
                x.Host(RunScopedBroker.HostAddress, h =>
                {
                    h.Username(RunScopedBroker.User);
                    h.Password(RunScopedBroker.Pass + "-wrong");
                });
            });

            Assert.That(async () =>
            {
                var handle = await busControl.StartAsync();
                try
                {
                    Console.WriteLine("Waiting for connection...");

                    await handle.Ready;
                }
                finally
                {
                    await handle.StopAsync();
                }
            }, Throws.TypeOf<RabbitMqConnectionException>());
        }

        /// <summary>
        /// Starting and stopping a bus cleanly, against the fixture this run started.
        /// <para>
        /// Five cases stood here and none of them asserted anything. Four addressed
        /// <c>rabbitmq://localhost/</c> or configured no host at all, so they could not reach the
        /// run-scoped fixture and two of them slept thirty and sixty seconds so a human could watch a
        /// connection. What they were about - a bus that comes up and goes down without faulting - is a
        /// product promise, and it is asserted here instead. The fifth wanted a human to crash the
        /// broker mid-run; automating that needs container control from inside a test, so it is removed
        /// rather than pretended, and the reconnect behaviour it gestured at has no coverage claimed for
        /// it here.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_start_and_stop_cleanly_without_a_receive_endpoint()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                x.Host(RunScopedBroker.HostAddress, h =>
                {
                    h.Username(RunScopedBroker.User);
                    h.Password(RunScopedBroker.Pass);
                });
            });

            var handle = await busControl.StartAsync(TestCancellationToken);

            await handle.Ready.OrCanceled(TestCancellationToken);

            Assert.That(handle.Ready.IsCompletedSuccessfully, Is.True, "the bus reported ready without being ready");

            await handle.StopAsync(TestCancellationToken);

            Assert.That(busControl.CheckHealth().Status, Is.EqualTo(BusHealthStatus.Unhealthy),
                "the bus still reports itself as healthy after it was stopped");
        }

        [Test]
        public async Task Should_start_and_stop_cleanly_with_a_receive_endpoint()
        {
            var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                x.Host(RunScopedBroker.HostAddress, h =>
                {
                    h.Username(RunScopedBroker.User);
                    h.Password(RunScopedBroker.Pass);
                });

                x.ReceiveEndpoint("start-stop-queue", e =>
                {
                    e.PurgeOnStartup = true;

                    e.Handler<Test>(_ =>
                    {
                        received.TrySetResult(true);

                        return Task.CompletedTask;
                    });
                });
            });

            var handle = await busControl.StartAsync(TestCancellationToken);
            try
            {
                await handle.Ready.OrCanceled(TestCancellationToken);

                // A started endpoint that cannot receive would still report ready, so the message is
                // what separates a running endpoint from a started one.
                await busControl.Publish<Test>(new TestMessage(), TestCancellationToken);

                await received.Task.OrCanceled(TestCancellationToken);
            }
            finally
            {
                await handle.StopAsync(TestCancellationToken);
            }

            Assert.That(received.Task.IsCompletedSuccessfully, Is.True, "the receive endpoint never delivered");
        }

        public Failing_to_connect_to_rabbitmq()
            : base(new InMemoryTestHarness())
        {
        }


        public interface Test
        {
        }


        public class TestMessage : Test
        {
        }
    }
}
