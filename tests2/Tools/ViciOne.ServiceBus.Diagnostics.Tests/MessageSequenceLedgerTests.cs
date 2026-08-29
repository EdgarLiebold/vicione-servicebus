using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Diagnostics.Tests;

public sealed class MessageSequenceLedgerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "exact-set")]
    public void ExactSetIsReportedAsExact()
    {
        var ledger = new MessageSequenceLedger(3);

        foreach (int sequence in new[] { 0, 1, 2 })
            ledger.Observed(sequence);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.True(snapshot.IsExact);
        Assert.True(ledger.AllExpectedSeen);
        Assert.Equal(3, snapshot.UniqueConsumed);
        Assert.Equal(3, snapshot.ObservationCount);
        Assert.Equal(0, snapshot.Missing.Count);
        Assert.Equal(0, snapshot.Duplicates.Count);
        Assert.Equal(0, snapshot.OutOfRange.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "missing-identity")]
    public void MissingIdentityIsNamed()
    {
        var ledger = new MessageSequenceLedger(3);
        ledger.Observed(0);
        ledger.Observed(2);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.False(snapshot.IsExact);
        Assert.False(ledger.AllExpectedSeen);
        Assert.Equal([1], snapshot.Missing.First);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "duplicate-and-missing")]
    public void DuplicateAndMissingAreReportedSeparately()
    {
        var ledger = new MessageSequenceLedger(3);
        ledger.Observed(0);
        ledger.Observed(0);
        ledger.Observed(2);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.False(snapshot.IsExact);
        Assert.Equal([1], snapshot.Missing.First);
        Assert.Equal([0], snapshot.Duplicates.First);
        Assert.Equal(3, snapshot.ObservationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "complete-with-duplicate")]
    public void CompleteSetWithDuplicateIsNotExact()
    {
        var ledger = new MessageSequenceLedger(3);
        foreach (int sequence in new[] { 0, 1, 2, 1 })
            ledger.Observed(sequence);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.True(ledger.AllExpectedSeen);
        Assert.False(snapshot.IsExact);
        Assert.Equal([1], snapshot.Duplicates.First);
        Assert.Equal(3, snapshot.UniqueConsumed);
        Assert.Equal(4, snapshot.ObservationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "complete-with-stranger")]
    public void CompleteSetWithStrangerIsNotExact()
    {
        var ledger = new MessageSequenceLedger(3);
        foreach (int sequence in new[] { 0, 1, 2, 99 })
            ledger.Observed(sequence);

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.True(ledger.AllExpectedSeen);
        Assert.False(snapshot.IsExact);
        Assert.Equal([99], snapshot.OutOfRange.First);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "duplicate-after-wait")]
    public async Task DuplicateAfterWaitChangesLaterSnapshot()
    {
        var ledger = new MessageSequenceLedger(2);
        ledger.Observed(0);
        ledger.Observed(1);

        Assert.True(await ledger.WaitForAllExpected(TimeSpan.FromDays(1), CancellationToken.None));
        Assert.True(ledger.Read().IsExact);

        ledger.Observed(1);

        Assert.False(ledger.Read().IsExact);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "budget-expiry")]
    public async Task BudgetExpiryReportsTimeout()
    {
        var time = new FakeTimeProvider();
        var ledger = new MessageSequenceLedger(2);
        ledger.Observed(0);

        Task<bool> waiting = ledger.WaitForAllExpected(TimeSpan.FromMinutes(1), CancellationToken.None, time);
        time.Advance(TimeSpan.FromMinutes(1));

        Assert.False(await waiting);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "caller-cancellation")]
    public async Task CallerCancellationIsPropagated()
    {
        var ledger = new MessageSequenceLedger(2);
        using var cancellation = new CancellationTokenSource();
        ledger.Observed(0);

        Task<bool> waiting = ledger.WaitForAllExpected(TimeSpan.FromDays(1), cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "concurrent-observation")]
    public async Task ConcurrentObservationReadsExactAfterCompletion()
    {
        const int expected = 5000;
        var ledger = new MessageSequenceLedger(expected);

        await Parallel.ForEachAsync(Enumerable.Range(0, expected),
            new ParallelOptions { MaxDegreeOfParallelism = 16 }, (sequence, _) =>
            {
                ledger.Observed(sequence);
                return ValueTask.CompletedTask;
            });

        MessageSequenceLedger.Snapshot snapshot = ledger.Read();

        Assert.True(snapshot.IsExact);
        Assert.Equal(expected, snapshot.UniqueConsumed);
        Assert.Equal(expected, snapshot.ObservationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-LEDGER", "positive-expected-count")]
    public void ZeroExpectedIdentitiesAreRejected()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => new MessageSequenceLedger(0));

        Assert.Equal("expected", exception.ParamName);
    }
}
