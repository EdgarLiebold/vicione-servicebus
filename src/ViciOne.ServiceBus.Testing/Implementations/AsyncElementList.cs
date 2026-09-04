using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides an async element list implementation.
/// </summary>
/// <typeparam name="TElement">The t element type.</typeparam>
public abstract class AsyncElementList<TElement> :
    IAsyncElementList<TElement>,
    ITestContextRetention
    where TElement : class, IAsyncListElement
{
    readonly Connectable<Channel<TElement>> _channels;
    readonly IDictionary<Guid, TElement> _messageLookup;
    readonly List<TElement> _messages;
    readonly CancellationToken _testCompleted;
    readonly TimeProvider _timeProvider;
    readonly TimeSpan _timeout;
    TestContextSaveMode _saveMode = TestContextSaveMode.All;
    int _maximumSavedElements = 4096;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    protected AsyncElementList(TimeSpan timeout, CancellationToken testCompleted = default)
        : this(timeout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    protected AsyncElementList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
    {
        _timeout = timeout;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _testCompleted = testCompleted;

        _messages = new List<TElement>();
        _messageLookup = new Dictionary<Guid, TElement>();
        _channels = new Connectable<Channel<TElement>>();
    }

    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    protected TimeProvider TimeProvider => _timeProvider;

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_messages)
                return _messages.Count;
        }
    }

    /// <summary>
    /// Gets the save mode value.
    /// </summary>
    public TestContextSaveMode SaveMode => _saveMode;

    /// <summary>
    /// Gets the maximum saved elements value.
    /// </summary>
    public int MaximumSavedElements => _maximumSavedElements;

    /// <summary>
    /// Performs the snapshot operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IReadOnlyList<TElement> Snapshot()
    {
        lock (_messages)
            return _messages.ToArray();
    }

    void ITestContextRetention.ConfigureRetention(TestContextSaveMode saveMode, int maximumSavedElements)
    {
        if (maximumSavedElements <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumSavedElements));

        lock (_messages)
        {
            _saveMode = saveMode;
            _maximumSavedElements = maximumSavedElements;

            if (saveMode == TestContextSaveMode.None)
            {
                _messages.Clear();
                _messageLookup.Clear();
                return;
            }

            if (saveMode == TestContextSaveMode.Bounded)
                TrimToCapacity();
        }
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async IAsyncEnumerable<TElement> SelectAsync(FilterDelegate<TElement> filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // This queue is scoped to one assertion enumerator and bounded both by the assertion timeout
        // and by an explicit element capacity. Overflow fails the assertion loudly instead of dropping
        // observations or allowing test traffic to grow process memory without a bound.
        var channel = Channel.CreateBounded<TElement>(new BoundedChannelOptions(_maximumSavedElements)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        var handle = _channels.Connect(channel);
        var returned = new HashSet<Guid>();

        try
        {
            foreach (var entry in GetMatchingSnapshot(filter, returned))
                yield return entry;

            if (cancellationToken.IsCancellationRequested || _testCompleted.IsCancellationRequested)
                yield break;

            using var timeout = new CancellationTokenSource(_timeout, _timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _testCompleted, cancellationToken);

            while (!linked.IsCancellationRequested)
            {
                TElement observed;
                try
                {
                    observed = await channel.Reader.ReadAsync(linked.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (_saveMode == TestContextSaveMode.None)
                {
                    if (observed.ElementId is Guid id && returned.Add(id) && filter(observed))
                        yield return observed;

                    continue;
                }

                foreach (var entry in GetMatchingSnapshot(filter, returned))
                    yield return entry;
            }
        }
        finally
        {
            handle.Disconnect();
            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<bool> AnyAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var _ in SelectAsync(filter, cancellationToken).ConfigureAwait(false))
                return true;
        }
        catch (OperationCanceledException)
        {
        }

        return false;
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<TElement> Select(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (_saveMode == TestContextSaveMode.None)
            yield break;

        var returned = new HashSet<Guid>();

        while (true)
        {
            foreach (var entry in GetMatchingSnapshot(filter, returned))
                yield return entry;

            if (cancellationToken.IsCancellationRequested || _testCompleted.IsCancellationRequested)
                yield break;

            using var timeout = new CancellationTokenSource(_timeout, _timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _testCompleted, cancellationToken);
            using var cancellationRegistration = linked.Token.Register(static state =>
            {
                var messageList = (List<TElement>)state!;
                lock (messageList)
                    Monitor.PulseAll(messageList);
            }, _messages);

            lock (_messages)
            {
                var observedCount = _messages.Count;
                while (observedCount == _messages.Count && !linked.IsCancellationRequested)
                    Monitor.Wait(_messages);
            }

            if (linked.IsCancellationRequested)
                yield break;
        }
    }

    IEnumerable<TElement> GetMatchingSnapshot(FilterDelegate<TElement> filter, HashSet<Guid> returned)
    {
        TElement[] snapshot;
        lock (_messages)
            snapshot = _messages.ToArray();

        foreach (var entry in snapshot)
        {
            if (!entry.ElementId.HasValue || !returned.Add(entry.ElementId.Value))
                continue;

            if (filter(entry))
                yield return entry;
        }
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected void Add(TElement context)
    {
        if (!context.ElementId.HasValue)
            return;

        if (_saveMode != TestContextSaveMode.None)
        {
            lock (_messages)
            {
                var elementId = context.ElementId.Value;

                if (_messageLookup.ContainsKey(elementId))
                    return;

                _messages.Add(context);
                _messageLookup.Add(elementId, context);

                if (_saveMode == TestContextSaveMode.Bounded)
                    TrimToCapacity();

                Monitor.PulseAll(_messages);
            }
        }

        _channels.ForEach(channel =>
        {
            if (!channel.Writer.TryWrite(context))
            {
                channel.Writer.TryComplete(new InvalidOperationException(
                    $"Test assertion observation capacity ({_maximumSavedElements}) was exceeded."));
            }
        });
    }

    void TrimToCapacity()
    {
        while (_messages.Count > _maximumSavedElements)
        {
            var removed = _messages[0];
            _messages.RemoveAt(0);
            if (removed.ElementId.HasValue)
                _messageLookup.Remove(removed.ElementId.Value);
        }
    }

}
