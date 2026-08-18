namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using HarnessContracts;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Testing;


    /// <summary>
    /// A virtual host that could not be prepared is an error the caller has to see.
    /// <para>
    /// It used to disappear: the failure was logged, the method returned normally, and the harness
    /// started against a broken virtual host. The first visible symptom was an unrelated failure in some
    /// later spec, which is the worst possible place to learn about it. A log entry is not an error
    /// contract — the caller of StartAsync gets the exception, with its own type, cause and stack.
    /// </para>
    /// <para>
    /// Closing the connection afterwards stays best effort. It is a second, independent failure and must
    /// neither replace the first one nor swallow it, which is what the long-message case below pins:
    /// that is exactly the shape in which the close used to throw on its own.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Preparing_a_virtual_host_that_fails
    {
        [Test]
        public async Task Should_surface_the_setup_failure_to_the_caller()
        {
            var exception = await StartWith("f6-setup", new Refusal("the callback refused").Refuse);

            Assert.That(exception, Is.Not.Null, "the harness started although preparing the virtual host had failed");

            var marker = Flatten(exception).OfType<VirtualHostSetupMarkerException>().FirstOrDefault();

            Assert.That(marker, Is.Not.Null,
                $"the setup failure did not reach the caller; what arrived was: {exception}");
            Assert.That(marker.Message, Is.EqualTo("the callback refused"),
                "the cause was replaced on the way out");
        }

        /// <summary>
        /// A rethrow that reconstructs the exception discards the stack of the original throw site.
        /// <para>
        /// The discriminator has to be a frame from below the catch. Anything at or above it is no
        /// evidence at all: an exception that leaves an async method is captured and rethrown at the
        /// await, which appends the caller's frames either way — so asserting on the caller passes for
        /// both a preserved and a reconstructed stack. The frame of the callback that actually refused
        /// is the one that survives a bare rethrow and disappears with a "throw ex".
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_keep_the_stack_of_the_original_failure()
        {
            var exception = await StartWith("f6-stack", new Refusal("the callback refused").Refuse);

            var marker = Flatten(exception).OfType<VirtualHostSetupMarkerException>().FirstOrDefault();

            Assert.That(marker, Is.Not.Null);
            Assert.That(marker.StackTrace, Is.Not.Null.And.Contains(nameof(Refusal.Refuse)),
                "the frame that raised the failure is gone, so the stack was reconstructed instead of preserved: "
                + marker.StackTrace);
        }

        /// <summary>
        /// The close reason is built from the failure message and travels in an AMQP shortstr, which holds
        /// 255 bytes. A longer message is the one case in which closing throws on its own — and the setup
        /// failure still has to be what the caller receives.
        /// </summary>
        [Test]
        public async Task Should_not_let_a_failing_close_replace_the_setup_failure()
        {
            var longMessage = new string('x', 4096);

            Assert.That(Encoding.UTF8.GetByteCount(longMessage), Is.GreaterThan(255),
                "the message has to exceed what a close reason can carry, or this spec proves nothing");

            var exception = await StartWith("f6-close", new Refusal(longMessage).Refuse);

            var marker = Flatten(exception).OfType<VirtualHostSetupMarkerException>().FirstOrDefault();

            Assert.That(marker, Is.Not.Null,
                $"the close replaced or swallowed the setup failure; what arrived was: {exception}");
            Assert.That(marker.Message, Is.EqualTo(longMessage));
            Assert.That(Flatten(exception).OfType<ArgumentException>().Any(), Is.False,
                "an encoding failure from building the close reason reached the caller instead of the real cause");
        }

        [Test]
        public async Task Should_leave_the_successful_setup_untouched()
        {
            var called = false;

            var exception = await StartWith("f6-success", _ =>
            {
                called = true;
                return Task.CompletedTask;
            });

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Null, $"a harness whose virtual host setup succeeded failed to start: {exception}");
                Assert.That(called, Is.True, "the callback was never invoked, so the success path was not exercised");
            });
        }

        /// <summary>
        /// Starts a harness whose virtual host setup runs the given callback, and reports what the caller
        /// of Start received — null when it started cleanly.
        /// </summary>
        static async Task<Exception> StartWith(string virtualHost, Func<RabbitMQ.Client.IChannel, Task> callback)
        {
            await using var provider = new ServiceCollection()
                .ConfigureRabbitMqTestOptions(r =>
                {
                    r.CreateVirtualHostIfNotExists = true;
                    r.ConfigureVirtualHostCallback = callback;
                })
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<SetupProbeConsumer>();

                    x.UsingRabbitMq((context, cfg) => cfg.ConfigureEndpoints(context));

                    x.AddOptions<RabbitMqTransportOptions>()
                        .Configure(options =>
                        {
                            options.VHost = virtualHost;
                            options.ApplyRunScopedCredentials();
                        });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            try
            {
                await harness.Start();
            }
            catch (Exception exception)
            {
                return exception;
            }

            return null;
        }

        static IEnumerable<Exception> Flatten(Exception exception)
        {
            while (exception != null)
            {
                yield return exception;

                if (exception is AggregateException aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions.SelectMany(Flatten))
                        yield return inner;

                    yield break;
                }

                exception = exception.InnerException;
            }
        }


        /// <summary>
        /// A named frame for the setup failure. A lambda would do the same job, but the stack spec needs
        /// a frame it can name, and a compiler generated one is not something to assert against.
        /// </summary>
        class Refusal
        {
            readonly string _message;

            public Refusal(string message)
            {
                _message = message;
            }

            public Task Refuse(RabbitMQ.Client.IChannel channel)
            {
                throw new VirtualHostSetupMarkerException(_message);
            }
        }


        class VirtualHostSetupMarkerException :
            Exception
        {
            public VirtualHostSetupMarkerException(string message)
                : base(message)
            {
            }
        }


        class SetupProbeConsumer :
            IConsumer<SubmitOrder>
        {
            public Task Consume(ConsumeContext<SubmitOrder> context)
            {
                return Task.CompletedTask;
            }
        }
    }
}
