namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// The exact set of identities a run published, and what was actually observed against it.
/// <para>
/// Two questions are kept apart on purpose, because one boolean carrying both is how a weaker property
/// gets reported as the stronger one. <see cref="AllExpectedSeen"/> asks whether every expected
/// identity has arrived at least once - that is the signal a wait can end on. <see cref="Read"/> asks
/// whether the observation was exact: every identity seen exactly once, nothing seen twice, nothing
/// seen that this run never published. A ledger that completed on the first question and reported the
/// second would call a run with a duplicate complete.
/// </para>
/// </summary>
sealed class MessageSequenceLedger
{
    readonly int _expected;
    readonly int[] _seen;
    readonly ConcurrentBag<int> _outOfRange = new();
    readonly TaskCompletionSource<bool> _allSeen = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _distinct;

    public MessageSequenceLedger(int expected)
    {
        if (expected <= 0)
            throw new ArgumentOutOfRangeException(nameof(expected), expected, "a run publishes at least one message");

        _expected = expected;
        _seen = new int[expected];
    }

    /// <summary>Every expected identity has arrived at least once. Says nothing about how often.</summary>
    public bool AllExpectedSeen => Volatile.Read(ref _distinct) == _expected;

    public void Observed(int sequence)
    {
        if (sequence < 0 || sequence >= _expected)
        {
            _outOfRange.Add(sequence);
            return;
        }

        if (Interlocked.Increment(ref _seen[sequence]) == 1 && Interlocked.Increment(ref _distinct) == _expected)
            _allSeen.TrySetResult(true);
    }

    /// <summary>
    /// Waits until every expected identity has been seen at least once, and says whether that happened
    /// inside the budget.
    /// <para>
    /// Cancellation is not an answer about the messages, so it is raised rather than folded into
    /// <c>false</c>. A caller that cancelled has to be able to tell that apart from a run that lost
    /// one.
    /// </para>
    /// </summary>
    public async Task<bool> WaitForAllExpected(TimeSpan budget, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task delay = Task.Delay(budget, linked.Token);

        Task finished = await Task.WhenAny(_allSeen.Task, delay).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (finished == delay)
            return false;

        linked.Cancel();

        return true;
    }

    /// <summary>
    /// What the ledger holds at this moment.
    /// <para>
    /// A snapshot, and named as one. Nothing here claims that no further message will ever arrive: the
    /// consumer is still attached until the bus stops, and a duplicate that arrives after the last
    /// first-seen identity is exactly what this scenario has to be able to report. The caller decides
    /// how long to keep observing before taking it, and the scenario states that boundary in its
    /// result.
    /// </para>
    /// </summary>
    public Snapshot Read()
    {
        var missing = new List<int>();
        var duplicates = new List<int>();
        var total = 0;

        for (var sequence = 0; sequence < _expected; sequence++)
        {
            var count = Volatile.Read(ref _seen[sequence]);
            total += count;

            if (count == 0)
                missing.Add(sequence);
            else if (count > 1)
                duplicates.Add(sequence);
        }

        List<int> outOfRange = _outOfRange.ToList();

        return new Snapshot(_expected, Volatile.Read(ref _distinct), total + outOfRange.Count,
            Summarise(missing), Summarise(duplicates), Summarise(outOfRange));
    }

    /// <summary>
    /// The count and the first few, not the whole list. A hundred thousand missing identities would
    /// bury the rest of the result, and the first few are what a reader looks at.
    /// </summary>
    static Detail Summarise(List<int> sequences)
    {
        sequences.Sort();

        return new Detail(sequences.Count, sequences.Take(20).ToArray());
    }


    public readonly record struct Detail(int Count, int[] First);


    /// <summary>What was observed, and whether it was exact.</summary>
    public readonly record struct Snapshot(
        int Expected,
        int UniqueConsumed,
        int ObservationCount,
        Detail Missing,
        Detail Duplicates,
        Detail OutOfRange)
    {
        /// <summary>
        /// Every expected identity seen exactly once, nothing seen twice and nothing seen that was
        /// never published. This is the only predicate a rate may be reported for.
        /// </summary>
        public bool IsExact => Missing.Count == 0 && Duplicates.Count == 0 && OutOfRange.Count == 0
            && UniqueConsumed == Expected && ObservationCount == Expected;
    }
}
