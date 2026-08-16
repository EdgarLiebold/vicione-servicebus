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

        [Test]
        [Explicit("Manual profile: the method asserts nothing, so it can never be a required proof. It sleeps 30 s while a human is expected to crash the broker.")]
        [Category("Manual")]
        public async Task Should_recover_from_a_crashed_server()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                x.ReceiveEndpoint("input-queue", e =>
                {
                });
            });

            var handle = await busControl.StartAsync();
            try
            {
                Console.WriteLine("Waiting for connection...");

                await handle.Ready;

                await Task.Delay(30000);
            }
            finally
            {
                await handle.StopAsync();
            }
        }

        [Test]
        [Explicit("Manual profile: the method asserts nothing, so it can never be a required proof. It deliberately configures no host, so it cannot address the run-scoped fixture.")]
        [Category("Manual")]
        public async Task Should_start_without_any_configuration()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);
            });

            var handle = await busControl.StartAsync(new CancellationTokenSource(5000).Token);
            try
            {
                await handle.Ready;
            }
            finally
            {
                await handle.StopAsync();
            }
        }

        [Test]
        [Explicit("Manual profile: the method asserts nothing, so it can never be a required proof. It deliberately configures no host, so it cannot address the run-scoped fixture.")]
        [Category("Manual")]
        public async Task Should_startup_and_shut_down_cleanly()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x => BusTestFixture.ConfigureBusDiagnostics(x));

            BusHandle handle;
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                handle = await busControl.StartAsync(timeout.Token);
            }

            try
            {
                await handle.Ready;
            }
            finally
            {
                await handle.StopAsync(CancellationToken.None);
            }
        }

        [Test]
        [Explicit("Manual profile: the method asserts nothing, so it can never be a required proof. It sleeps 60 s, which no test budget covers, and observes the connection by eye.")]
        [Category("Manual")]
        public async Task Should_startup_and_shut_down_cleanly_with_an_endpoint()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                x.Host(new Uri("rabbitmq://localhost/"), h =>
                {
                });

                x.ReceiveEndpoint("input_queue", e =>
                {
                    e.Handler<Test>(async context =>
                    {
                    });
                });
            });

            var handle = await busControl.StartAsync(TestCancellationToken);
            try
            {
                Console.WriteLine("Waiting for connection...");

                await handle.Ready;

                await Task.Delay(60000);
            }
            finally
            {
                await handle.StopAsync(TestCancellationToken);
            }
        }

        [Test]
        [Explicit("Manual profile: the method asserts nothing, so it can never be a required proof. It deliberately configures no host, so it cannot address the run-scoped fixture.")]
        [Category("Manual")]
        public async Task Should_startup_and_shut_down_cleanly_with_publish()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                x.Host(new Uri("rabbitmq://localhost/"), h =>
                {
                });
            });

            await busControl.StartAsync();
            try
            {
                await busControl.Publish(new TestMessage());
            }
            finally
            {
                await busControl.StopAsync();
            }
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
