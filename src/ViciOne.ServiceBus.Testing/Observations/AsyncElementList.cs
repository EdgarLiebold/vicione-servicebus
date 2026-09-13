using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores observations for stable snapshots and bounded asynchronous queries over current and future elements.</summary>
/// <typeparam name="TElement">The observed element type.</typeparam>
public abstract class AsyncElementList<TElement> :
    IAsyncElementList<TElement>,
    ITestContextRetention
    where TElement : class, IAsyncListElement
{
    readonly Connectable<Channel<TElement>> _channels;
    readonly IDictionary<Guid, TElement> _messageLookup;
    readonly List<TElement> _messages;
    readonly HashSet<TElement> _unidentifiedMessages;
    readonly CancellationToken _testCompleted;
    readonly TimeProvider _timeProvider;
    readonly TimeSpan _timeout;
    TestContextSaveMode _saveMode = TestContextSaveMode.All;
    int _maximumSavedElements = 4096;

    /// <summary>Creates an observation list that uses the system clock.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    protected AsyncElementList(TimeSpan timeout, CancellationToken testCompleted = default)
        : this(timeout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>Creates an observation list.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    /// <param name="timeProvider">The clock used for query timeouts.</param>
    protected AsyncElementList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
    {
        _timeout = timeout > TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan
            ? timeout
            : throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be greater than zero.");
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _testCompleted = testCompleted;

        _messages = new List<TElement>();
        _messageLookup = new Dictionary<Guid, TElement>();
        _unidentifiedMessages = new HashSet<TElement>(ReferenceEqualityComparer.Instance);
        _channels = new Connectable<Channel<TElement>>();
    }

    /// <summary>Gets the clock used for query timeouts and observation timestamps.</summary>
    protected TimeProvider TimeProvider => _timeProvider;

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_messages)
                return _messages.Count;
        }
    }

    /// <inheritdoc />
    public TestContextSaveMode SaveMode
    {
        get
        {
            lock (_messages)
                return _saveMode;
        }
    }

    /// <inheritdoc />
    public int MaximumSavedElements
    {
        get
        {
            lock (_messages)
                return _maximumSavedElements;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<TElement> Snapshot()
    {
        lock (_messages)
            return _messages.ToArray();
    }

    void ITestContextRetention.ConfigureRetention(TestContextSaveMode saveMode, int maximumSavedElements)
    {
        if (!Enum.IsDefined(saveMode))
            throw new ArgumentOutOfRangeException(nameof(saveMode), saveMode, "The context save mode is not defined.");
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
                _unidentifiedMessages.Clear();
                return;
            }

            if (saveMode == TestContextSaveMode.Bounded)
                TrimToCapacity();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TElement> SelectAsync(FilterDelegate<TElement> filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // This queue is scoped to one assertion enumerator and bounded both by the assertion timeout
        // and by an explicit element capacity. Overflow fails the assertion loudly instead of dropping
        // observations or allowing test traffic to grow process memory without a bound.
        int capacity;
        lock (_messages)
            capacity = _maximumSavedElements;

        var channel = Channel.CreateBounded<TElement>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        var handle = _channels.Connect(channel);
        var returnedIds = new HashSet<Guid>();
        var returnedUnidentified = new HashSet<TElement>(ReferenceEqualityComparer.Instance);

        try
        {
            foreach (var entry in GetMatchingSnapshot(filter, returnedIds, returnedUnidentified))
                yield return entry;

            cancellationToken.ThrowIfCancellationRequested();
            if (_testCompleted.IsCancellationRequested)
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
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                }

                TestContextSaveMode saveMode;
                lock (_messages)
                    saveMode = _saveMode;

                if (saveMode == TestContextSaveMode.None)
                {
                    if (MarkReturned(observed, returnedIds, returnedUnidentified) && filter(observed))
                        yield return observed;

                    continue;
                }

                foreach (var entry in GetMatchingSnapshot(filter, returnedIds, returnedUnidentified))
                    yield return entry;
            }
        }
        finally
        {
            handle.Disconnect();
            channel.Writer.TryComplete();
        }
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default)
    {
        await foreach (var _ in SelectAsync(filter, cancellationToken).ConfigureAwait(false))
            return true;

        return false;
    }

    IEnumerable<TElement> GetMatchingSnapshot(FilterDelegate<TElement> filter, HashSet<Guid> returnedIds,
        HashSet<TElement> returnedUnidentified)
    {
        TElement[] snapshot;
        lock (_messages)
            snapshot = _messages.ToArray();

        foreach (var entry in snapshot)
        {
            if (!MarkReturned(entry, returnedIds, returnedUnidentified))
                continue;

            if (filter(entry))
                yield return entry;
        }
    }

    /// <summary>Records an observation and makes it available to active queries.</summary>
    /// <param name="element">The observation to record.</param>
    protected void Add(TElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        int capacity;
        lock (_messages)
        {
            capacity = _maximumSavedElements;
            if (_saveMode != TestContextSaveMode.None)
            {
                if (element.ElementId is Guid elementId)
                {
                    if (_messageLookup.ContainsKey(elementId))
                        return;

                    _messageLookup.Add(elementId, element);
                }
                else if (!_unidentifiedMessages.Add(element))
                    return;

                _messages.Add(element);
                if (_saveMode == TestContextSaveMode.Bounded)
                    TrimToCapacity();
            }
        }

        _channels.ForEach(channel =>
        {
            if (!channel.Writer.TryWrite(element))
            {
                channel.Writer.TryComplete(new InvalidOperationException(
                    $"Test assertion observation capacity ({capacity}) was exceeded."));
            }
        });
    }

    static bool MarkReturned(TElement element, ISet<Guid> returnedIds, ISet<TElement> returnedUnidentified)
    {
        return element.ElementId is Guid elementId
            ? returnedIds.Add(elementId)
            : returnedUnidentified.Add(element);
    }

    void TrimToCapacity()
    {
        while (_messages.Count > _maximumSavedElements)
        {
            var removed = _messages[0];
            _messages.RemoveAt(0);
            if (removed.ElementId.HasValue)
                _messageLookup.Remove(removed.ElementId.Value);
            else
                _unidentifiedMessages.Remove(removed);
        }
    }

}
