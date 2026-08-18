namespace ViciOne.ServiceBus.QuartzIntegration.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Quartz;


    /// <summary>
    /// A Quartz fixture whose scheduler clock stands still between explicit steps.
    ///
    /// The adjustment of the product keeps returning the running system clock plus an offset, so time continues
    /// to pass while a test awaits a barrier, and a due time that is only reached by chance can fire between two
    /// steps. Everything this fixture schedules therefore reads its time from one frozen clock: the schedule
    /// start, the schedule end, every due time, every horizon and the clock Quartz itself asks. No test in this
    /// fixture reads DateTime.Now or DateTime.UtcNow.
    ///
    /// Because the clock only moves inside <see cref="AdvanceClock"/>, a step that ends between two due times is
    /// a structural statement: no further trigger can become due or be dispatched while the test works.
    /// </summary>
    public abstract class FrozenSchedulerClockTestFixture :
        QuartzInMemoryTestFixture
    {
        FrozenClock _clock;

        /// <summary>
        /// The single time truth of this fixture.
        /// </summary>
        protected DateTimeOffset ClockNow => _clock.UtcNow;

        /// <summary>
        /// How far the clock has been moved since the current case started.
        /// </summary>
        protected TimeSpan ClockOffset => _clock.Offset;

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            base.ConfigureInMemoryBus(configurator);

            // Installed after the base fixture, so this clock is the one Quartz asks.
            _clock = new FrozenClock();
        }

        /// <summary>
        /// Moves the frozen clock by one named functional step and lets the scheduler work through whatever
        /// becomes due. The caller then waits for the barrier that belongs to the step.
        /// </summary>
        protected async Task AdvanceClock(TimeSpan step)
        {
            IScheduler scheduler = await SchedulerFactory.GetScheduler().ConfigureAwait(false);

            await scheduler.Standby().ConfigureAwait(false);

            _clock.Advance(step);

            await scheduler.Start().ConfigureAwait(false);
        }

        /// <summary>
        /// Holds the scheduler while a control command is processed. Quartz acquires due triggers ahead of
        /// their fire time, so a trigger that is deleted afterwards can still have an acquired firing pending.
        /// Standby releases those acquisitions, which is what makes a cancel or pause observable as complete.
        /// </summary>
        protected async Task HoldScheduler()
        {
            IScheduler scheduler = await SchedulerFactory.GetScheduler().ConfigureAwait(false);

            await scheduler.Standby().ConfigureAwait(false);
        }

        protected async Task ReleaseScheduler()
        {
            IScheduler scheduler = await SchedulerFactory.GetScheduler().ConfigureAwait(false);

            await scheduler.Start().ConfigureAwait(false);
        }

        /// <summary>
        /// Puts the clock back to where the current case found it. Safe after a failed or aborted case.
        /// </summary>
        protected async Task ResetClock()
        {
            if (_clock.Offset == TimeSpan.Zero)
                return;

            IScheduler scheduler = await SchedulerFactory.GetScheduler().ConfigureAwait(false);

            await scheduler.Standby().ConfigureAwait(false);

            _clock.Reset();

            await scheduler.Start().ConfigureAwait(false);
        }

        [OneTimeTearDown]
        public void Release_the_frozen_clock()
        {
            _clock?.Dispose();
            _clock = null;
        }


        sealed class FrozenClock :
            IDisposable
        {
            readonly long _startTicks;
            long _nowTicks;

            public FrozenClock()
            {
                // Aligned to the next whole second. Cron expressions fire on whole seconds, so an unaligned start
                // would put the due times at an arbitrary sub second distance from the steps of a test, and a
                // step that is meant to stop between two due times could land right next to one.
                _startTicks = (DateTimeOffset.UtcNow.UtcTicks / TimeSpan.TicksPerSecond + 1) * TimeSpan.TicksPerSecond;
                Volatile.Write(ref _nowTicks, _startTicks);

                // Quartz reads the clock from its own threads, so the value is published through a volatile read.
                SystemTime.UtcNow = () => new DateTimeOffset(Volatile.Read(ref _nowTicks), TimeSpan.Zero);
                SystemTime.Now = () => new DateTimeOffset(Volatile.Read(ref _nowTicks), TimeSpan.Zero).ToLocalTime();
            }

            public DateTimeOffset UtcNow => new(Volatile.Read(ref _nowTicks), TimeSpan.Zero);

            public TimeSpan Offset => TimeSpan.FromTicks(Volatile.Read(ref _nowTicks) - _startTicks);

            public void Advance(TimeSpan step)
            {
                Volatile.Write(ref _nowTicks, Volatile.Read(ref _nowTicks) + step.Ticks);
            }

            public void Reset()
            {
                Volatile.Write(ref _nowTicks, _startTicks);
            }

            public void Dispose()
            {
                SystemTime.UtcNow = () => DateTimeOffset.UtcNow;
                SystemTime.Now = () => DateTimeOffset.Now;
            }
        }
    }
}
