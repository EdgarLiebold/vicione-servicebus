using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Stores a collection of pending fault values.</summary>
public class PendingFaultCollection
{
    readonly List<IPendingFault> _pendingFaults;

    /// <summary>Initializes a new instance.</summary>
    public PendingFaultCollection()
    {
        _pendingFaults = new List<IPendingFault>();
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="elapsed">The elapsed.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add<T>(ConsumeContext<T> context, TimeSpan elapsed, string consumerType, Exception exception)
        where T : class
    {
        var pendingFault = new PendingFault<T>(context, elapsed, consumerType, exception);

        lock (_pendingFaults)
            _pendingFaults.Add(pendingFault);
    }

    /// <summary>Notifies the registered observers.</summary>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task NotifyAsync(ConsumeContext consumeContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); IPendingFault[] pendingFaults;
        do
        {
            lock (_pendingFaults)
            {
                if (_pendingFaults.Count == 0)
                    return;

                pendingFaults = _pendingFaults.ToArray();

                _pendingFaults.Clear();
            }

            await Task.WhenAll(pendingFaults.Select(x => x.NotifyAsync(consumeContext))).ConfigureAwait(false);
        }
        while (pendingFaults.Length > 0);
    }


    interface IPendingFault
    {
        Task NotifyAsync(ConsumeContext context);
    }


    class PendingFault<T> :
        IPendingFault
        where T : class
    {
        readonly string _consumerType;
        readonly ConsumeContext<T> _context;
        readonly TimeSpan _elapsed;
        readonly Exception _exception;

        public PendingFault(ConsumeContext<T> context, TimeSpan elapsed, string consumerType, Exception exception)
        {
            _context = context;
            _elapsed = elapsed;
            _consumerType = consumerType;
            _exception = exception;
        }

        public Task NotifyAsync(ConsumeContext context)
        {
            return context.NotifyFaultedAsync(_context, _elapsed, _consumerType, _exception);
        }
    }
}
