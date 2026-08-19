namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using Transports;


    /// <summary>
    /// Whether one receive endpoint went through a recovery sequence, in order.
    /// <para>
    /// Recovery is a sequence, not two independent signals. Resetting both and waiting for either
    /// lets a Ready that arrives before the Fault satisfy the recovery: the endpoint that was
    /// already ready reports it again, the case sees Ready, and nothing was recovered. So this
    /// carries a state rather than two latches - a Ready is only the recovery when it is observed
    /// after the Fault that this outage caused.
    /// </para>
    /// </summary>
    sealed class RecoverySequenceObserver :
        IReceiveEndpointObserver
    {
        readonly object _gate = new();
        readonly string _endpoint;
        TaskCompletionSource<bool> _faulted = Fresh();
        TaskCompletionSource<bool> _recovered = Fresh();
        bool _watching;
        bool _faultSeen;

        /// <summary>
        /// Binds this observer to one receive endpoint, by the last segment of its input address.
        /// <para>
        /// A bus has more than one receive endpoint - the harness adds its own, and a fault handler
        /// gets one too - and every one of them reports to every observer. Without this binding a
        /// Ready from an endpoint that has nothing to do with the outage completes the recovery of the
        /// endpoint that was actually taken away.
        /// </para>
        /// </summary>
        public RecoverySequenceObserver(string endpoint)
        {
            _endpoint = endpoint;
        }

        bool Mine(ReceiveEndpointEvent observed)
        {
            var address = observed.InputAddress;
            if (address is null)
                return false;

            var segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            return segments.Length > 0 && segments[^1] == _endpoint;
        }

        /// <summary>
        /// Starts watching one outage. Everything before this - including the readiness of the
        /// start - is forgotten, and no Ready counts until a Fault has been seen.
        /// </summary>
        public void Watch()
        {
            lock (_gate)
            {
                _faulted = Fresh();
                _recovered = Fresh();
                _watching = true;
                _faultSeen = false;
            }
        }

        public Task<bool> Faulted(TimeSpan budget)
        {
            Task<bool> signal;
            lock (_gate)
                signal = _faulted.Task;

            return Within(signal, budget);
        }

        public Task<bool> ReadyAfterTheFault(TimeSpan budget)
        {
            Task<bool> signal;
            lock (_gate)
                signal = _recovered.Task;

            return Within(signal, budget);
        }

        static async Task<bool> Within(Task<bool> signal, TimeSpan budget)
        {
            return await Task.WhenAny(signal, Task.Delay(budget)) == signal;
        }

        static TaskCompletionSource<bool> Fresh()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task Ready(ReceiveEndpointReady ready)
        {
            if (!Mine(ready))
                return Task.CompletedTask;

            lock (_gate)
            {
                // A Ready before the Fault is the endpoint that never went away. It is not a
                // recovery and it may not complete one.
                if (_watching && _faultSeen)
                    _recovered.TrySetResult(true);
            }

            return Task.CompletedTask;
        }

        public Task Stopping(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task Completed(ReceiveEndpointCompleted completed) => Task.CompletedTask;

        public Task Faulted(ReceiveEndpointFaulted faulted)
        {
            if (!Mine(faulted))
                return Task.CompletedTask;

            lock (_gate)
            {
                if (!_watching)
                    return Task.CompletedTask;

                _faultSeen = true;
                _faulted.TrySetResult(true);
            }

            return Task.CompletedTask;
        }
    }
}
