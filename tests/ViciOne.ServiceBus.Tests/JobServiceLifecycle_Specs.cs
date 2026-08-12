// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-10.
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Configuration;
    using ViciOne.ServiceBus.Contracts.JobService;
    using ViciOne.ServiceBus.Internals;
    using ViciOne.ServiceBus.JobService;
    using ViciOne.ServiceBus.TestFramework;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// A job service lifecycle transition has exactly one owner at a time.
    /// <para>
    /// Stop and BusStarted both move the stopping state and the heartbeat timer. Marking those fields
    /// volatile makes a write visible but orders nothing: a BusStarted that overtook a Stop left a
    /// service that considered itself running while the stop was still draining, and every start could
    /// hang a second heartbeat timer beside the one already ticking. Both transitions now run under a
    /// gate, and these specs force the overlap instead of assuming it cannot occur.
    /// </para>
    /// <para>
    /// The sequential restart is covered against a real broker by JobConsumer_Specs. What is proved here
    /// is what that spec cannot reach: two transitions in flight at once, a start that fails, and how
    /// many heartbeats are left behind afterwards.
    /// </para>
    /// <para>
    /// Every lifecycle transition announces itself by publishing a SetConcurrentJobLimit carrying its
    /// own <see cref="ConcurrentLimitKind" />. The publish endpoint is therefore the one place where a
    /// transition can be held open from the outside, and where heartbeats can be counted, without
    /// widening the public surface of the job service by a single member.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Owning_the_job_service_lifecycle
    {
        [Test]
        public async Task Should_not_let_a_start_overtake_a_running_stop()
        {
            var publish = new LifecyclePublishEndpoint { BlockOn = ConcurrentLimitKind.Stopped };
            var service = JobServiceLifecycle.NewService();

            var stop = service.Stop(publish);

            await publish.Blocked.OrTimeout(TimeSpan.FromSeconds(10));

            var started = service.BusStarted(publish);

            await Task.Delay(250);

            Assert.Multiple(() =>
            {
                Assert.That(started.IsCompleted, Is.False,
                    "the start completed while the stop was still in flight, so both transitions owned the lifecycle at once");
                Assert.That(publish.Count(ConcurrentLimitKind.Configured), Is.Zero,
                    "the start published its job limits from inside a running stop");
            });

            publish.Release();

            await stop.OrTimeout(TimeSpan.FromSeconds(10));
            await started.OrTimeout(TimeSpan.FromSeconds(10));

            await service.Stop(publish);
        }

        [Test]
        public async Task Should_not_let_a_stop_overtake_a_running_start()
        {
            var publish = new LifecyclePublishEndpoint { BlockOn = ConcurrentLimitKind.Configured };
            var service = JobServiceLifecycle.NewService();

            var started = service.BusStarted(publish);

            await publish.Blocked.OrTimeout(TimeSpan.FromSeconds(10));

            var stop = service.Stop(publish);

            await Task.Delay(250);

            Assert.Multiple(() =>
            {
                Assert.That(stop.IsCompleted, Is.False,
                    "the stop completed while the start was still in flight, so both transitions owned the lifecycle at once");
                Assert.That(publish.Count(ConcurrentLimitKind.Stopped), Is.Zero,
                    "the stop announced itself from inside a running start");
            });

            publish.Release();

            await started.OrTimeout(TimeSpan.FromSeconds(10));
            await stop.OrTimeout(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// A stop returns only once the heartbeat it owns has finished publishing.
        /// <para>
        /// A timer could be disposed while the publication it had already started was still in flight,
        /// so a heartbeat could reach the broker after the stop had returned — the service announcing
        /// itself alive after saying it had stopped. The publication is held open here rather than
        /// waited out, so the assertion is about ordering and not about how fast anything runs.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_not_finish_a_stop_while_a_heartbeat_is_still_publishing()
        {
            var publish = new LifecyclePublishEndpoint { BlockOn = ConcurrentLimitKind.Heartbeat };
            var service = JobServiceLifecycle.NewService();

            await service.BusStarted(publish);

            await publish.Blocked.OrTimeout(TimeSpan.FromSeconds(10));

            var stopping = service.Stop(publish);

            await Task.Delay(250);

            Assert.That(stopping.IsCompleted, Is.False,
                "the stop returned while a heartbeat was still publishing, so the service reported itself alive after stopping");

            publish.Release();

            await stopping.OrTimeout(TimeSpan.FromSeconds(10));

            var afterStop = publish.Count(ConcurrentLimitKind.Heartbeat);

            await Task.Delay(JobServiceLifecycle.HeartbeatInterval * 6);

            Assert.That(publish.Count(ConcurrentLimitKind.Heartbeat), Is.EqualTo(afterStop),
                "a heartbeat published after the stop had returned");
        }

        [Test]
        public async Task Should_leave_no_heartbeat_behind_after_a_stop()
        {
            var publish = new LifecyclePublishEndpoint();
            var service = JobServiceLifecycle.NewService();

            await service.BusStarted(publish);

            await publish.WaitForHeartbeats(2).OrTimeout(TimeSpan.FromSeconds(10));

            await service.Stop(publish);

            var afterStop = publish.Count(ConcurrentLimitKind.Heartbeat);

            await Task.Delay(JobServiceLifecycle.HeartbeatInterval * 6);

            Assert.That(publish.Count(ConcurrentLimitKind.Heartbeat), Is.EqualTo(afterStop),
                "a heartbeat was still ticking after the stop, so the timer outlived the lifecycle that owned it");
        }

        /// <summary>
        /// Three starts in a row must leave one timer, not three. The count is compared against a service
        /// started exactly once over the same window, so the assertion measures the number of timers and
        /// not the accuracy of the scheduler.
        /// </summary>
        [Test]
        public async Task Should_own_exactly_one_heartbeat_after_repeated_starts()
        {
            var repeated = new LifecyclePublishEndpoint();
            var repeatedService = JobServiceLifecycle.NewService();

            await repeatedService.BusStarted(repeated);
            await repeatedService.BusStarted(repeated);
            await repeatedService.BusStarted(repeated);

            var single = new LifecyclePublishEndpoint();
            var singleService = JobServiceLifecycle.NewService();

            await singleService.BusStarted(single);

            repeated.ResetCounts();
            single.ResetCounts();

            await Task.Delay(JobServiceLifecycle.HeartbeatInterval * 8);

            var repeatedBeats = repeated.Count(ConcurrentLimitKind.Heartbeat);
            var singleBeats = single.Count(ConcurrentLimitKind.Heartbeat);

            await repeatedService.Stop(repeated);
            await singleService.Stop(single);

            Assert.That(singleBeats, Is.GreaterThan(0), "the reference service published no heartbeat at all");
            Assert.That(repeatedBeats, Is.LessThanOrEqualTo(singleBeats + 1),
                $"three starts produced {repeatedBeats} heartbeats against {singleBeats} for a single start, "
                + "so a start hung a second timer beside the one already running");
        }

        /// <summary>
        /// A start that throws must leave the service exactly as unstarted as it was: no live heartbeat
        /// behind, and the failure at the caller rather than a silent partial start.
        /// </summary>
        [Test]
        public async Task Should_leave_no_heartbeat_when_a_start_fails()
        {
            var publish = new LifecyclePublishEndpoint { FailOn = ConcurrentLimitKind.Configured };
            var service = JobServiceLifecycle.NewService();

            Assert.That(async () => await service.BusStarted(publish), Throws.Exception,
                "a start that cannot publish its job limits reported success");

            publish.FailOn = null;
            publish.ResetCounts();

            await Task.Delay(JobServiceLifecycle.HeartbeatInterval * 6);

            Assert.That(publish.Count(ConcurrentLimitKind.Heartbeat), Is.Zero,
                "the failed start left a heartbeat ticking");

            // And the service is still startable: the failure left no half-held gate behind.
            await service.BusStarted(publish).OrTimeout(TimeSpan.FromSeconds(10));

            await service.Stop(publish);
        }
    }


    /// <summary>
    /// Job acceptance is closed for the whole duration of a stop, and reopened only by a start that
    /// actually succeeded.
    /// <para>
    /// This runs over a real consume context from the in-memory bus rather than a hand-built double.
    /// The rejection path constructs a ConsumeJobContext and notifies a fault through the receive
    /// context, so a stub would have had to reproduce that path to be believable — and a proof that
    /// reproduces the thing it is proving is not a proof.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Protecting_job_acceptance :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_reject_a_job_while_the_service_is_stopping()
        {
            var publish = new LifecyclePublishEndpoint();

            await _service.BusStarted(publish);
            await _service.Stop(publish);

            var accepted = await SubmitJob();

            Assert.That(accepted, Is.False, "a job was accepted after the service had stopped");
        }

        [Test]
        public async Task Should_reject_a_job_for_the_whole_duration_of_a_stop()
        {
            var publish = new LifecyclePublishEndpoint { BlockOn = ConcurrentLimitKind.Stopped };

            await _service.BusStarted(new LifecyclePublishEndpoint());

            var stop = _service.Stop(publish);

            await publish.Blocked.OrTimeout(TimeSpan.FromSeconds(10));

            var accepted = await SubmitJob();

            publish.Release();
            await stop.OrTimeout(TimeSpan.FromSeconds(10));

            Assert.That(accepted, Is.False,
                "a job was accepted while the stop was still draining, so acceptance closed only at the end of the stop");
        }

        [Test]
        public async Task Should_accept_a_job_after_a_start_that_succeeded()
        {
            var publish = new LifecyclePublishEndpoint();

            await _service.Stop(publish);
            await _service.BusStarted(publish);

            var accepted = await SubmitJob();

            Assert.That(accepted, Is.True, "a restarted service kept rejecting jobs");

            await _service.Stop(publish);
        }

        [Test]
        public async Task Should_reject_a_job_after_a_start_that_failed()
        {
            var publish = new LifecyclePublishEndpoint();

            await _service.Stop(publish);

            publish.FailOn = ConcurrentLimitKind.Configured;

            Assert.That(async () => await _service.BusStarted(publish), Throws.Exception);

            var accepted = await SubmitJob();

            Assert.That(accepted, Is.False,
                "a start that failed reopened job acceptance, so jobs were taken by a service that never came up");
        }

        /// <summary>
        /// The harder direction of the same rule. A service that was running and whose restart fails has
        /// to close acceptance, not keep it open: the heartbeat of the previous lifecycle is already
        /// disposed at that point, so a service that still took jobs would be one nothing supervises.
        /// Leaving the stopping state untouched in the failure path is invisible to a start that failed
        /// out of a stopped service, which is why this case is asserted separately.
        /// </summary>
        [Test]
        public async Task Should_reject_a_job_when_a_restart_of_a_running_service_fails()
        {
            var publish = new LifecyclePublishEndpoint();

            await _service.BusStarted(publish);

            Assert.That(await SubmitJob(), Is.True, "the running service was not accepting jobs to begin with");

            publish.FailOn = ConcurrentLimitKind.Configured;

            Assert.That(async () => await _service.BusStarted(publish), Throws.Exception);

            var accepted = await SubmitJob();

            Assert.That(accepted, Is.False,
                "a running service whose restart failed kept accepting jobs, although its heartbeat was already gone");
        }

        /// <summary>
        /// A job admitted a moment before the stop must not outlive it.
        /// <para>
        /// The pipe blocks between the admission decision and the registration of the handle — exactly
        /// the window the lifecycle gate did not cover. A stop entering there used to find an empty set,
        /// drain nothing and finish, while this job was already on its way in and appeared afterwards.
        /// The synchronisation is explicit: nothing here waits for a duration.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_not_finish_a_stop_while_a_job_is_still_being_admitted()
        {
            var publish = new LifecyclePublishEndpoint();

            await _service.Stop(publish);
            await _service.BusStarted(publish);

            Assert.That(await SubmitJob(), Is.True, "the service was not accepting jobs to begin with");

            _jobPipe.HoldNext();

            var submitting = SubmitJob();

            await _jobPipe.Entered.OrTimeout(TimeSpan.FromSeconds(10));

            // The job is admitted but not registered. A stop must see it.
            var stopping = _service.Stop(publish);

            await Task.Delay(250);

            Assert.That(stopping.IsCompleted, Is.False,
                "the stop finished while a job was still being admitted, so that job outlived it");

            _jobPipe.Release();

            await stopping.OrTimeout(TimeSpan.FromSeconds(10));
            await submitting.OrTimeout(TimeSpan.FromSeconds(10));
        }

        JobService _service;
        CountingJobPipe _jobPipe;

        [SetUp]
        public void ResetService()
        {
            _jobPipe.Reset();
        }

        /// <summary>
        /// Sends a StartJob through the real endpoint and reports whether the job pipe ran. The handler
        /// signals completion itself, so the wait is on the consumed message rather than on a delay.
        /// </summary>
        async Task<bool> SubmitJob()
        {
            var before = _jobPipe.Count;

            _jobPipe.Handled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await InputQueueSendEndpoint.Send<StartJob>(new
            {
                JobId = NewId.NextGuid(),
                AttemptId = NewId.NextGuid(),
                RetryAttempt = 0,
                Job = new Dictionary<string, object>(),
                JobTypeId = NewId.NextGuid(),
                JobProperties = new Dictionary<string, object>()
            });

            await _jobPipe.Handled.Task.OrTimeout(TimeSpan.FromSeconds(10));

            return _jobPipe.Count > before;
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _service = JobServiceLifecycle.NewService();
            _jobPipe = new CountingJobPipe();

            configurator.Handler<StartJob>(async context =>
            {
                var handled = _jobPipe.Handled;

                try
                {
                    await _service.StartJob(context, new SomeJob(), _jobPipe, new JobOptions<SomeJob>());
                }
                finally
                {
                    handled?.TrySetResult(true);
                }
            });
        }


        class CountingJobPipe :
            IPipe<ConsumeContext<SomeJob>>
        {
            TaskCompletionSource<bool> _entered;
            ManualResetEventSlim _release;
            int _armed;
            int _count;

            public int Count => _count;
            public TaskCompletionSource<bool> Handled { get; set; }

            /// <summary>Completes once the next job has reached the pipe and is being held there.</summary>
            public Task Entered => _entered?.Task ?? Task.CompletedTask;

            /// <summary>
            /// Holds the next job inside the window between its admission and the registration of its
            /// handle.
            /// <para>
            /// The wait is synchronous on purpose. StartJob does not await the pipe — it takes the task
            /// the pipe returns and registers the handle straight after — so the window is synchronous
            /// code, and only a synchronous block sits inside it. It runs on the consumer's thread, not
            /// the test's, and is bounded so a mistake here fails rather than hangs.
            /// </para>
            /// </summary>
            public void HoldNext()
            {
                _entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _release = new ManualResetEventSlim(false);

                Interlocked.Exchange(ref _armed, 1);
            }

            public void Release()
            {
                _release?.Set();
            }

            public Task Send(ConsumeContext<SomeJob> context)
            {
                Interlocked.Increment(ref _count);

                // Only the first job after HoldNext is held, and the event stays reachable so Release
                // can still open it: nulling the field here left the release with nothing to signal.
                if (Interlocked.Exchange(ref _armed, 0) == 1)
                {
                    _entered.TrySetResult(true);

                    _release.Wait(TimeSpan.FromSeconds(30));
                }

                return Task.CompletedTask;
            }

            public void Probe(ProbeContext context)
            {
            }

            public void Reset()
            {
                Interlocked.Exchange(ref _count, 0);
            }
        }
    }


    public class SomeJob
    {
    }


    static class JobServiceLifecycle
    {
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMilliseconds(100);

        public static JobService NewService()
        {
            var service = new JobService(new StubSettings(HeartbeatInterval));

            // The configurator argument is not used by this overload, which is what makes a broker-free
            // registration possible at all.
            service.RegisterJobType(null, new JobOptions<SomeJob>(), NewId.NextGuid(), "some-job");

            return service;
        }


        class StubSettings :
            JobServiceSettings
        {
            public StubSettings(TimeSpan heartbeatInterval)
            {
                HeartbeatInterval = heartbeatInterval;
            }

            public IJobService JobService => throw new NotSupportedException("these specs drive the job service directly");
            public TimeSpan HeartbeatInterval { get; }
            public TimeSpan RejectedJobDelay { get; } = TimeSpan.FromMilliseconds(10);
            public Uri InstanceAddress { get; } = new Uri("loopback://localhost/job-instance");
            public IReceiveEndpointConfigurator InstanceEndpointConfigurator => null;

            public IEnumerable<ValidationResult> Validate()
            {
                yield break;
            }
        }
    }


    /// <summary>
    /// Counts the lifecycle announcements a job service makes, and can hold one of them open or fail it.
    /// <para>
    /// Every announcement is a SetConcurrentJobLimit that names its own kind, so a Stop, a start and a
    /// heartbeat are told apart by the message itself rather than by a hook added to the product.
    /// </para>
    /// </summary>
    class LifecyclePublishEndpoint :
        IPublishEndpoint
    {
        readonly Dictionary<ConcurrentLimitKind, int> _counts = new Dictionary<ConcurrentLimitKind, int>();
        readonly TaskCompletionSource<bool> _blocked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<bool> _release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly object _lock = new object();

        TaskCompletionSource<bool> _heartbeatsReached;
        int _heartbeatsWanted;

        /// <summary>The announcement to hold open until <see cref="Release" />, if any.</summary>
        public ConcurrentLimitKind? BlockOn { get; set; }

        /// <summary>The announcement to fail, if any.</summary>
        public ConcurrentLimitKind? FailOn { get; set; }

        /// <summary>Completes once the blocked announcement has been reached.</summary>
        public Task Blocked => _blocked.Task;

        public void Release()
        {
            _release.TrySetResult(true);
        }

        public int Count(ConcurrentLimitKind kind)
        {
            lock (_lock)
                return _counts.TryGetValue(kind, out var count) ? count : 0;
        }

        public void ResetCounts()
        {
            lock (_lock)
                _counts.Clear();
        }

        public Task WaitForHeartbeats(int count)
        {
            lock (_lock)
            {
                if (Count(ConcurrentLimitKind.Heartbeat) >= count)
                    return Task.CompletedTask;

                _heartbeatsWanted = count;
                _heartbeatsReached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                return _heartbeatsReached.Task;
            }
        }

        public async Task Publish<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            if (message is not SetConcurrentJobLimit limit)
                return;

            if (FailOn == limit.Kind)
                throw new InvalidOperationException($"The lifecycle announcement was refused: {limit.Kind}");

            if (BlockOn == limit.Kind)
            {
                _blocked.TrySetResult(true);

                await _release.Task.ConfigureAwait(false);
            }

            TaskCompletionSource<bool> reached = null;

            lock (_lock)
            {
                _counts[limit.Kind] = (_counts.TryGetValue(limit.Kind, out var count) ? count : 0) + 1;

                if (limit.Kind == ConcurrentLimitKind.Heartbeat && _heartbeatsReached != null && _counts[limit.Kind] >= _heartbeatsWanted)
                {
                    reached = _heartbeatsReached;
                    _heartbeatsReached = null;
                }
            }

            reached?.TrySetResult(true);
        }

        public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            return Publish(message, cancellationToken);
        }

        public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            return Publish(message, cancellationToken);
        }

        public Task Publish(object message, CancellationToken cancellationToken = default)
        {
            return Publish<object>(message, cancellationToken);
        }

        public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        {
            return Publish<object>(message, cancellationToken);
        }

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
        {
            return Publish<object>(message, cancellationToken);
        }

        public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        {
            return Publish<object>(message, cancellationToken);
        }

        public Task Publish<T>(object values, CancellationToken cancellationToken = default)
            where T : class
        {
            throw new NotSupportedException("the job service publishes typed lifecycle messages only");
        }

        public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            throw new NotSupportedException("the job service publishes typed lifecycle messages only");
        }

        public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            throw new NotSupportedException("the job service publishes typed lifecycle messages only");
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            throw new NotSupportedException("the lifecycle specs observe the published messages directly");
        }
    }
}
