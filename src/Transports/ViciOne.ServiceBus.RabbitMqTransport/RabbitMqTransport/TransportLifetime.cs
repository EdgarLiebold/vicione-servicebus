namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;


    /// <summary>
    /// Owns one channel or one connection and separates saying it is finished from taking it away.
    /// <para>
    /// The two used to be the same act, and that cost the broker's answer. When RabbitMQ refuses an
    /// operation it completes that operation's continuation first — with the real reply code — and only
    /// then raises its shutdown notification. The old handler disposed from inside that notification,
    /// while the refused caller was still unwinding and had yet to release the client's own RPC
    /// semaphore in its finally. Disposing underneath it turned a broker's answer into an
    /// ObjectDisposedException, and a permanent refusal was retried as though it were a hiccup. Measured
    /// against the pinned broker: five declares, five refusals with reply code 405, and four of them
    /// replaced before anything could read them.
    /// </para>
    /// <para>
    /// So an operation takes a lease for as long as it runs, invalidation is immediate and never waits,
    /// and the subject is disposed only once the last lease is gone — after the finally blocks that
    /// still touch it. Callers arriving after invalidation are told the reason it closed, unaltered and
    /// typed, rather than being handed something that is about to vanish.
    /// </para>
    /// <para>
    /// One type serves both channel and connection deliberately. Two ownership models that differ in
    /// their details are two chances to get the order wrong, and the order is the entire subject here.
    /// </para>
    /// </summary>
    internal sealed class TransportLifetime :
        IAsyncDisposable
    {
        readonly Func<Task> _disposeSubject;
        readonly object _lock = new object();
        readonly string _subject;

        /// <summary>
        /// The one authoritative completion: it completes when the subject is really gone, carrying the
        /// disposal's failure if it had one and null if it did not. Everything that waits waits on this,
        /// and there is deliberately no second representation of the same fact to disagree with it.
        /// <para>
        /// A result rather than a faulted task on purpose: disposal is started by whoever released the
        /// last lease, which is usually nobody who will ever await it, and a faulted task nobody awaits
        /// is an unobserved exception waiting to surface somewhere unrelated. The failure is logged
        /// where it happens and rethrown, with its stack, to an owner that asks.
        /// </para>
        /// </summary>
        readonly TaskCompletionSource<Exception> _disposed =
            new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        ShutdownEventArgs _closeReason;
        int _active;
        int _disposeStarted;
        bool _invalidated;

        /// <summary>
        /// The disposal is injected rather than performed here, so the ownership rules can be proved
        /// deterministically without standing up a broker: a probe records when disposal starts, how
        /// often, and what happens when it fails, which is exactly what these rules are about.
        /// </summary>
        /// <param name="subject">What is owned — used only to say honestly what is no longer available.</param>
        /// <param name="disposeSubject">Disposes the owned subject, exactly once, once nothing holds it.</param>
        public TransportLifetime(string subject, Func<Task> disposeSubject)
        {
            _subject = subject;
            _disposeSubject = disposeSubject;
        }

        /// <summary>The broker's own reason for closing, or null while the subject is open.</summary>
        public ShutdownEventArgs CloseReason
        {
            get
            {
                lock (_lock)
                    return _closeReason;
            }
        }

        /// <summary>
        /// Takes a lease for one operation, or refuses because the subject is finished.
        /// <para>
        /// Refusal is not an error of this method: the caller decides what to do, and for a real
        /// operation that means reporting the stored close reason rather than inventing one.
        /// </para>
        /// </summary>
        public bool TryLease(out Lease lease)
        {
            lock (_lock)
            {
                if (_invalidated || Volatile.Read(ref _disposeStarted) != 0)
                {
                    lease = null;
                    return false;
                }

                _active++;
            }

            lease = new Lease(this);
            return true;
        }

        /// <summary>
        /// The exception a caller gets when the subject is finished: the broker's own answer if there is
        /// one, and otherwise this transport's own, which says only what it can honestly say.
        /// </summary>
        public OperationInterruptedException NotAvailable()
        {
            var reason = CloseReason;

            return reason != null
                ? new OperationInterruptedException(reason)
                : new OperationInterruptedException(
                    new ShutdownEventArgs(ShutdownInitiator.Library, 491, $"The {_subject} is no longer available"));
        }

        /// <summary>
        /// Marks the subject finished and keeps the reason. Never blocks and never disposes inline: this
        /// is called from the client's shutdown notification, and waiting there would hold the very
        /// callback the disposal is waiting on.
        /// </summary>
        public void Invalidate(ShutdownEventArgs reason)
        {
            bool idle;

            lock (_lock)
            {
                if (_closeReason == null && reason != null)
                    _closeReason = reason;

                _invalidated = true;
                idle = _active == 0;
            }

            if (idle)
                ScheduleDispose();
        }

        /// <summary>
        /// Invalidates and then waits for the subject to be really gone, rethrowing a disposal failure
        /// to the owner that asked for it. Used by the owner's own disposal, where waiting is correct —
        /// unlike in the shutdown notification.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            Invalidate(null);

            var failure = await _disposed.Task.ConfigureAwait(false);

            if (failure != null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }

        void Release()
        {
            bool idle;

            lock (_lock)
            {
                _active--;
                idle = _invalidated && _active == 0;
            }

            if (idle)
                ScheduleDispose();
        }

        void ScheduleDispose()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
                return;

            // Off the caller's thread on purpose: when this comes from the client's shutdown
            // notification, the notification has to return before anything disposes the subject it is
            // notifying about.
            //
            // The task is discarded explicitly rather than by accident. DisposeSubject catches every
            // exception and reports the outcome through _disposed, which DisposeAsync awaits and
            // rethrows from, so the discarded task carries no outcome that awaiting could recover.
            ThreadPool.QueueUserWorkItem(state => { _ = DisposeSubject(); });
        }

        /// <summary>
        /// Disposes the subject once and reports the outcome through <see cref="_disposed" />. It catches
        /// everything on purpose: nothing awaits the task this returns, so a failure left in it would be
        /// an unobserved exception rather than a report.
        /// </summary>
        async Task DisposeSubject()
        {
            Exception failure = null;

            try
            {
                await _disposeSubject().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failure = exception;

                LogContext.Error?.Log(exception, "Disposing the {Subject} faulted after its last operation finished", _subject);
            }

            _disposed.TrySetResult(failure);
        }


        /// <summary>
        /// One running operation's claim on the subject, released exactly once however often it is
        /// disposed. A class rather than a struct because a struct is copied silently, and a copy
        /// released twice would take the active count below zero and let disposal begin underneath an
        /// operation that is still running — the very defect this type exists to prevent.
        /// </summary>
        public sealed class Lease :
            IDisposable
        {
            readonly TransportLifetime _lifetime;
            int _released;

            internal Lease(TransportLifetime lifetime)
            {
                _lifetime = lifetime;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                    _lifetime.Release();
            }
        }
    }
}
