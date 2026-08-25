namespace ViciOne.ServiceBus.Middleware.CircuitBreaker
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Represents a circuit that is unavailable, with a timer waiting to partially close
    /// the circuit.
    /// </summary>
    internal sealed class OpenBehavior :
        ICircuitBreakerBehavior
    {
        readonly ICircuitBreaker _breaker;
        readonly Exception _exception;
        readonly long _openedTimestamp;
        readonly IEnumerator<TimeSpan> _timeoutEnumerator;
        readonly ITimer _timer;

        public OpenBehavior(ICircuitBreaker breaker, Exception exception, IEnumerator<TimeSpan> timeoutEnumerator)
        {
            _breaker = breaker;
            _exception = exception;
            _timeoutEnumerator = timeoutEnumerator;

            _timer = GetTimer(timeoutEnumerator);
            _openedTimestamp = breaker.TimeProvider.GetTimestamp();
        }

        Task ICircuitBreakerBehavior.PreSend()
        {
            throw _exception;
        }

        Task ICircuitBreakerBehavior.PostSend()
        {
            return Task.CompletedTask;
        }

        Task ICircuitBreakerBehavior.SendFault(Exception exception)
        {
            return Task.CompletedTask;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            var timeout = _timeoutEnumerator.Current;
            context.Set(new
            {
                State = "open",
                Exception = _exception,
                Timeout = timeout,
                Remaining = timeout - _breaker.TimeProvider.GetElapsedTime(_openedTimestamp)
            });
        }

        ITimer GetTimer(IEnumerator<TimeSpan> timeoutEnumerator)
        {
            timeoutEnumerator.MoveNext();

            return _breaker.TimeProvider.CreateTimer(PartiallyCloseCircuit, this, timeoutEnumerator.Current, Timeout.InfiniteTimeSpan);
        }

        void PartiallyCloseCircuit(object state)
        {
            _breaker.ClosePartially(_exception, _timeoutEnumerator, this);
            _timer.Dispose();
        }
    }
}
