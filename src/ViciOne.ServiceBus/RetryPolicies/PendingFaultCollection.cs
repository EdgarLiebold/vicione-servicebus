namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Collects consumer faults until the owning consume attempt is ready to notify observers.</summary>
internal sealed class PendingFaultCollection
{
    private readonly List<IPendingFault> _pendingFaults = [];
    private bool _notificationStarted;

    /// <summary>Adds a typed consumer fault before notification begins.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The typed context whose consumer faulted.</param>
    /// <param name="elapsed">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <param name="exception">The consumer exception.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="exception" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elapsed" /> is negative.</exception>
    /// <exception cref="ArgumentException"><paramref name="consumerType" /> is empty or consists only of white-space characters.</exception>
    /// <exception cref="InvalidOperationException">Fault notification has already started.</exception>
    public void Add<TMessage>(ConsumeContext<TMessage> context, TimeSpan elapsed, string consumerType,
        Exception exception)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "The elapsed duration cannot be negative.");
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);

        var pendingFault = new PendingFault<TMessage>(context, elapsed, consumerType, exception);

        lock (_pendingFaults)
        {
            if (_notificationStarted)
                throw new InvalidOperationException("Consumer faults cannot be added after notification has started.");

            _pendingFaults.Add(pendingFault);
        }
    }

    /// <summary>Notifies the owning context of every collected consumer fault.</summary>
    /// <param name="consumeContext">The context that owns the observer pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the notifications.</param>
    /// <returns>A task that completes after every pending fault notification completes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="consumeContext" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Notification has already started.</exception>
    public async Task NotifyAsync(ConsumeContext consumeContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        cancellationToken.ThrowIfCancellationRequested();

        IPendingFault[] pendingFaults;
        lock (_pendingFaults)
        {
            if (_notificationStarted)
                throw new InvalidOperationException("Consumer fault notification can only be started once.");

            _notificationStarted = true;
            pendingFaults = [.. _pendingFaults];
            _pendingFaults.Clear();
        }

        await Task.WhenAll(pendingFaults.Select(fault => NotifySafelyAsync(fault, consumeContext, cancellationToken)))
            .ConfigureAwait(false);
    }

    private static Task NotifySafelyAsync(IPendingFault fault, ConsumeContext context, CancellationToken cancellationToken)
    {
        try
        {
            return fault.NotifyAsync(context, cancellationToken)
                ?? Task.FromException(new InvalidOperationException("A consumer fault notification returned no task."));
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }


    interface IPendingFault
    {
        Task NotifyAsync(ConsumeContext context, CancellationToken cancellationToken);
    }


    private sealed class PendingFault<TMessage> :
        IPendingFault
        where TMessage : class
    {
        private readonly string _consumerType;
        private readonly ConsumeContext<TMessage> _context;
        private readonly TimeSpan _elapsed;
        private readonly Exception _exception;

        public PendingFault(ConsumeContext<TMessage> context, TimeSpan elapsed, string consumerType,
            Exception exception)
        {
            _context = context;
            _elapsed = elapsed;
            _consumerType = consumerType;
            _exception = exception;
        }

        public Task NotifyAsync(ConsumeContext context, CancellationToken cancellationToken)
        {
            return context.NotifyFaultedAsync(_context, _elapsed, _consumerType, _exception, cancellationToken);
        }
    }
}
