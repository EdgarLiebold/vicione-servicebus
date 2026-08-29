using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Diagnostics.Tests;

public sealed class ObservationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OBSERVATION", "completed-stop")]
    public async Task CompletedStopReportsQuiescence()
    {
        Assert.True(await PublishLoadScenario.Quiesce(_ => Task.CompletedTask, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OBSERVATION", "swallowed-budget-cancellation")]
    public async Task SwallowedBudgetCancellationIsNotQuiescence()
    {
        var time = new FakeTimeProvider();
        var stopFinished = new TaskCompletionSource();
        Task<bool> quiescing = PublishLoadScenario.Quiesce(token =>
        {
            token.Register(() => stopFinished.TrySetResult());
            return stopFinished.Task;
        }, TimeSpan.FromMinutes(1), time);

        time.Advance(TimeSpan.FromMinutes(1));

        Assert.True(quiescing.IsCompleted);
        Assert.False(await quiescing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OBSERVATION", "raised-budget-cancellation")]
    public async Task RaisedBudgetCancellationIsNotQuiescence()
    {
        var time = new FakeTimeProvider();
        Task<bool> quiescing = PublishLoadScenario.Quiesce(
            token => Task.Delay(Timeout.InfiniteTimeSpan, time, token), TimeSpan.FromMinutes(1), time);

        time.Advance(TimeSpan.FromMinutes(1));
        await Task.Yield();
        await Task.Yield();

        Assert.True(quiescing.IsCompleted);
        Assert.False(await quiescing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OBSERVATION", "bounded-stop-token")]
    public async Task StopReceivesBoundedToken()
    {
        CancellationToken handed = CancellationToken.None;

        bool quiesced = await PublishLoadScenario.Quiesce(token =>
        {
            handed = token;
            return Task.CompletedTask;
        }, TimeSpan.FromMinutes(1));

        Assert.True(quiesced);
        Assert.True(handed.CanBeCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OBSERVATION", "snapshot-after-stop")]
    public async Task SnapshotIsReadAfterStopCompletes()
    {
        var ledger = new MessageSequenceLedger(2);
        ledger.Observed(0);

        (bool quiesced, MessageSequenceLedger.Snapshot snapshot) =
            await PublishLoadScenario.ObserveThenQuiesceThenRead(ledger, _ =>
            {
                ledger.Observed(1);
                return Task.CompletedTask;
            }, TimeSpan.Zero, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.True(quiesced);
        Assert.True(snapshot.IsExact);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OBSERVATION", "whole-observation-window")]
    public async Task ObservationWindowCompletesBeforeStop()
    {
        var time = new FakeTimeProvider();
        var stopCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task observing = PublishLoadScenario.ObserveThenQuiesceThenRead(new MessageSequenceLedger(1), _ =>
        {
            stopCalled.TrySetResult();
            return Task.CompletedTask;
        }, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), CancellationToken.None, time);

        Assert.False(stopCalled.Task.IsCompleted);
        time.Advance(TimeSpan.FromMinutes(1));
        await Task.Yield();

        Assert.True(stopCalled.Task.IsCompleted);
        await stopCalled.Task;
        await observing;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OUTCOME", "nothing-seen-no-quiescence")]
    public void NothingSeenWithoutQuiescenceIsTimeout()
    {
        Assert.Equal("timeout", PublishLoadScenario.Outcome(false, false, false));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OUTCOME", "missing-messages")]
    public void MissingMessagesRemainTimeout()
    {
        Assert.Equal("timeout", PublishLoadScenario.Outcome(false, true, true));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OUTCOME", "live-exact-snapshot")]
    public void ExactLiveSnapshotIsInconclusive()
    {
        Assert.Equal("inconclusive", PublishLoadScenario.Outcome(true, false, true));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OUTCOME", "quiesced-inexact-set")]
    public void QuiescedInexactSetIsInvalid()
    {
        Assert.Equal("invalid", PublishLoadScenario.Outcome(true, true, false));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-OUTCOME", "quiesced-exact-set")]
    public void QuiescedExactSetIsExact()
    {
        Assert.Equal("exact", PublishLoadScenario.Outcome(true, true, true));
    }
}
