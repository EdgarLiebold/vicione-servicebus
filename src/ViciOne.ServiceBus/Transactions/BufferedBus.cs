namespace ViciOne.ServiceBus.Transactions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;


    internal sealed class BufferedBus :
        DeferredBus,
        IBufferedBus
    {
        readonly SemaphoreSlim _flushLock;
        readonly AsyncLocal<FlushFrame> _flushFrame;
        readonly object _lock;
        readonly Queue<Func<CancellationToken, Task>> _pendingActions;

        public BufferedBus(IBus bus)
            : base(bus)
        {
            _flushLock = new SemaphoreSlim(1, 1);
            _flushFrame = new AsyncLocal<FlushFrame>();
            _lock = new object();
            _pendingActions = new Queue<Func<CancellationToken, Task>>();
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_flushFrame.Value?.IsActive == true)
            {
                throw new InvalidOperationException(
                    "FlushAsync cannot be called recursively from an action being flushed by the same buffered bus.");
            }

            await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Func<CancellationToken, Task>[] actions;
                lock (_lock)
                {
                    actions = _pendingActions.ToArray();
                    _pendingActions.Clear();
                }

                for (var index = 0; index < actions.Length; index++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        RestoreUnattempted(actions, index);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    try
                    {
                        FlushFrame inheritedFrame = _flushFrame.Value;
                        var frame = new FlushFrame();
                        _flushFrame.Value = frame;
                        try
                        {
                            await actions[index](cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            frame.Complete();
                            _flushFrame.Value = inheritedFrame;
                        }
                    }
                    catch
                    {
                        RestoreUnattempted(actions, index + 1);
                        throw;
                    }
                }
            }
            finally
            {
                _flushLock.Release();
            }
        }

        internal override Task Add(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            cancellationToken.ThrowIfCancellationRequested();

            lock (_lock)
                _pendingActions.Enqueue(action);

            return Task.CompletedTask;
        }

        void RestoreUnattempted(Func<CancellationToken, Task>[] actions, int firstUnattempted)
        {
            if (firstUnattempted >= actions.Length)
                return;

            lock (_lock)
            {
                Func<CancellationToken, Task>[] addedDuringFlush = _pendingActions.ToArray();
                _pendingActions.Clear();

                for (var index = firstUnattempted; index < actions.Length; index++)
                    _pendingActions.Enqueue(actions[index]);

                foreach (Func<CancellationToken, Task> action in addedDuringFlush)
                    _pendingActions.Enqueue(action);
            }
        }

        sealed class FlushFrame
        {
            int _active = 1;

            public bool IsActive => Volatile.Read(ref _active) != 0;

            public void Complete()
            {
                Volatile.Write(ref _active, 0);
            }
        }
    }
}
