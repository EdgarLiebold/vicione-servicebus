using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides an active request implementation.
/// </summary>
public sealed class ActiveRequest :
    IDisposable
{
    readonly RequestRateAlgorithm _algorithm;
    readonly CancellationTokenRegistration _registration;
    readonly CancellationTokenSource _source;
    readonly TimeProvider _timeProvider;
    readonly TimeSpan _timeout;
    ITimer? _cancelTimer;
    bool _completed;
    int _disposed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="algorithm">The algorithm value.</param>
    /// <param name="resultLimit">The result limit value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ActiveRequest(RequestRateAlgorithm algorithm, int resultLimit, CancellationToken cancellationToken, TimeSpan timeout,
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

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public CancellationToken CancellationToken { get; }
    /// <summary>
    /// Gets the result limit value.
    /// </summary>
    public int ResultLimit { get; }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompleteAsync(int count, CancellationToken cancellationToken = default)
    {
        _completed = true;

        return _algorithm.EndRequestAsync(count, ResultLimit, cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _registration.Dispose();
        _cancelTimer?.Dispose();
        _source.Dispose();

        if (_completed)
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
