using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Carries the request for active.</summary>
public sealed class ActiveRequest :
    IDisposable
{
    readonly RequestRateAlgorithm _algorithm;
    readonly CancellationTokenRegistration _registration;
    readonly CancellationTokenSource _source;
    readonly TimeProvider _timeProvider;
    readonly TimeSpan _timeout;
    ITimer? _cancelTimer;
    int _disposed;
    int _settlement;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="algorithm">The algorithm.</param>
    /// <param name="resultLimit">The result limit.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    internal ActiveRequest(RequestRateAlgorithm algorithm, int resultLimit, CancellationToken cancellationToken, TimeSpan timeout,
        TimeProvider timeProvider)
    {
        _algorithm = algorithm;
        _timeout = timeout;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _source = new CancellationTokenSource();

        CancellationToken = _source.Token;
        ResultLimit = resultLimit;

        _registration = cancellationToken.Register(static state => ((ActiveRequest)state!).ScheduleCancellation(), this);
    }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken { get; }
    /// <summary>Gets the result limit.</summary>
    public int ResultLimit { get; }

    /// <summary>Marks the current operation as complete.</summary>
    /// <param name="count">The count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompleteAsync(int count, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(ActiveRequest));

        int previous = Interlocked.CompareExchange(ref _settlement, 1, 0);
        if (previous == 1)
            throw new InvalidOperationException("The request has already been completed.");
        if (previous == 2)
            throw new ObjectDisposedException(nameof(ActiveRequest));

        return _algorithm.EndRequestAsync(count, ResultLimit, cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _registration.Dispose();
        _cancelTimer?.Dispose();
        _source.Dispose();

        if (Interlocked.CompareExchange(ref _settlement, 2, 0) != 0)
            return;

        _algorithm.CancelRequest(ResultLimit);
    }

    void ScheduleCancellation()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        if (_timeout <= TimeSpan.Zero)
        {
            Cancel();
            return;
        }

        var timer = _timeProvider.CreateTimer(static state => ((ActiveRequest)state!).Cancel(), this, _timeout, Timeout.InfiniteTimeSpan);
        var previous = Interlocked.CompareExchange(ref _cancelTimer, timer, null);
        if (previous != null)
            timer.Dispose();
    }

    void Cancel()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        try
        {
            _source.Cancel();
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0)
        {
        }
    }
}
