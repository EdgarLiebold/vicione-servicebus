// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    [TestFixture]
    public class PublishTimeout_Specs :
        AsyncTestFixture
    {
        /// <summary>
        /// Cancelling a publish has to surface as a cancellation of the token the caller passed.
        /// <para>
        /// The retry loop waits out its backoff on a token linked from the caller token and the
        /// stopping token. A cancellation raised by that wait therefore carries the linked token, and
        /// the loop used to rethrow it unchanged: the caller saw a TaskCanceledException bound to a
        /// token it had never seen, instead of its own cancellation. The same linked token also made
        /// the 'transport is stopping' translation unreachable, because that branch matches on the
        /// exception carrying the stopping token.
        /// </para>
        /// <para>
        /// The assertions below are the ones the imported spec already made, plus the token identity
        /// that names the actual cause rather than only its symptom.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_fault_with_operation_cancelled_on_publish()
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                BusTestFixture.ConfigureBusDiagnostics(x);

                x.Host("unknown_host");

                x.AutoStart = true;
            });

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            Task<BusHandle> startTask = busControl.StartAsync(startTimeout.Token).OrCanceled(TestCancellationToken);

            var publishTimer = Stopwatch.StartNew();
            using var publishTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var cancelled = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await busControl.Publish(new PingMessage(), publishTimeout.Token);
            });

            Assert.That(cancelled.CancellationToken, Is.EqualTo(publishTimeout.Token),
                "the cancellation was reported against a token the caller never passed");

            publishTimer.Stop();
            var publishElapsed = publishTimer.Elapsed;

            Assert.That(publishElapsed, Is.LessThan(TimeSpan.FromSeconds(19)));

            Assert.ThrowsAsync<RabbitMqConnectionException>(async () =>
            {
                await startTask;
            });
        }

        public PublishTimeout_Specs()
            : base(new InMemoryTestHarness())
        {
        }
    }
}
