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
    public TestContextSaveMode SaveMode => _saveMode;

    /// <inheritdoc />
    public int MaximumSavedElements => _maximumSavedElements;

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

    /// <inheritdoc />
    public async Task<bool> AnyAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default)
    {
        await foreach (var _ in SelectAsync(filter, cancellationToken).ConfigureAwait(false))
            return true;

        return false;
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

    /// <summary>Records an identified observation and makes it available to active queries.</summary>
    /// <param name="element">The observation to record.</param>
    protected void Add(TElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!element.ElementId.HasValue)
            return;

        if (_saveMode != TestContextSaveMode.None)
        {
            lock (_messages)
            {
                var elementId = element.ElementId.Value;

                if (_messageLookup.ContainsKey(elementId))
                    return;

                _messages.Add(element);
                _messageLookup.Add(elementId, element);

                if (_saveMode == TestContextSaveMode.Bounded)
                    TrimToCapacity();

                Monitor.PulseAll(_messages);
            }
        }

        _channels.ForEach(channel =>
        {
            if (!channel.Writer.TryWrite(element))
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
