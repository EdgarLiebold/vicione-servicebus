namespace ViciOne.ServiceBus.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CircuitBreaker;


    public class CircuitBreakerFilter<TContext> :
        IFilter<TContext>,
        ICircuitBreaker
        where TContext : class, PipeContext
    {
        readonly IExceptionFilter _exceptionFilter;
        readonly CircuitBreakerSettings _settings;
        readonly object _stateLock;
        ICircuitBreakerBehavior _behavior;

        public CircuitBreakerFilter(CircuitBreakerSettings settings, IExceptionFilter exceptionFilter)
        {
            _settings = settings;
            _exceptionFilter = exceptionFilter;
            _stateLock = new object();

            _behavior = new ClosedBehavior(this);
        }

        public TimeSpan OpenDuration => _settings.TrackingPeriod;

        public TimeProvider TimeProvider => _settings.TimeProvider;

        CircuitBreakerTransition ICircuitBreaker.Open(Exception exception, ICircuitBreakerBehavior behavior,
            IEnumerator<TimeSpan> timeoutEnumerator)
        {
            lock (_stateLock)
            {
                if (!ReferenceEquals(_behavior, behavior))
                    return CircuitBreakerTransition.Unchanged;

                timeoutEnumerator ??= _settings.ResetTimeout.GetEnumerator();
                Volatile.Write(ref _behavior, new OpenBehavior(this, exception, timeoutEnumerator));
            }

            return new CircuitBreakerTransition(true, PublishOpened(exception));
        }

        CircuitBreakerTransition ICircuitBreaker.Close(ICircuitBreakerBehavior behavior)
        {
            lock (_stateLock)
            {
                if (!ReferenceEquals(_behavior, behavior))
                    return CircuitBreakerTransition.Unchanged;

                Volatile.Write(ref _behavior, new ClosedBehavior(this));
            }

            return new CircuitBreakerTransition(true, PublishClosed());
        }

        bool ICircuitBreaker.ClosePartially(Exception exception, IEnumerator<TimeSpan> timeoutEnumerator,
            ICircuitBreakerBehavior behavior)
        {
            lock (_stateLock)
            {
                if (!ReferenceEquals(_behavior, behavior))
                    return false;

                Volatile.Write(ref _behavior, new HalfOpenBehavior(this, exception, timeoutEnumerator));
                return true;
            }
        }

        public int TripThreshold => _settings.TripThreshold;

        public int ActiveThreshold => _settings.ActiveThreshold;

        public async Task Send(TContext context, IPipe<TContext> next)
        {
            ICircuitBreakerBehavior behavior = Volatile.Read(ref _behavior);
            try
            {
                await behavior.PreSend().ConfigureAwait(false);

                await next.Send(context).ConfigureAwait(false);

                await behavior.PostSend().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_exceptionFilter.Match(ex))
                    throw;

                await behavior.SendFault(ex).ConfigureAwait(false);

                throw;
            }
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("circuitBreaker");
            scope.Set(new
            {
                ActiveCount = _settings.ActiveThreshold,
                _settings.TripThreshold,
                Duration = _settings.TrackingPeriod,
                ResetTimeout = _settings.ResetTimeout.Take(10).ToArray()
            });

            Volatile.Read(ref _behavior).Probe(scope);
        }

        Task PublishOpened(Exception exception)
        {
            try
            {
                return _settings.Router?.PublishCircuitBreakerOpened(exception) ?? Task.CompletedTask;
            }
            catch (Exception caught)
            {
                return Task.FromException(caught);
            }
        }

        Task PublishClosed()
        {
            try
            {
                return _settings.Router?.PublishCircuitBreakerClosed() ?? Task.CompletedTask;
            }
            catch (Exception caught)
            {
                return Task.FromException(caught);
            }
        }
    }
}
