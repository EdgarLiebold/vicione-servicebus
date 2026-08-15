// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Internals;
    using ViciOne.ServiceBus.Testing;
    using ViciOne.ServiceBus.Testing.Implementations;


    /// <summary>
    /// The inactivity observer of the test framework decides when a harness considers itself quiet. Forcing that
    /// decision has to take effect at once, and the ordinary path has to keep waiting exactly as long as a
    /// connected source still reports activity.
    ///
    /// The observed task is created lazily on first access, so forcing the completion before anyone awaited it used
    /// to be ignored: the freshly created timeout task began with an unconditional wait over the whole interval.
    /// Every case here uses an interval that provably cannot elapse while it runs, so a completion can only come
    /// from the statement under test and never from an elapsed clock.
    /// </summary>
    [TestFixture]
    public class InactivityObserver_Specs
    {
        [Test]
        public async Task Should_complete_when_forced_before_the_task_is_first_awaited()
        {
            using var cancellation = new CancellationTokenSource();

            var observer = new AsyncInactivityObserver(LongerThanTheCase, cancellation.Token);

            observer.ForceInactive();

            await observer.InactivityTask.OrTimeout(s: 10);

            Assert.That(observer.InactivityToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public async Task Should_complete_when_forced_during_the_first_interval()
        {
            using var cancellation = new CancellationTokenSource();

            var observer = new AsyncInactivityObserver(LongerThanTheCase, cancellation.Token);

            // Materializes the task, so the interval is already running when the completion is forced.
            Task inactivity = observer.InactivityTask;

            Assert.That(inactivity.IsCompleted, Is.False, "The interval cannot have elapsed");

            observer.ForceInactive();

            await inactivity.OrTimeout(s: 10);
        }

        [Test]
        public async Task Should_keep_waiting_while_a_source_is_active_and_close_once_it_is_not()
        {
            using var cancellation = new CancellationTokenSource();

            var observer = new AsyncInactivityObserver(LongerThanTheCase, cancellation.Token);
            var source = new ControlledSource { IsInactive = false };

            observer.Connected(source);

            Task inactivity = observer.InactivityTask;

            await observer.NoActivity();

            Assert.That(inactivity.IsCompleted, Is.False,
                "A source that still reports activity must keep the observer waiting");

            source.IsInactive = true;

            await observer.NoActivity();

            await inactivity.OrTimeout(s: 10);

            Assert.That(observer.InactivityToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public async Task Should_query_the_source_again_after_every_elapsed_interval()
        {
            // The case above drives CheckSourceActivity directly and therefore never enters the interval loop.
            // This one lets the interval of the product elapse and observes the queries the loop itself makes.
            using var cancellation = new CancellationTokenSource();

            var observer = new AsyncInactivityObserver(ShortEnoughToElapse, cancellation.Token);
            var source = new QueryRecordingSource();

            observer.Connected(source);

            Task inactivity = observer.InactivityTask;

            await Reached(source.FirstQuery, "The loop never queried the source after its first interval elapsed");

            // The next query cannot come before another interval has elapsed, so this observation cannot race it.
            Assert.That(inactivity.IsCompleted, Is.False,
                "A source that still reports activity must keep the observer waiting after the first interval");

            await Reached(source.SecondQuery, "The loop did not begin another interval after the source reported activity");

            await Reached(inactivity, "The loop did not close once the source reported inactivity");

            Assert.Multiple(() =>
            {
                Assert.That(observer.InactivityToken.IsCancellationRequested, Is.True);
                Assert.That(source.QueryCount, Is.EqualTo(2), "The loop must query once per elapsed interval");
            });
        }

        /// <summary>
        /// Waits for an event of the product and reports the missing statement by name. A bare timeout would only
        /// say that a task was cancelled and never what was not observed.
        /// </summary>
        static async Task Reached(Task signal, string missing)
        {
            try
            {
                await signal.OrTimeout(s: 30);
            }
            catch (TimeoutException)
            {
                Assert.Fail(missing);
            }
        }

        /// <summary>
        /// Long enough that the interval of the observer provably cannot elapse while a case runs.
        /// </summary>
        static readonly TimeSpan LongerThanTheCase = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Short enough that the interval of the product elapses twice within the case, and long enough that the
        /// observation between two queries cannot race the loop. It is the interval of the product, never a test
        /// oracle: the case waits for events only.
        /// </summary>
        static readonly TimeSpan ShortEnoughToElapse = TimeSpan.FromMilliseconds(500);


        /// <summary>
        /// Reports activity on the first query the loop makes and inactivity from the second on, and makes both
        /// queries observable.
        /// </summary>
        class QueryRecordingSource :
            IInactivityObservationSource
        {
            readonly TaskCompletionSource<bool> _first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            readonly TaskCompletionSource<bool> _second = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int _queries;

            public Task FirstQuery => _first.Task;
            public Task SecondQuery => _second.Task;
            public int QueryCount => Volatile.Read(ref _queries);

            public bool IsInactive
            {
                get
                {
                    if (Interlocked.Increment(ref _queries) == 1)
                    {
                        _first.TrySetResult(true);

                        return false;
                    }

                    _second.TrySetResult(true);

                    return true;
                }
            }

            public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer)
            {
                throw new NotSupportedException("The observer of this case is connected directly");
            }
        }


        class ControlledSource :
            IInactivityObservationSource
        {
            public bool IsInactive { get; set; }

            public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer)
            {
                throw new NotSupportedException("The observer of this case is connected directly");
            }
        }
    }
}
