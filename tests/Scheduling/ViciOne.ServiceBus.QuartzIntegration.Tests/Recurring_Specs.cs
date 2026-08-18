namespace ViciOne.ServiceBus.QuartzIntegration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Scheduling;


    /// <summary>
    /// The recurring schedule of this fixture starts three seconds after it is created and ends seven seconds
    /// later, with a cron expression that fires every second. That is eight deliveries.
    ///
    /// Every step below moves the scheduler clock by one named functional step and then waits for a message
    /// barrier. Nothing waits on the wall clock, nothing polls, and no step hopes for a Quartz misfire: a jump
    /// over several due times may coalesce them, which is why each step names the transition it waits for.
    /// </summary>
    [TestFixture]
    public class Specifying_a_recurring_event :
        FrozenSchedulerClockTestFixture
    {
        const int ExpectedIntervals = 8;
        static readonly TimeSpan ScheduleStart = TimeSpan.FromSeconds(3);
        static readonly TimeSpan IntervalStep = TimeSpan.FromSeconds(1);
        static readonly TimeSpan HalfStep = TimeSpan.FromSeconds(0.5);
        // The horizon deliberately lies between two due seconds, so the marker never shares its instant with a
        // delivery.
        static readonly TimeSpan BeyondScheduleEnd = TimeSpan.FromSeconds(10.5);

        [SetUp]
        public async Task Reset_the_scheduler_clock_and_the_counters()
        {
            await ResetClock();

            _intervals.Reset();
        }

        [TearDown]
        public async Task Cancel_the_schedule_and_put_the_clock_back()
        {
            if (_recurring != null)
            {
                await Bus.CancelScheduledRecurringSend(_recurring);

                _recurring = null;
            }

            await ResetClock();
        }

        [Test]
        public async Task Should_cancel_recurring_schedule()
        {
            var scheduleId = NewId.NextGuid().ToString();

            Task<ConsumeContext<Done>> horizon = _intervals.Marker(out var marker);
            await Scheduler.ScheduleSend(InputQueueAddress, (ClockNow + BeyondScheduleEnd).UtcDateTime, new Done { Name = marker });

            _recurring = await QuartzEndpoint.ScheduleRecurringSend(InputQueueAddress, new MyCancelableSchedule(scheduleId, ClockNow),
                new Interval { Name = "Joe" });

            // The baseline is built by waiting for deliveries, not by reading a counter.
            await Advance_to_the_first_delivery();
            await Advance_by(IntervalStep);
            await Await("the second delivery before the cancel", _intervals.Received(2));

            // On a frozen clock, stopping half a second before the next due time means nothing further can
            // become due or be dispatched while the control command travels. That is a structural quiescence,
            // not something an endpoint concurrency limit could establish.
            await Advance_by(HalfStep);

            var deliveredBeforeCancel = _intervals.Count;

            Assert.That(deliveredBeforeCancel, Is.EqualTo(2),
                "The schedule has to deliver twice before it is canceled");

            // Quartz acquires due triggers ahead of their fire time. Holding the scheduler while the command is
            // processed releases an already acquired firing, so a canceled schedule is really gone.
            await HoldScheduler();

            await Bus.CancelScheduledRecurringSend(_recurring);

            // The control command travels the same transport as the schedule. Waiting for its successful consume
            // is what makes the following horizon a statement about a schedule that is really gone.
            Assert.That(await InMemoryTestHarness.Consumed.Any<CancelScheduledRecurringMessage>(
                    x => x.Exception == null && x.Context.Message.ScheduleId == scheduleId, TestCancellationToken),
                Is.True, "The cancel command was not consumed successfully by the scheduler");

            // Only now the teardown may forget the handle. If the assertion above fails, the schedule is still
            // known and gets cleaned up on the failure path as well.
            _recurring = null;

            await ReleaseScheduler();

            // A controlled horizon well past the end of the schedule. Its own message proves the scheduler has
            // worked through everything up to that point, so an unchanged counter is a statement, not a guess.
            await Advance_to(BeyondScheduleEnd);
            await Await("the horizon message after the cancel", horizon);

            Assert.That(_intervals.Count, Is.EqualTo(deliveredBeforeCancel),
                "A canceled schedule must not deliver again, not even after its regular end time");
        }

        [Test]
        public async Task Should_pause_recurring_schedule()
        {
            var scheduleId = NewId.NextGuid().ToString();

            Task<ConsumeContext<Done>> horizon = _intervals.Marker(out var marker);
            await Scheduler.ScheduleSend(InputQueueAddress, (ClockNow + BeyondScheduleEnd).UtcDateTime, new Done { Name = marker });

            _recurring = await QuartzEndpoint.ScheduleRecurringSend(InputQueueAddress, new MyCancelableSchedule(scheduleId, ClockNow),
                new Interval { Name = "Joe" });

            // The baseline is built by waiting for deliveries, not by reading a counter.
            await Advance_to_the_first_delivery();
            await Advance_by(IntervalStep);
            await Await("the second delivery before the pause", _intervals.Received(2));

            // On a frozen clock, stopping half a second before the next due time means nothing further can
            // become due or be dispatched while the control command travels. That is a structural quiescence,
            // not something an endpoint concurrency limit could establish.
            await Advance_by(HalfStep);

            var deliveredBeforePause = _intervals.Count;

            Assert.That(deliveredBeforePause, Is.EqualTo(2),
                "The schedule has to deliver twice before it is paused");

            await HoldScheduler();

            await Bus.PauseScheduledRecurringSend(_recurring);

            Assert.That(await InMemoryTestHarness.Consumed.Any<PauseScheduledRecurringMessage>(
                    x => x.Exception == null && x.Context.Message.ScheduleId == scheduleId, TestCancellationToken),
                Is.True, "The pause command was not consumed successfully by the scheduler");

            await ReleaseScheduler();

            await Advance_to(BeyondScheduleEnd);
            await Await("the horizon message after the pause", horizon);

            Assert.That(_intervals.Count, Is.EqualTo(deliveredBeforePause),
                "A paused schedule must not deliver again, not even after its regular end time");
        }

        [Test]
        public async Task Should_handle_now_properly()
        {
            Task<ConsumeContext<Done>> horizon = _intervals.Marker(out var marker);
            await Scheduler.ScheduleSend(InputQueueAddress, (ClockNow + BeyondScheduleEnd).UtcDateTime, new Done { Name = marker });

            _recurring = await QuartzEndpoint.ScheduleRecurringSend(InputQueueAddress, new MySchedule(ClockNow), new Interval { Name = "Joe" });

            // One step per due second. Each delivery is awaited before the clock moves again, so the count is
            // reached by eight separate firings and not by one jump that coalesces them.
            await Advance_to_the_first_delivery();

            for (var delivered = 1; delivered < ExpectedIntervals; delivered++)
            {
                await Advance_by(IntervalStep);
                await Await($"delivery {delivered + 1} of the recurring schedule", _intervals.Received(delivered + 1));
            }

            Assert.That(_intervals.Count, Is.EqualTo(ExpectedIntervals),
                $"The schedule runs for seven seconds at one delivery per second and therefore delivers {ExpectedIntervals} times");

            await Advance_to(BeyondScheduleEnd);
            await Await("the horizon message after the end of the schedule", horizon);

            Assert.That(_intervals.Count, Is.EqualTo(ExpectedIntervals),
                "After its end time the schedule must not deliver again");
        }

        [Test]
        public async Task Should_contain_additional_headers_that_provide_schedule_key_context()
        {
            _recurring = await QuartzEndpoint.ScheduleRecurringSend(InputQueueAddress, new MySchedule(ClockNow), new Interval { Name = "Joe" });

            await Advance_to_the_first_delivery();

            ConsumeContext<Interval> interval = _intervals.Last;

            Assert.Multiple(() =>
            {
                Assert.That(interval.Headers.Get<string>(MessageHeaders.Quartz.ScheduleId),
                    Is.EqualTo(_recurring.Schedule.ScheduleId),
                    "The schedule id header has to carry the identity of the schedule that produced the message");
                Assert.That(interval.Headers.Get<string>(MessageHeaders.Quartz.ScheduleGroup),
                    Is.EqualTo(_recurring.Schedule.ScheduleGroup),
                    "The schedule group header has to carry the group of the schedule that produced the message");
            });
        }

        [Test]
        public async Task Should_contain_additional_headers_that_provide_time_domain_context()
        {
            _recurring = await QuartzEndpoint.ScheduleRecurringSend(InputQueueAddress, new MySchedule(ClockNow), new Interval { Name = "Joe" });

            // The previous-sent header only carries a value once a delivery has a predecessor.
            await Advance_to_the_first_delivery();
            await Advance_by(IntervalStep);
            await Await("the second delivery of the recurring schedule", _intervals.Received(2));

            ConsumeContext<Interval> interval = _intervals.Last;

            DateTimeOffset? scheduled = interval.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Scheduled);
            DateTimeOffset? sent = interval.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Sent);
            DateTimeOffset? previousSent = interval.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.PreviousSent);
            DateTimeOffset? nextScheduled = interval.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.NextScheduled);

            Assert.Multiple(() =>
            {
                Assert.That(scheduled.HasValue, Is.True, "The scheduled header is missing");
                Assert.That(sent.HasValue, Is.True, "The sent header is missing");
                Assert.That(previousSent.HasValue, Is.True, "The previous sent header is missing");
                Assert.That(nextScheduled.HasValue, Is.True, "The next scheduled header is missing");
            });

            Assert.Multiple(() =>
            {
                Assert.That(sent.Value, Is.GreaterThanOrEqualTo(scheduled.Value),
                    "A message cannot be sent before the time it was scheduled for");
                Assert.That(previousSent.Value, Is.LessThan(sent.Value),
                    "The previous delivery of the schedule has to lie before this one");
                Assert.That(nextScheduled.Value, Is.GreaterThan(sent.Value),
                    "The next due time of the schedule has to lie after this delivery");
                Assert.That(nextScheduled.Value - scheduled.Value, Is.EqualTo(IntervalStep),
                    "The schedule fires once per second, so the next due time is one second after this one");
            });
        }

        Task Advance_to_the_first_delivery()
        {
            return Advance_to(ScheduleStart, "the first delivery of the recurring schedule", _intervals.Received(1));
        }

        async Task Advance_to(TimeSpan target, string what = null, Task barrier = null)
        {
            var remaining = target - ClockOffset;
            if (remaining > TimeSpan.Zero)
                await AdvanceClock(remaining);

            if (barrier != null)
                await Await(what, barrier);
        }

        Task Advance_by(TimeSpan step)
        {
            return AdvanceClock(step);
        }

        async Task Await(string what, Task barrier)
        {
            try
            {
                await barrier.WaitAsync(TestCancellationToken);
            }
            catch (OperationCanceledException)
            {
                Assert.Fail($"The test never observed {what}: {_intervals.Diagnostics} offset={ClockOffset}");
                throw;
            }
        }

        readonly IntervalObserver _intervals = new();
        ScheduledRecurringMessage<Interval> _recurring;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            // One message at a time. Otherwise the handler of a horizon marker can finish before the handler of
            // an interval that was dispatched earlier, and a counter read would race a delivery in flight.
            configurator.ConcurrentMessageLimit = 1;

            configurator.Handler<Interval>(context =>
            {
                _intervals.OnInterval(context);

                return Task.CompletedTask;
            });

            configurator.Handler<Done>(context =>
            {
                _intervals.OnMarker(context);

                return Task.CompletedTask;
            });
        }


        /// <summary>
        /// Counts the deliveries of the schedule and turns them into barriers. A marker message scheduled beyond
        /// the end of the schedule gives the test a horizon it can wait for instead of sleeping.
        /// </summary>
        class IntervalObserver
        {
            readonly object _lock = new();
            readonly List<TaskCompletionSource<int>> _waiters = new();
            readonly Dictionary<string, TaskCompletionSource<ConsumeContext<Done>>> _markers = new();
            ConsumeContext<Interval> _last;
            int _count;

            public int Count
            {
                get
                {
                    lock (_lock)
                        return _count;
                }
            }

            public ConsumeContext<Interval> Last
            {
                get
                {
                    lock (_lock)
                        return _last;
                }
            }

            public string Diagnostics
            {
                get
                {
                    lock (_lock)
                        return $"intervals={_count} markers={_markers.Count}";
                }
            }

            public void Reset()
            {
                lock (_lock)
                {
                    _count = 0;
                    _last = null;
                    _waiters.Clear();
                    _markers.Clear();
                }
            }

            public Task<ConsumeContext<Done>> Marker(string marker)
            {
                lock (_lock)
                    return _markers[marker].Task;
            }

            public Task<ConsumeContext<Done>> Marker(out string marker)
            {
                var name = NewId.NextGuid().ToString();
                var source = new TaskCompletionSource<ConsumeContext<Done>>(TaskCreationOptions.RunContinuationsAsynchronously);

                lock (_lock)
                    _markers[name] = source;

                marker = name;

                return source.Task;
            }

            public Task Received(int count)
            {
                TaskCompletionSource<int> waiter;
                lock (_lock)
                {
                    if (_count >= count)
                        return Task.CompletedTask;

                    waiter = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waiters.Add(waiter);
                }

                return WaitFor(waiter, count);

                async Task WaitFor(TaskCompletionSource<int> current, int required)
                {
                    while (await current.Task.ConfigureAwait(false) < required)
                    {
                        lock (_lock)
                        {
                            if (_count >= required)
                                return;

                            current = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                            _waiters.Add(current);
                        }
                    }
                }
            }

            public void OnInterval(ConsumeContext<Interval> context)
            {
                List<TaskCompletionSource<int>> waiters;
                int count;
                lock (_lock)
                {
                    _last = context;
                    count = ++_count;
                    waiters = new List<TaskCompletionSource<int>>(_waiters);
                    _waiters.Clear();
                }

                foreach (var waiter in waiters)
                    waiter.TrySetResult(count);
            }

            public void OnMarker(ConsumeContext<Done> context)
            {
                TaskCompletionSource<ConsumeContext<Done>> source;
                lock (_lock)
                    _markers.TryGetValue(context.Message.Name ?? string.Empty, out source);

                source?.TrySetResult(context);
            }
        }


        class MySchedule :
            DefaultRecurringSchedule
        {
            public MySchedule(DateTimeOffset now)
            {
                CronExpression = "0/1 * * * * ?";

                StartTime = now + ScheduleStart;
                EndTime = StartTime + TimeSpan.FromSeconds(7);

                Description = "my description";
            }
        }


        class MyCancelableSchedule :
            RecurringSchedule
        {
            public MyCancelableSchedule(string scheduleId, DateTimeOffset now)
            {
                ScheduleId = scheduleId;
                CronExpression = "0/1 * * * * ?";

                StartTime = now + ScheduleStart;
                EndTime = StartTime + TimeSpan.FromSeconds(20);
            }

            public MissedEventPolicy MisfirePolicy { get; protected set; }
            public string TimeZoneId { get; protected set; }
            public DateTimeOffset StartTime { get; protected set; }
            public DateTimeOffset? EndTime { get; protected set; }
            public string ScheduleId { get; private set; }
            public string ScheduleGroup { get; private set; }
            public string CronExpression { get; protected set; }
            public string Description { get; protected set; }
        }


        public class Interval
        {
            public string Name { get; set; }
        }


        public class Done
        {
            public string Name { get; set; }
        }
    }
}
