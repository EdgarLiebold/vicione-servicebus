namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using Transports;


    /// <summary>
    /// What the receive endpoint said about itself, in order.
    /// <para>
    /// Recovery is a sequence, not two independent signals. Resetting both and waiting for either
    /// lets a Ready that arrives before the Fault satisfy the recovery: the endpoint that was
    /// already ready reports it again, the case sees Ready, and nothing was recovered. So this
    /// carries a state rather than two latches - a Ready is only the recovery when it is observed
    /// after the Fault that this outage caused.
    /// </para>
    /// </summary>
    sealed class EndpointStateObserver :
        IReceiveEndpointObserver
    {
        readonly object _gate = new();
        TaskCompletionSource<bool> _faulted = Fresh();
        TaskCompletionSource<bool> _recovered = Fresh();
        bool _watching;
        bool _faultSeen;

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
