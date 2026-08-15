// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.QuartzIntegration.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Quartz;
    using Scheduling;
    using TestFramework;


    public abstract class QuartzInMemoryTestFixture :
        InMemoryTestFixture
    {
        readonly Lazy<IMessageScheduler> _messageScheduler;
        QuartzTimeAdjustment _adjustment;
        TimeSpan _appliedOffset;
        ISchedulerFactory _schedulerFactory;

        protected QuartzInMemoryTestFixture()
        {
            QuartzAddress = new Uri("loopback://localhost/quartz");

            _messageScheduler = new Lazy<IMessageScheduler>(() =>
                new MessageScheduler(new EndpointScheduleMessageProvider(() => GetSendEndpoint(QuartzAddress)), Bus.Topology));
        }

        protected Uri QuartzAddress { get; }

        /// <summary>
        /// The scheduler factory of the in memory scheduler, so a fixture can drive the scheduler directly.
        /// </summary>
        protected ISchedulerFactory SchedulerFactory => _schedulerFactory;

        protected ISendEndpoint QuartzEndpoint { get; set; }

        protected IMessageScheduler Scheduler => _messageScheduler.Value;

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.UseInMemoryScheduler(out _schedulerFactory);

            _adjustment = new QuartzTimeAdjustment(_schedulerFactory);

            base.ConfigureInMemoryBus(configurator);
        }

        /// <summary>
        /// The offset this fixture has applied to the scheduler clock so far.
        /// </summary>
        protected TimeSpan AppliedTimeOffset => _appliedOffset;

        /// <summary>
        /// Moves the scheduler clock by a named functional step and remembers the applied offset, so a test can
        /// put the clock back before the next one starts.
        /// </summary>
        protected async Task AdvanceTime(TimeSpan duration)
        {
            await _adjustment.AdvanceTime(duration).ConfigureAwait(false);

            _appliedOffset += duration;
        }

        /// <summary>
        /// Puts the scheduler clock back to where this fixture found it. Safe to call more than once and from a
        /// teardown that runs after a failed test.
        /// </summary>
        protected Task ResetTime()
        {
            return _appliedOffset == TimeSpan.Zero
                ? Task.CompletedTask
                : AdvanceTime(-_appliedOffset);
        }

        [OneTimeSetUp]
        public async Task Setup_quartz_service()
        {
            QuartzEndpoint = await GetSendEndpoint(QuartzAddress);
        }

        [OneTimeTearDown]
        public void Take_it_down()
        {
            // Disposing restores the process wide Quartz time provider even when a test failed or was aborted.
            _adjustment?.Dispose();
            _appliedOffset = TimeSpan.Zero;
        }
    }
}
