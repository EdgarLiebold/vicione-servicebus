namespace ViciOne.ServiceBus.Tests.Middleware
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
    using NUnit.Framework;
    using TestFramework;
    using Util;
    using ViciOne.ServiceBus.Internals;
    using ViciOne.ServiceBus.Middleware;


    /// <summary>
    /// A concurrency limit, a circuit breaker, a retry policy and an application filter in one pipe have to work
    /// together: the retry sits inside the breaker, the breaker reacts to the faults that survive the retry, and the
    /// controller that observes the breaker changes the concurrency limit of the same pipe while it is running.
    ///
    /// The composition is driven by exactly as many messages as the configured thresholds require, and every step is
    /// proven by the event it produces. Nothing is paced by a delay and no exception is swallowed, so this fixture
    /// cannot fail by hanging instead of by asserting.
    /// </summary>
    [TestFixture]
    public class Layering_retry_components_into_a_set
    {
        [Test]
        public async Task Should_support_interaction_between_filters()
        {
            var myFilter = new MyFilter { Throw = true };
            var retryObserver = new CountingRetryObserver();

            IPipeRouter router = new PipeRouter();

            TaskCompletionSource<CircuitBreakerOpened> opened = TaskUtil.GetTask<CircuitBreakerOpened>();
            TaskCompletionSource<int> limitApplied = TaskUtil.GetTask<int>();

            var controller = new MyController(router, opened, limitApplied);

            IPipe<InputContext> pipe = Pipe.New<InputContext>(cfg =>
            {
                cfg.UseConcurrencyLimit(ConcurrencyLimit, router);
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.ActiveThreshold = ActiveThreshold;
                    cb.TrackingPeriod = LongerThanTheCase;
                    cb.TripThreshold = TripThreshold;
                    cb.ResetInterval = LongerThanTheCase;

                    cb.Router = router;
                });
                cfg.UseRetry(x =>
                {
                    x.Immediate(1);
                    x.ConnectRetryObserver(retryObserver);
                });

                cfg.UseFilter(myFilter);
            });

            router.ConnectPipe(Pipe.New<EventContext<CircuitBreakerOpened>>(x => x.UseFilter(controller)));

            // The breaker counts one attempt per message, because the retry filter sits inside it. It becomes active
            // above the active threshold and every one of these messages fails, so the message that crosses the
            // threshold is the one that trips it. Sending them one after another keeps that statement exact.
            for (var index = 0; index < ActiveThreshold + 1; index++)
                Assert.That(async () => await pipe.Send(new InputContext("Hello")), Throws.TypeOf<IntentionalTestException>());

            CircuitBreakerOpened breakerOpened = await opened.Task.OrTimeout(s: 30);
            var appliedLimit = await limitApplied.Task.OrTimeout(s: 30);

            var attemptsWhenOpened = myFilter.AttemptCount;

            // The open breaker sits in front of the retry filter, so this message is rejected before any attempt.
            Assert.That(async () => await pipe.Send(new InputContext("Hello")), Throws.TypeOf<IntentionalTestException>());

            Assert.Multiple(() =>
            {
                Assert.That(attemptsWhenOpened, Is.EqualTo((ActiveThreshold + 1) * 2),
                    "An immediate retry of one must run every message through the filter twice");
                Assert.That(retryObserver.PreRetryCount, Is.EqualTo(ActiveThreshold + 1),
                    "Every message must have been retried once");
                Assert.That(retryObserver.RetryFaultCount, Is.EqualTo(ActiveThreshold + 1),
                    "Every message must have exhausted its retry and faulted");

                Assert.That(breakerOpened.Exception, Is.TypeOf<IntentionalTestException>(),
                    "The circuit breaker must report the exception that opened it");
                Assert.That(controller.OpenedCount, Is.EqualTo(1), "The circuit breaker must open exactly once");

                Assert.That(appliedLimit, Is.EqualTo(ReducedConcurrencyLimit),
                    "The controller must have applied the reduced concurrency limit to the running pipe");

                Assert.That(myFilter.AttemptCount, Is.EqualTo(attemptsWhenOpened),
                    "A message that arrives after the breaker opened must not reach the filter at all");
            });
        }

        const int ActiveThreshold = 5;
        const int ConcurrencyLimit = 10;
        const int ReducedConcurrencyLimit = 1;
        const int TripThreshold = 25;

        /// <summary>
        /// Both circuit breaker timers are armed with this value: the closed behaviour resets its counts after the
        /// tracking period and the open behaviour goes half open after the reset interval. Neither may happen while
        /// the case runs, otherwise the counts it asserts would silently change underneath it.
        /// </summary>
        static readonly TimeSpan LongerThanTheCase = TimeSpan.FromMinutes(5);


        class MyController :
            IFilter<EventContext<CircuitBreakerOpened>>
        {
            readonly TaskCompletionSource<int> _limitApplied;
            readonly TaskCompletionSource<CircuitBreakerOpened> _opened;
            readonly IPipeRouter _router;
            int _openedCount;

            public MyController(IPipeRouter router, TaskCompletionSource<CircuitBreakerOpened> opened, TaskCompletionSource<int> limitApplied)
            {
                _router = router;
                _opened = opened;
                _limitApplied = limitApplied;
            }

            public int OpenedCount => Volatile.Read(ref _openedCount);

            public async Task Send(EventContext<CircuitBreakerOpened> context, IPipe<EventContext<CircuitBreakerOpened>> next)
            {
                Interlocked.Increment(ref _openedCount);

                _opened.TrySetResult(context.Event);

                // Returns only after the concurrency limit filter has taken back the permits the lower limit costs,
                // so completing the barrier afterwards states that the change was executed, not just requested.
                await _router.SetConcurrencyLimit(ReducedConcurrencyLimit);

                _limitApplied.TrySetResult(ReducedConcurrencyLimit);

                await next.Send(context);
            }

            public void Probe(ProbeContext context)
            {
            }
        }


        class MyFilter :
            IFilter<InputContext>
        {
            int _attemptCount;

            public bool Throw { get; set; }

            public int AttemptCount => Volatile.Read(ref _attemptCount);

            public Task Send(InputContext context, IPipe<InputContext> next)
            {
                Interlocked.Increment(ref _attemptCount);

                if (Throw)
                    throw new IntentionalTestException("MyFilter is throwing");

                return next.Send(context);
            }

            public void Probe(ProbeContext context)
            {
            }
        }


        class CountingRetryObserver :
            IRetryObserver
        {
            int _preRetryCount;
            int _retryFaultCount;

            public int PreRetryCount => Volatile.Read(ref _preRetryCount);

            public int RetryFaultCount => Volatile.Read(ref _retryFaultCount);

            public Task PostCreate<T>(RetryPolicyContext<T> context)
                where T : class, PipeContext
            {
                return Task.CompletedTask;
            }

            public Task PostFault<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                return Task.CompletedTask;
            }

            public Task PreRetry<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                Interlocked.Increment(ref _preRetryCount);

                return Task.CompletedTask;
            }

            public Task RetryFault<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                Interlocked.Increment(ref _retryFaultCount);

                return Task.CompletedTask;
            }

            public Task RetryComplete<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                return Task.CompletedTask;
            }
        }
    }
}
