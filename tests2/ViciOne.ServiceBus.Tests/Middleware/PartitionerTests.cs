using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class PartitionerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "same-key-serialized-different-key-concurrent")]
    public async Task Partitioner_SerializesEqualKeysWhileDifferentPartitionsCanOverlap()
    {
        await using var partitioner = new Partitioner(2, new FirstByteHashGenerator());
        var firstEntered = NewSignal();
        var secondEntered = NewSignal();
        var otherPartitionEntered = NewSignal();
        var releaseFirst = NewSignal();
        var keyZeroInvocation = 0;
        var total = 0;
        IPipe<PartitionContext> pipe = Pipe.New<PartitionContext>(configuration =>
        {
            configuration.UsePartitioner(partitioner, context => context.Key);
            configuration.UseExecuteAsync(async context =>
            {
                Interlocked.Increment(ref total);
                if (context.Key[0] == 1)
                {
                    otherPartitionEntered.SetResult();
                    return;
                }

                if (Interlocked.Increment(ref keyZeroInvocation) == 1)
                {
                    firstEntered.SetResult();
                    await releaseFirst.Task;
                }
                else
                    secondEntered.SetResult();
            });
        });

        Task first = pipe.Send(new PartitionContext([0]));
        await firstEntered.Task;
        Task second = pipe.Send(new PartitionContext([0]));
        Task other = pipe.Send(new PartitionContext([1]));
        await otherPartitionEntered.Task;

        Assert.False(secondEntered.Task.IsCompleted);
        await other;
        releaseFirst.SetResult();
        await Task.WhenAll(first, second, secondEntered.Task);

        Assert.Equal(2, keyZeroInvocation);
        Assert.Equal(3, total);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "invalid-configuration-and-null-key")]
    public async Task Partitioner_RejectsInvalidConstructionAndANullRuntimeKey()
    {
        var hash = new FirstByteHashGenerator();
        Assert.Throws<ArgumentOutOfRangeException>(() => new Partitioner(0, hash));
        Assert.Throws<ArgumentNullException>(() => new Partitioner(2, null!));

        await using var partitioner = new Partitioner(2, hash);
        IPipe<PartitionContext> pipe = Pipe.New<PartitionContext>(configuration =>
        {
            configuration.UsePartitioner(partitioner, _ => (byte[])null!);
            configuration.UseExecute(_ => { });
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipe.Send(new PartitionContext([0])));

        Assert.Equal("The partition key provider returned null.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "string-key-overload-delivery-and-null-boundary")]
    public async Task Partitioner_StringKeysDeliverEveryContextAndRejectANullRuntimeKey()
    {
        var delivered = 0;
        IPipe<StringPartitionContext> pipe = Pipe.New<StringPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(8, context => context.Key);
            configuration.UseExecute(_ => Interlocked.Increment(ref delivered));
        });

        await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(index => pipe.Send(new StringPartitionContext(index.ToString()))));

        Assert.Equal(100, delivered);

        IPipe<StringPartitionContext> nullKeyPipe = Pipe.New<StringPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(2, _ => (string)null!);
            configuration.UseExecute(_ => { });
        });
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullKeyPipe.Send(new StringPartitionContext("ignored")));

        Assert.Equal("The partition key provider returned null.", exception.Message);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class PartitionContext(byte[] key) : BasePipeContext
    {
        public byte[] Key { get; } = key;
    }

    private sealed class StringPartitionContext(string key) : BasePipeContext
    {
        public string Key { get; } = key;
    }

    private sealed class FirstByteHashGenerator : IHashGenerator
    {
        public uint Hash(byte[] data) => data[0];
    }
}
