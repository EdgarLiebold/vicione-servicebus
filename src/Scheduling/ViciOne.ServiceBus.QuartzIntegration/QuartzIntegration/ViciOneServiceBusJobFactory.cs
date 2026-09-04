namespace ViciOne.ServiceBus.QuartzIntegration
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Quartz;
    using Quartz.Extensibility;


    public class ViciOneServiceBusJobFactory :
        IJobFactory
    {
        readonly IBus? _bus;
        readonly TimeProvider? _timeProvider;

        /// <summary>
        /// Creates a factory that resolves the bus and clock from the Quartz scheduler context populated by
        /// <c>UseInMemoryScheduler</c>.
        /// </summary>
        public ViciOneServiceBusJobFactory()
        {
        }

        public ViciOneServiceBusJobFactory(IBus bus, TimeProvider timeProvider)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public ValueTask<JobScope> CreateJob(TriggerFiredBundle bundle, IScheduler scheduler, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(bundle);
            ArgumentNullException.ThrowIfNull(scheduler);
            cancellationToken.ThrowIfCancellationRequested();

            IJob job = _bus is not null && _timeProvider is not null
                ? new ScheduledMessageJob(_bus, _timeProvider)
                : new ScheduledMessageJob();

            return ValueTask.FromResult(new JobScope(job, state: null));
        }

        public async ValueTask ReturnJob(JobScope scope, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (scope.Job is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else if (scope.Job is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
