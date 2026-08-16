namespace ViciOne.ServiceBus.Tests.Middleware
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework;
    using ViciOne.ServiceBus.Internals;
    using ViciOne.ServiceBus.Middleware;


    /// <summary>
    /// The rate limit filter admits a fixed number of messages per interval and returns the consumed permits when
    /// the interval elapses.
    ///
    /// Every case in this fixture used to assert nothing but a stopwatch reading of a hundred and one messages that
    /// were pushed through the filter concurrently, so a slow machine passed and a broken limiter that simply stalled
    /// passed as well. The limiter is now driven by its own observable effects: a message that has no permit does not
    /// complete, and the send that was held completes exactly when the limit is raised or the interval replenishes
    /// the permits. Where the interval must provably not interfere, it is configured so long that its timer cannot
    /// fire while the case runs.
    /// </summary>
    [TestFixture]
    public class Specifying_a_rate_limit
    {
        [Test]
        public async Task Should_allow_dynamic_reconfiguration_down()
        {
            var router = new PipeRouter();
            var count = 0;
            IPipe<InputContext> pipe = Pipe.New<InputContext>(cfg =>
            {
                cfg.UseRateLimit(100, NoReplenishment, router);
                cfg.UseExecute(cxt =>
                {
                    Interlocked.Increment(ref count);
                });
            });

            // The command completes only after the filter has taken back the ninety permits it has to give up.
            await router.SetRateLimit(10);

            var context = new InputContext("Hello");

            for (var index = 0; index < 10; index++)
                await pipe.Send(context);

            Assert.That(count, Is.EqualTo(10), "The lowered limit must admit exactly ten messages");

            using var held = new CancellationTokenSource();

            Task pending = pipe.Send(new InputContext("Hello", held.Token));

            Assert.That(pending.IsCompleted, Is.False, "The eleventh message must be held by the lowered limit");

            held.Cancel();

            Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>(),
                "The held message must be waiting on the rate limit, not on anything else");

            Assert.That(count, Is.EqualTo(10), "A held message must not reach the pipe");
        }

        [Test]
        public async Task Should_allow_dynamic_reconfiguration_up()
        {
            var router = new PipeRouter();
            var count = 0;
            IPipe<InputContext> pipe = Pipe.New<InputContext>(cfg =>
            {
                cfg.UseRateLimit(10, NoReplenishment, router);
                cfg.UseExecute(cxt =>
                {
                    Interlocked.Increment(ref count);
                });
            });

            var context = new InputContext("Hello");

            for (var index = 0; index < 10; index++)
                await pipe.Send(context);

            Task pending = pipe.Send(context);

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(10));
                Assert.That(pending.IsCompleted, Is.False, "The eleventh message must be held by the initial limit");
            });

            await router.SetRateLimit(100);

            // The completion of the previously held send is the barrier; it can only be caused by the raised limit.
            await pending.OrTimeout(s: 30);

            Assert.That(count, Is.EqualTo(11), "Raising the limit must release exactly the message it had held");
        }

        [Test]
        public async Task Should_count_success_and_failure_as_same()
        {
            var count = 0;
            IPipe<InputContext> pipe = Pipe.New<InputContext>(cfg =>
            {
                cfg.UseRateLimit(10, NoReplenishment);
                cfg.UseExecute(cxt =>
                {
                    var index = Interlocked.Increment(ref count);
                    if (index % 2 == 0)
                        throw new IntentionalTestException();
                });
            });

            var context = new InputContext("Hello");

            var faulted = 0;
            for (var index = 0; index < 10; index++)
            {
                try
                {
                    await pipe.Send(context);
                }
                catch (IntentionalTestException)
                {
                    faulted++;
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(10));
                Assert.That(faulted, Is.EqualTo(5), "Every second message must have faulted");
            });

            using var held = new CancellationTokenSource();

            Task pending = pipe.Send(new InputContext("Hello", held.Token));

            Assert.That(pending.IsCompleted, Is.False,
                "A faulted message consumes its permit like a successful one, so no permit is left for the eleventh");

            held.Cancel();

            Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public async Task Should_only_do_n_messages_per_interval()
        {
            // The limit: an interval that cannot elapse during the case proves the cap without measuring time.
            var count = 0;
            IPipe<InputContext> capped = Pipe.New<InputContext>(x =>
            {
                x.UseRateLimit(10, NoReplenishment);
                x.UseExecute(cxt =>
                {
                    Interlocked.Increment(ref count);
                });
            });

            var context = new InputContext("Hello");

            for (var index = 0; index < 10; index++)
                await capped.Send(context);

            using var held = new CancellationTokenSource();

            Task pending = capped.Send(new InputContext("Hello", held.Token));

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(10), "The limit must admit exactly ten messages");
                Assert.That(pending.IsCompleted, Is.False, "The eleventh message must be held until the interval elapses");
            });

            held.Cancel();

            Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>());

            // The interval: the held send completes when the elapsed interval returns the consumed permits. The
            // completion is the barrier, so nothing here reads a clock or compares an elapsed duration.
            var replenished = 0;
            IPipe<InputContext> replenishing = Pipe.New<InputContext>(x =>
            {
                x.UseRateLimit(2, TimeSpan.FromSeconds(1));
                x.UseExecute(cxt =>
                {
                    Interlocked.Increment(ref replenished);
                });
            });

            await replenishing.Send(context);
            await replenishing.Send(context);

            Assert.That(replenished, Is.EqualTo(2));

            await replenishing.Send(context).OrTimeout(s: 30);

            Assert.That(replenished, Is.EqualTo(3), "The elapsed interval must return the consumed permits");
        }

        /// <summary>
        /// Long enough that the replenishment timer of the filter provably cannot fire while a case runs, so a
        /// message that is held stays held.
        /// </summary>
        static readonly TimeSpan NoReplenishment = TimeSpan.FromHours(1);
    }
}
