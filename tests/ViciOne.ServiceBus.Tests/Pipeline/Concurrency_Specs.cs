namespace ViciOne.ServiceBus.Tests.Pipeline
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using NUnit.Framework;
    using TestFramework;


    [TestFixture]
    public class Specifying_a_concurrency_limit
    {
        [Test]
        public async Task Should_allow_just_enough_threads_at_once()
        {
            var currentCount = 0;
            var maxCount = 0;

            IPipe<ConsumeContext<Running_two_in_memory_transports.A>> pipe = Pipe.New<ConsumeContext<Running_two_in_memory_transports.A>>(x =>
            {
                x.UseConcurrencyLimit(32);
                x.UseExecuteAsync(async payload =>
                {
                    var current = Interlocked.Increment(ref currentCount);
                    while (current > maxCount)
                        Interlocked.CompareExchange(ref maxCount, current, maxCount);

                    await Task.Delay(10);

                    Interlocked.Decrement(ref currentCount);
                });
            });

            var context = new TestConsumeContext<Running_two_in_memory_transports.A>(new Running_two_in_memory_transports.A());

            Task[] tasks = Enumerable.Range(0, 500)
                .Select(index => Task.Run(async () => await pipe.Send(context)))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.That(maxCount, Is.EqualTo(32));
        }

        [Test]
        public async Task Should_prevent_too_many_threads_at_one_time()
        {
            var currentCount = 0;
            var maxCount = 0;

            IPipe<ConsumeContext<Running_two_in_memory_transports.A>> pipe = Pipe.New<ConsumeContext<Running_two_in_memory_transports.A>>(x =>
            {
                x.UseConcurrencyLimit(1);
                x.UseExecuteAsync(async payload =>
                {
                    var current = Interlocked.Increment(ref currentCount);
                    while (current > maxCount)
                        Interlocked.CompareExchange(ref maxCount, current, maxCount);

                    await Task.Delay(10);

                    Interlocked.Decrement(ref currentCount);
                });
            });

            var context = new TestConsumeContext<Running_two_in_memory_transports.A>(new Running_two_in_memory_transports.A());

            Task[] tasks = Enumerable.Range(0, 50)
                .Select(index => Task.Run(async () => await pipe.Send(context)))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.That(maxCount, Is.EqualTo(1));
        }
    }


    /// <summary>
    /// The same rate limit contract as in the middleware fixture, proven for the consume context closure of the
    /// filter. Both cases use the held send as the observable effect of the limiter rather than a stopwatch reading.
    /// </summary>
    [TestFixture]
    public class Specifying_a_rate_limit
    {
        [Test]
        public async Task Should_count_success_and_failure_as_same()
        {
            var count = 0;
            IPipe<ConsumeContext<Running_two_in_memory_transports.A>> pipe = Pipe.New<ConsumeContext<Running_two_in_memory_transports.A>>(x =>
            {
                x.UseRateLimit(10, NoReplenishment);
                x.UseExecute(payload =>
                {
                    var index = Interlocked.Increment(ref count);
                    if (index % 2 == 0)
                        throw new IntentionalTestException();
                });
            });

            var context = new TestConsumeContext<Running_two_in_memory_transports.A>(new Running_two_in_memory_transports.A());

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

            Task pending = pipe.Send(new TestConsumeContext<Running_two_in_memory_transports.A>(new Running_two_in_memory_transports.A(), held.Token));

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
            IPipe<ConsumeContext<Running_two_in_memory_transports.A>> capped = Pipe.New<ConsumeContext<Running_two_in_memory_transports.A>>(x =>
            {
                x.UseRateLimit(10, NoReplenishment);
                x.UseExecute(payload =>
                {
                    Interlocked.Increment(ref count);
                });
            });

            var context = new TestConsumeContext<Running_two_in_memory_transports.A>(new Running_two_in_memory_transports.A());

            for (var index = 0; index < 10; index++)
                await capped.Send(context);

            using var held = new CancellationTokenSource();

            Task pending = capped.Send(new TestConsumeContext<Running_two_in_memory_transports.A>(new Running_two_in_memory_transports.A(), held.Token));

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(10), "The limit must admit exactly ten messages");
                Assert.That(pending.IsCompleted, Is.False, "The eleventh message must be held until the interval elapses");
            });

            held.Cancel();

            Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>());

            // The interval: the completion of the held send is the barrier for the returned permits.
            var replenished = 0;
            IPipe<ConsumeContext<Running_two_in_memory_transports.A>> replenishing =
                Pipe.New<ConsumeContext<Running_two_in_memory_transports.A>>(x =>
                {
                    x.UseRateLimit(2, TimeSpan.FromSeconds(1));
                    x.UseExecute(payload =>
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
        /// Long enough that the replenishment timer of the filter provably cannot fire while a case runs.
        /// </summary>
        static readonly TimeSpan NoReplenishment = TimeSpan.FromHours(1);
    }
}
