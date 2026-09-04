using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class ForkFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-FORK", "parallel-execution-and-joined-completion")]
    public async Task Fork_StartsBothBranchesAndCompletesOnlyAfterBothFinishAsync()
    {
        var forkEntered = NewSignal();
        var nextEntered = NewSignal();
        var releaseFork = NewSignal();
        var releaseNext = NewSignal();
        IPipe<TestPipeContext> fork = Pipe.ExecuteAsync<TestPipeContext>(async _ =>
        {
            forkEntered.SetResult();
            await releaseFork.Task;
        });
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseFork(fork);
            configuration.UseExecuteAsync(async _ =>
            {
                nextEntered.SetResult();
                await releaseNext.Task;
            });
        });

        Task send = pipe.SendAsync(new TestPipeContext());
        await Task.WhenAll(forkEntered.Task, nextEntered.Task);
        Assert.False(send.IsCompleted);

        releaseFork.SetResult();
        Assert.False(send.IsCompleted);
        releaseNext.SetResult();
        await send;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-FORK", "both-branch-failures-retained")]
    public async Task Fork_RetainsFailuresFromBothBranchesAsync()
    {
        var forkFailure = new ForkBranchException("fork");
        var nextFailure = new NextBranchException("next");
        IPipe<TestPipeContext> fork = Pipe.ExecuteAsync<TestPipeContext>(_ => Task.FromException(forkFailure));
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseFork(fork);
            configuration.UseExecuteAsync(_ => Task.FromException(nextFailure));
        });

        Task send = pipe.SendAsync(new TestPipeContext());
        await Assert.ThrowsAnyAsync<Exception>(() => send);
        AggregateException aggregate = Assert.IsType<AggregateException>(send.Exception);

        Assert.Contains(forkFailure, aggregate.InnerExceptions);
        Assert.Contains(nextFailure, aggregate.InnerExceptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-FORK", "null-branch-rejected")]
    public void Fork_RejectsANullBranchAtTheConfigurationBoundary()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            Pipe.New<TestPipeContext>(configuration => configuration.UseFork(null!)));

        Assert.Equal("pipe", exception.ParamName);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TestPipeContext : BasePipeContext
    {
    }

    private sealed class ForkBranchException(string message) : Exception(message);

    private sealed class NextBranchException(string message) : Exception(message);
}
