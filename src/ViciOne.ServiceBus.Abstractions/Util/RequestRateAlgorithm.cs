using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Calculates request rate values.</summary>
public class RequestRateAlgorithm :
    IDisposable
{
    /// <summary>Represents the method that handles group callback.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TKey">The key used for lookup.</typeparam>
    /// <param name="results">The results.</param>
    /// <returns>The value produced by the operation.</returns>
    public delegate IEnumerable<IGrouping<TKey, T>> GroupCallback<T, out TKey>(IEnumerable<T> results);


    /// <summary>Represents the method that handles order callback.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="results">The results.</param>
    /// <returns>The value produced by the operation.</returns>
    public delegate IEnumerable<T> OrderCallback<T>(IEnumerable<T> results);


    /// <summary>Represents the method that handles request callback.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultLimit">The result limit.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public delegate Task<IEnumerable<T>> RequestCallback<T>(int resultLimit, CancellationToken cancellationToken);


    /// <summary>Represents the method that handles request callback.</summary>
    /// <param name="resultLimit">The result limit.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public delegate Task<int> RequestCallback(int resultLimit, CancellationToken cancellationToken);


    /// <summary>Represents the method that handles result callback.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public delegate Task ResultCallback<in T>(T result, CancellationToken cancellationToken);


    readonly int _concurrentResultLimit;
    readonly CancellationTokenSource _disposeToken;
    readonly RequestRateAlgorithmOptions _options;
    readonly SemaphoreSlim? _rateLimitSemaphore;
    readonly ITimer? _rateLimitTimer;
    readonly int _refreshThreshold;
    readonly TimeSpan _requestCancellationTimeout;
    readonly int _requestLimit;
    readonly object _requestLock = new object();
    readonly SemaphoreSlim _requestSemaphore;
    readonly int _resultLimit;
    readonly SemaphoreSlim _resultSemaphore;
    readonly ConcurrentDictionary<long, Task> _tasks;
    readonly TimeProvider _timeProvider;

    int _activeRequestCount;
    int _count;
    bool _disposed;
    int _maxRequestCount;
    long _nextId;
    int _pendingResultCount;
    int _rateLimit;
    int _requestCount;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public RequestRateAlgorithm(RequestRateAlgorithmOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.PrefetchCount <= 0)
            throw new ArgumentException("PrefetchCount must be > 0", nameof(options));
        if (options.RequestResultLimit <= 0)
            throw new ArgumentException("RequestResultLimit must be > 0", nameof(options));
        if (options.ConcurrentResultLimit is <= 0)
            throw new ArgumentException("ConcurrentResultLimit must be > 0 when specified", nameof(options));
        if (options.RequestRateLimit.HasValue != options.RequestRateInterval.HasValue)
            throw new ArgumentException("RequestRateLimit and RequestRateInterval must either both be specified or both be omitted", nameof(options));
        if (options.RequestRateLimit is <= 0)
            throw new ArgumentException("RequestRateLimit must be > 0 when specified", nameof(options));
        if (options.RequestRateInterval.HasValue && options.RequestRateInterval.Value <= TimeSpan.Zero)
            throw new ArgumentException("RequestRateInterval must be > TimeSpan.Zero when specified", nameof(options));
        if (options.RequestCancellationTimeout.HasValue && options.RequestCancellationTimeout.Value <= TimeSpan.Zero)
            throw new ArgumentException("RequestCancellationTimeout must be > TimeSpan.Zero when specified", nameof(options));

        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _requestCancellationTimeout = _options.RequestCancellationTimeout ?? TimeSpan.FromSeconds(1);

        _disposeToken = new CancellationTokenSource();
        _requestCount = 1;
        _requestSemaphore = new SemaphoreSlim(_requestCount);

        _requestLimit = (_options.PrefetchCount + _options.RequestResultLimit - 1) / _options.RequestResultLimit;

        _resultLimit = Math.Min(_options.PrefetchCount, _options.RequestResultLimit);
        _concurrentResultLimit = options.ConcurrentResultLimit ?? _requestLimit * _resultLimit;

        _refreshThreshold = 1;

        _resultSemaphore = new SemaphoreSlim(_concurrentResultLimit);

        _tasks = new ConcurrentDictionary<long, Task>();

        if (options is { RequestRateLimit: not null, RequestRateInterval: not null })
        {
            _rateLimit = options.RequestRateLimit.Value;
            _rateLimitSemaphore = new SemaphoreSlim(_rateLimit);

            var interval = options.RequestRateInterval.Value;
            _rateLimitTimer = _timeProvider.CreateTimer(Reset, null, interval, interval);
        }
    }

    /// <summary>The number of concurrent requests that should be performed based upon current response volume.</summary>
    public int RequestCount => _requestCount;

    /// <summary>The number of results that should be requested for each request.</summary>
    public int ResultLimit => _resultLimit;

    /// <summary>The current active request count.</summary>
    public int ActiveRequestCount => _activeRequestCount;

    /// <summary>The maximum number of active requests that were made concurrently.</summary>
    public int MaxActiveRequestCount => _maxRequestCount;

    int ActiveResultCount => _tasks.Count;

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _rateLimitTimer?.Dispose();
        _rateLimitSemaphore?.Dispose();

        _requestSemaphore.Dispose();
        _resultSemaphore.Dispose();
    }

    /// <summary>Run a series of requests, up the limits, as a single pass.</summary>
    /// <param name="requestCallback">The request callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the run outcome.</returns>
    public async Task<int> RunAsync(RequestCallback requestCallback, CancellationToken cancellationToken = default)
    {
        var requestCount = _requestCount;

        var tasks = new List<Task<int>>(requestCount);

        try
        {
            for (var i = 0; i < requestCount; i++)
                tasks.Add(RunRequestAsync(requestCallback, cancellationToken));
        }
        catch (Exception)
        {
            if (tasks.Count == 0)
                throw;
        }

        var counts = await Task.WhenAll(tasks).ConfigureAwait(false);

        return counts.Sum();
    }

    async Task<int> RunRequestAsync(RequestCallback requestCallback, CancellationToken cancellationToken = default)
    {
        using var activeRequest = await BeginRequestAsync(cancellationToken).ConfigureAwait(false);

        var count = await requestCallback(activeRequest.ResultLimit, activeRequest.CancellationToken).ConfigureAwait(false);

        await activeRequest.CompleteAsync(count, CancellationToken.None).ConfigureAwait(false);

        return count;
    }

    /// <summary>Run a series of requests, up the limits, as a single pass.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestCallback">The request callback.</param>
    /// <param name="resultCallback">The result callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the run outcome.</returns>
    public async Task<int> RunAsync<T>(RequestCallback<T> requestCallback, ResultCallback<T> resultCallback, CancellationToken cancellationToken = default)
    {
        var requestCount = _requestCount;

        var tasks = new List<Task<int>>(requestCount);

        try
        {
            for (var i = 0; i < requestCount; i++)
                tasks.Add(RunRequestAsync(requestCallback, resultCallback, cancellationToken));
        }
        catch (Exception)
        {
            if (tasks.Count == 0)
                throw;
        }

        var counts = await Task.WhenAll(tasks).ConfigureAwait(false);

        return counts.Sum();
    }

    async Task<int> RunRequestAsync<T>(RequestCallback<T> requestCallback, ResultCallback<T> resultCallback, CancellationToken cancellationToken = default)
    {
        using var activeRequest = await BeginRequestAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<T> results = await requestCallback(activeRequest.ResultLimit, activeRequest.CancellationToken).ConfigureAwait(false);

        var count = 0;
        try
        {
            foreach (var result in results)
            {
                await _resultSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                async Task RunResultCallbackAsync()
                {
                    try
                    {
                        await resultCallback(result, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (!_disposed)
                            _resultSemaphore.Release();
                    }
                }

                Add(Task.Run(() => RunResultCallbackAsync(), cancellationToken));
                count++;
            }
        }
        catch (Exception)
        {
            if (count == 0)
                throw;
        }

        await activeRequest.CompleteAsync(count, CancellationToken.None).ConfigureAwait(false);

        return count;
    }

    /// <summary>Run a series of requests, up the limits, as a single pass.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TKey">The key used for lookup.</typeparam>
    /// <param name="requestCallback">The request callback.</param>
    /// <param name="resultCallback">The result callback.</param>
    /// <param name="groupCallback">The group callback.</param>
    /// <param name="orderCallback">The order callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the run outcome.</returns>
    public async Task<int> RunAsync<T, TKey>(RequestCallback<T> requestCallback, ResultCallback<T> resultCallback, GroupCallback<T, TKey> groupCallback,
        OrderCallback<T> orderCallback, CancellationToken cancellationToken = default)
    {
        var requestCount = _requestCount;

        var tasks = new List<Task<IReadOnlyList<T>>>(requestCount);

        try
        {
            for (var i = 0; i < requestCount; i++)
                tasks.Add(Task.Run(() => RunRequestAsync(requestCallback, cancellationToken), cancellationToken));
        }
        catch (Exception)
        {
            if (tasks.Count == 0)
                throw;
        }

        IReadOnlyList<T>[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        List<IGrouping<TKey, T>> resultSets = groupCallback(results.SelectMany(x => x)).ToList();

        var resultTasks = new List<Task<int>>(ResultLimit);

        try
        {
            foreach (IGrouping<TKey, T> result in resultSets)
                resultTasks.Add(Task.Run(() => RunResultSetAsync(result, resultCallback, orderCallback, cancellationToken), cancellationToken));
        }
        catch (Exception)
        {
            if (resultTasks.Count == 0)
                throw;
        }

        var counts = await Task.WhenAll(resultTasks).ConfigureAwait(false);

        return counts.Sum();
    }

    async Task<IReadOnlyList<T>> RunRequestAsync<T>(RequestCallback<T> requestCallback, CancellationToken cancellationToken = default)
    {
        using var activeRequest = await BeginRequestAsync(cancellationToken).ConfigureAwait(false);

        List<T> results = (await requestCallback(activeRequest.ResultLimit, activeRequest.CancellationToken).ConfigureAwait(false)).ToList();

        await activeRequest.CompleteAsync(results.Count, CancellationToken.None).ConfigureAwait(false);

        return results;
    }

    async Task<int> RunResultSetAsync<TKey, T>(IGrouping<TKey, T> results, ResultCallback<T> resultCallback, OrderCallback<T> orderCallback,
        CancellationToken cancellationToken = default)
    {
        var count = 0;

        try
        {
            foreach (var result in orderCallback(results))
            {
                await _resultSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await resultCallback(result, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _resultSemaphore.Release();
                }
                count++;
            }
        }
        catch (Exception)
        {
            if (count == 0)
                throw;
        }

        return count;
    }

    /// <summary>Begins tracking the request.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the begin request outcome.</returns>
    public async Task<ActiveRequest> BeginRequestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeToken.Token);

            await _requestSemaphore.WaitAsync(linked.Token).ConfigureAwait(false);

            if (_rateLimitSemaphore != null)
            {
                await _rateLimitSemaphore.WaitAsync(linked.Token).ConfigureAwait(false);

                Interlocked.Increment(ref _count);
            }

            var current = Interlocked.Increment(ref _activeRequestCount);
            while (current > _maxRequestCount)
                Interlocked.CompareExchange(ref _maxRequestCount, current, _maxRequestCount);

            int resultLimit;
            lock (_requestLock)
            {
                resultLimit = Math.Min(_concurrentResultLimit - ActiveResultCount - _pendingResultCount, ResultLimit);
                while (resultLimit < _refreshThreshold)
                {
                    Monitor.Wait(_requestLock, 100);

                    if (cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();

                    resultLimit = Math.Min(_concurrentResultLimit - ActiveResultCount - _pendingResultCount, ResultLimit);
                }

                _pendingResultCount += resultLimit;
            }

            return new ActiveRequest(this, resultLimit, cancellationToken, _requestCancellationTimeout, _timeProvider);
        }
        catch (OperationCanceledException)
        {
            if (!_disposed)
                _requestSemaphore.Release();

            throw;
        }
    }

    internal Task EndRequestAsync(int count, int resultLimit, CancellationToken cancellationToken = default)
    {
        Interlocked.Decrement(ref _activeRequestCount);

        lock (_requestLock)
        {
            _pendingResultCount -= resultLimit;

            Monitor.PulseAll(_requestLock);
        }

        if (_disposed)
            return Task.CompletedTask;

        _requestSemaphore.Release();

        var currentRequestCount = _requestCount;

        var requestCount = count >= _options.RequestResultLimit
            ? Math.Min(_requestLimit, currentRequestCount + (_requestLimit - currentRequestCount + 1) / 2)
            : Math.Max(1, currentRequestCount - currentRequestCount / 2);

        if (requestCount != currentRequestCount)
        {
            var previousValue = Interlocked.CompareExchange(ref _requestCount, requestCount, currentRequestCount);

            if (previousValue == currentRequestCount)
                return ChangeRequestCountAsync(requestCount, currentRequestCount, cancellationToken);
        }

        return Task.CompletedTask;
    }

    internal void CancelRequest(int resultLimit)
    {
        Interlocked.Decrement(ref _activeRequestCount);

        lock (_requestLock)
        {
            _pendingResultCount -= resultLimit;

            Monitor.PulseAll(_requestLock);
        }

        if (_disposed)
            return;

        _requestSemaphore.Release();
    }

    /// <summary>Changes rate limit.</summary>
    /// <param name="newRateLimit">The new rate limit.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ChangeRateLimitAsync(int newRateLimit, CancellationToken cancellationToken = default)
    {
        if (newRateLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(newRateLimit), "The rate limit must be >= 1");

        if (_rateLimitSemaphore == null)
            throw new InvalidOperationException("Rate limit can only be changed when an original rate limit was specified.");

        if (_disposed)
            throw new ObjectDisposedException("The RequestRateAlgorithm was disposed");

        var previousLimit = _rateLimit;
        if (newRateLimit > previousLimit)
        {
            var releaseCount = newRateLimit - previousLimit;

            _rateLimitSemaphore.Release(releaseCount);

            Interlocked.Add(ref _rateLimit, releaseCount);
        }
        else
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeToken.Token);

            for (; previousLimit > newRateLimit; previousLimit--)
            {
                await _rateLimitSemaphore.WaitAsync(linked.Token).ConfigureAwait(false);

                Interlocked.Decrement(ref _rateLimit);
            }
        }
    }

    async Task ChangeRequestCountAsync(int newRequestCount, int currentRequestCount, CancellationToken cancellationToken = default)
    {
        if (newRequestCount < 1 || newRequestCount > _requestLimit)
            throw new ArgumentOutOfRangeException(nameof(newRequestCount), $"The request count {newRequestCount} must be >= 1 and <= {_requestLimit}");

        var previousRequestCount = currentRequestCount;
        if (newRequestCount > previousRequestCount)
        {
            var releaseCount = newRequestCount - previousRequestCount;

            _requestSemaphore.Release(releaseCount);

            Interlocked.Add(ref _rateLimit, releaseCount);
        }
        else
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeToken.Token);

            for (; previousRequestCount > newRequestCount; previousRequestCount--)
            {
                await _requestSemaphore.WaitAsync(linked.Token).ConfigureAwait(false);

                Interlocked.Decrement(ref _rateLimit);
            }
        }
    }

    void Reset(object? state)
    {
        var processed = Interlocked.Exchange(ref _count, 0);
        if (processed > 0)
            _rateLimitSemaphore!.Release(processed);
    }

    void Add(Task task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (task.Status == TaskStatus.RanToCompletion)
            return;

        var id = Interlocked.Increment(ref _nextId);

        if (_tasks.TryAdd(id, task))
            task.ContinueWith(_ => Remove(id), TaskContinuationOptions.ExecuteSynchronously);
    }

    void Remove(long id)
    {
        _tasks.TryRemove(id, out _);

        var remaining = _tasks.Count;
        if (remaining > 0)
            return;

        lock (_requestLock)
            Monitor.PulseAll(_requestLock);
    }
}
