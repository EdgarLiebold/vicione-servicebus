using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class LatestFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-LATEST-FILTER", "creation-first-value-and-latest-snapshot")]
    public async Task Latest_TransitionsFromPendingToTheMostRecentlyObservedContext()
    {
        ILatestFilter<ObservedContext>? latest = null;
        var createdCount = 0;
        IPipe<ObservedContext> pipe = Pipe.New<ObservedContext>(configuration =>
        {
            configuration.UseLatest(configurator => configurator.Created = filter =>
            {
                Interlocked.Increment(ref createdCount);
                latest = filter;
            });
            configuration.UseExecute(_ => { });
        });

        Task<ObservedContext> beforeFirst = latest!.Latest;
        Assert.False(beforeFirst.IsCompleted);

        var first = new ObservedContext(1);
        await pipe.Send(first);
        Assert.Same(first, await beforeFirst);

        var last = new ObservedContext(100);
        for (var value = 2; value < 100; value++)
            await pipe.Send(new ObservedContext(value));
        await pipe.Send(last);

        Assert.Same(last, await latest.Latest);
        Assert.Equal(1, createdCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LATEST-FILTER", "observed-entry-survives-downstream-fault")]
    public async Task Latest_RetainsTheContextObservedBeforeADownstreamFailure()
    {
        ILatestFilter<ObservedContext>? latest = null;
        var expected = new DownstreamException("downstream failed");
        IPipe<ObservedContext> pipe = Pipe.New<ObservedContext>(configuration =>
        {
            configuration.UseLatest(configurator => configurator.Created = filter => latest = filter);
            configuration.UseExecute(context =>
            {
                if (context.Value == 2)
                    throw expected;
            });
        });
        await pipe.Send(new ObservedContext(1));
        var faulting = new ObservedContext(2);

        DownstreamException actual = await Assert.ThrowsAsync<DownstreamException>(() => pipe.Send(faulting));

        Assert.Same(expected, actual);
        Assert.Same(faulting, await latest!.Latest);
    }

    private sealed class ObservedContext(int value) : BasePipeContext
    {
        public int Value { get; } = value;
    }

    private sealed class DownstreamException(string message) : Exception(message);
}
