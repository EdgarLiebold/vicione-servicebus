namespace ViciOne.ServiceBus.Diagnostics.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;


/// <summary>
/// What the ledger says about a set of identities, and where the difference between "everything
/// arrived" and "the observation was exact" actually shows.
/// <para>
/// The second question is the one the diagnostic reports on, and the first version of this ledger
/// answered it with the first question: it completed as soon as every identity had been seen once,
/// so a run that also delivered a duplicate was reported as complete. Every case below fails on the
/// design that conflates them.
/// </para>
/// </summary>
[TestFixture]
public class Reading_a_ledger_of_expected_identities
{
    [Test]
    public void Should_call_the_exact_set_exact()
    {
        var ledger = new MessageSequenceLedger(3);

        foreach (var sequence in new[] { 0, 1, 2 })
            ledger.Observed(sequence);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.IsExact, Is.True);
            Assert.That(ledger.AllExpectedSeen, Is.True);
            Assert.That(snapshot.UniqueConsumed, Is.EqualTo(3));
            Assert.That(snapshot.ObservationCount, Is.EqualTo(3));
            Assert.That(snapshot.Missing.Count, Is.Zero);
            Assert.That(snapshot.Duplicates.Count, Is.Zero);
            Assert.That(snapshot.OutOfRange.Count, Is.Zero);
        });
    }

    [Test]
    public void Should_report_a_missing_identity_and_name_it()
    {
        var ledger = new MessageSequenceLedger(3);

        ledger.Observed(0);
        ledger.Observed(2);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.IsExact, Is.False);
            Assert.That(ledger.AllExpectedSeen, Is.False);
            Assert.That(snapshot.Missing.First, Is.EqualTo(new[] { 1 }));
        });
    }

    [Test]
    public void Should_report_a_duplicate_and_a_missing_identity_separately()
    {
        var ledger = new MessageSequenceLedger(3);

        ledger.Observed(0);
        ledger.Observed(0);
        ledger.Observed(2);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.IsExact, Is.False);
            Assert.That(snapshot.Missing.First, Is.EqualTo(new[] { 1 }));
            Assert.That(snapshot.Duplicates.First, Is.EqualTo(new[] { 0 }));
            Assert.That(snapshot.ObservationCount, Is.EqualTo(3), "three observations, and still not the expected set");
        });
    }

    /// <summary>
    /// The case the previous design could not fail on: every expected identity arrived, and one of
    /// them arrived twice. All the counters that matter to a wait are satisfied.
    /// </summary>
    [Test]
    public void Should_refuse_to_call_a_complete_set_with_a_duplicate_exact()
    {
        var ledger = new MessageSequenceLedger(3);

        foreach (var sequence in new[] { 0, 1, 2, 1 })
            ledger.Observed(sequence);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.Multiple(() =>
        {
            Assert.That(ledger.AllExpectedSeen, Is.True, "every identity did arrive, which is the weaker question");
            Assert.That(snapshot.IsExact, Is.False, "and the observation was not exact");
            Assert.That(snapshot.Duplicates.First, Is.EqualTo(new[] { 1 }));
            Assert.That(snapshot.UniqueConsumed, Is.EqualTo(3));
            Assert.That(snapshot.ObservationCount, Is.EqualTo(4));
        });
    }

    /// <summary>The same shape for something that was never published at all.</summary>
    [Test]
    public void Should_refuse_to_call_a_complete_set_with_a_stranger_exact()
    {
        var ledger = new MessageSequenceLedger(3);

        foreach (var sequence in new[] { 0, 1, 2, 99 })
            ledger.Observed(sequence);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.Multiple(() =>
        {
            Assert.That(ledger.AllExpectedSeen, Is.True);
            Assert.That(snapshot.IsExact, Is.False);
            Assert.That(snapshot.OutOfRange.First, Is.EqualTo(new[] { 99 }));
        });
    }

    /// <summary>
    /// A duplicate that arrives after the last first-seen identity, which is exactly what the drain
    /// window in the scenario exists for.
    /// </summary>
    [Test]
    public async Task Should_still_see_a_duplicate_that_arrives_after_the_wait_ended()
    {
        var ledger = new MessageSequenceLedger(2);

        ledger.Observed(0);
        ledger.Observed(1);

        Assert.That(await ledger.WaitForAllExpected(TimeSpan.FromSeconds(5), CancellationToken.None), Is.True);
        Assert.That(ledger.Read().IsExact, Is.True, "exact at the moment the wait ended");

        ledger.Observed(1);

        Assert.That(ledger.Read().IsExact, Is.False,
            "a snapshot taken after the drain window has to show the duplicate that arrived in it");
    }

    [Test]
    public async Task Should_report_a_timeout_rather_than_an_answer_about_the_messages()
    {
        var ledger = new MessageSequenceLedger(2);

        ledger.Observed(0);

        Assert.That(await ledger.WaitForAllExpected(TimeSpan.FromMilliseconds(200), CancellationToken.None), Is.False);
    }

    /// <summary>
    /// Cancellation is not an answer about the messages. Folded into false it would look exactly like
    /// a run that lost one, and the caller could not tell which happened.
    /// </summary>
    [Test]
    public void Should_raise_cancellation_instead_of_reporting_an_incomplete_set()
    {
        var ledger = new MessageSequenceLedger(2);
        using var cancellation = new CancellationTokenSource();

        ledger.Observed(0);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        Assert.ThrowsAsync<OperationCanceledException>(
            () => ledger.WaitForAllExpected(TimeSpan.FromSeconds(30), cancellation.Token));
    }

    [Test]
    public async Task Should_hold_under_concurrent_observation()
    {
        const int expected = 5000;
        var ledger = new MessageSequenceLedger(expected);

        await Parallel.ForEachAsync(Enumerable.Range(0, expected), new ParallelOptions { MaxDegreeOfParallelism = 16 },
            (sequence, _) =>
            {
                ledger.Observed(sequence);

                return ValueTask.CompletedTask;
            });

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.IsExact, Is.True);
            Assert.That(snapshot.UniqueConsumed, Is.EqualTo(expected));
            Assert.That(snapshot.ObservationCount, Is.EqualTo(expected));
        });
    }

    [Test]
    public void Should_refuse_a_run_that_publishes_nothing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new MessageSequenceLedger(0));
    }
}
