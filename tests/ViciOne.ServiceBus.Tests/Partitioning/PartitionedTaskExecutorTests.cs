using System.Collections.Concurrent;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Partitioning;

public sealed class PartitionedTaskExecutorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-CONSTRUCTION", "dependencies-and-limits")]
    public void Constructor_RejectsInvalidDependenciesAndLimits()
    {
        var hashGenerator = new FirstByteHashGenerator();

        ArgumentNullException partitionKeyProvider = Assert.Throws<ArgumentNullException>(() =>
            new PartitionedTaskExecutor<byte[]>(null!, 1, hashGenerator: hashGenerator));
        ArgumentOutOfRangeException partitions = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PartitionedTaskExecutor<byte[]>(value => value, 0, hashGenerator: hashGenerator));
        ArgumentOutOfRangeException concurrency = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PartitionedTaskExecutor<byte[]>(value => value, 1, 0, hashGenerator: hashGenerator));
        ArgumentOutOfRangeException capacity = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PartitionedTaskExecutor<byte[]>(value => value, 1, 1, 0, hashGenerator));

        Assert.Equal("partitionKeyProvider", partitionKeyProvider.ParamName);
        Assert.Equal("partitionCount", partitions.ParamName);
        Assert.Equal(0, partitions.ActualValue);
        Assert.Equal("concurrencyLimit", concurrency.ParamName);
        Assert.Equal(0, concurrency.ActualValue);
        Assert.Equal("partitionCapacity", capacity.ParamName);
        Assert.Equal(0, capacity.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-BOUNDARY", "null-delegates-fail-before-partition-selection")]
    public async Task Operations_RejectNullDelegatesBeforePartitionSelectionAsync()
    {
        var keyProviderInvocations = 0;
        await using var executor = new PartitionedTaskExecutor<int>(
            value =>
            {
                Interlocked.Increment(ref keyProviderInvocations);
                return BitConverter.GetBytes(value);
            },
            partitionCount: 2);

        ArgumentNullException enqueue = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.EnqueueAsync(1, null!, TestCancellationToken));
        ArgumentNullException execute = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync(1, null!, TestCancellationToken));

        Assert.Equal("method", enqueue.ParamName);
        Assert.Equal("method", execute.ParamName);
        Assert.Equal(0, keyProviderInvocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-HASH", "default-murmur-routing")]
    public async Task DefaultHashGenerator_RoutesKnownMurmurKeysToIndependentPartitionsAsync()
    {
        await using var executor = new PartitionedTaskExecutor<byte[]>(value => value, partitionCount: 2);
        var bothStarted = NewCompletionSource();
        var release = NewCompletionSource();
        var active = 0;

        async Task RunPartitionAsync()
        {
            if (Interlocked.Increment(ref active) == 2)
                bothStarted.TrySetResult();

            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Interlocked.Decrement(ref active);
        }

        Task first = executor.ExecuteAsync([0x61], RunPartitionAsync, TestCancellationToken);
        Task second = executor.ExecuteAsync([0x61, 0x61], RunPartitionAsync, TestCancellationToken);

        try
        {
            await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestCancellationToken);
            Assert.Equal(2, Volatile.Read(ref active));
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(first, second).WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Equal(0, Volatile.Read(ref active));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-ORDER", "same-partition-fifo-and-serialization")]
    public async Task SamePartition_ExecutesInSubmissionOrderWithoutOverlapAsync()
    {
        await using var executor = CreateExecutor(partitionCount: 2);
        var firstStarted = NewCompletionSource();
        var release = NewCompletionSource();
        var order = new ConcurrentQueue<int>();
        var active = 0;
        var maximum = 0;

        Task first = executor.ExecuteAsync(
            [0],
            async () =>
            {
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, current);
                order.Enqueue(1);
                firstStarted.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                Interlocked.Decrement(ref active);
            },
            TestCancellationToken);
        await firstStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Task second = executor.ExecuteAsync(
            [0],
            () =>
            {
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, current);
                order.Enqueue(2);
                Interlocked.Decrement(ref active);
                return Task.CompletedTask;
            },
            TestCancellationToken);

        try
        {
            Assert.False(second.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(first, second).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal([1, 2], order);
        Assert.Equal(1, Volatile.Read(ref maximum));
        Assert.Equal(0, Volatile.Read(ref active));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-CONCURRENCY", "independent-partitions")]
    public async Task DifferentPartitions_CanExecuteConcurrentlyAsync()
    {
        await using var executor = CreateExecutor(partitionCount: 2);
        var bothStarted = NewCompletionSource();
        var release = NewCompletionSource();
        var active = 0;
        var maximum = 0;

        async Task RunPartitionAsync()
        {
            int current = Interlocked.Increment(ref active);
            UpdateMaximum(ref maximum, current);
            if (current == 2)
                bothStarted.TrySetResult();

            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Interlocked.Decrement(ref active);
        }

        Task first = executor.ExecuteAsync([0], RunPartitionAsync, TestCancellationToken);
        Task second = executor.ExecuteAsync([1], RunPartitionAsync, TestCancellationToken);

        try
        {
            await bothStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Assert.Equal(2, Volatile.Read(ref maximum));
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(first, second).WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Equal(0, Volatile.Read(ref active));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-CONCURRENCY", "same-partition-configured-limit")]
    public async Task ConcurrentDeliveryLimit_IsAppliedWithinEachPartitionAsync()
    {
        await using var executor = new PartitionedTaskExecutor<byte[]>(
            value => value,
            partitionCount: 2,
            concurrencyLimit: 2,
            hashGenerator: new FirstByteHashGenerator());
        var saturated = NewCompletionSource();
        var release = NewCompletionSource();
        var active = 0;
        var maximum = 0;

        async Task RunPartitionAsync()
        {
            int current = Interlocked.Increment(ref active);
            UpdateMaximum(ref maximum, current);
            if (current == 2)
                saturated.TrySetResult();

            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Interlocked.Decrement(ref active);
        }

        Task[] work =
        [
            executor.ExecuteAsync([0], RunPartitionAsync, TestCancellationToken),
            executor.ExecuteAsync([0], RunPartitionAsync, TestCancellationToken),
            executor.ExecuteAsync([0], RunPartitionAsync, TestCancellationToken),
        ];

        try
        {
            await saturated.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Assert.Equal(2, Volatile.Read(ref maximum));
            Assert.All(work, task => Assert.False(task.IsCompleted));
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(work).WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Equal(0, Volatile.Read(ref active));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-KEY", "null-rejected-empty-uses-configured-hash")]
    public async Task NullKeysAreRejectedAndEmptyKeysUseTheConfiguredHashGeneratorAsync()
    {
        var hashGenerator = new EmptyKeyHashGenerator();
        await using var executor = new PartitionedTaskExecutor<string>(
            value => value switch
            {
                "null" => null!,
                "zero" => [0],
                _ => [],
            },
            partitionCount: 2,
            hashGenerator: hashGenerator);
        var firstStarted = NewCompletionSource();
        var release = NewCompletionSource();
        var zeroPartitionExecuted = NewCompletionSource();

        InvalidOperationException nullKey = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync("null", () => Task.CompletedTask, TestCancellationToken));
        Assert.Equal("The partition key provider returned null.", nullKey.Message);

        Task first = executor.ExecuteAsync(
            "empty",
            async () =>
            {
                firstStarted.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            },
            TestCancellationToken);
        await firstStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Task independentPartition = executor.ExecuteAsync(
            "zero",
            () =>
            {
                zeroPartitionExecuted.TrySetResult();
                return Task.CompletedTask;
            },
            TestCancellationToken);

        try
        {
            await zeroPartitionExecuted.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Assert.False(first.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(first, independentPartition).WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Equal(2, hashGenerator.InvocationCount);
        Assert.Equal(1, hashGenerator.EmptyKeyInvocationCount);

        await using var singlePartitionExecutor = new PartitionedTaskExecutor<string>(
            _ => null!,
            partitionCount: 1,
            hashGenerator: new ThrowingHashGenerator());
        InvalidOperationException singlePartitionNullKey = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            singlePartitionExecutor.ExecuteAsync("null", () => Task.CompletedTask, TestCancellationToken));
        Assert.Equal("The partition key provider returned null.", singlePartitionNullKey.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-BACKPRESSURE", "bounded-per-partition")]
    public async Task Capacity_IsBoundedPerPartitionWithoutBlockingIndependentPartitionsAsync()
    {
        await using var executor = CreateExecutor(partitionCount: 2, partitionCapacity: 1);
        var firstStarted = NewCompletionSource();
        var release = NewCompletionSource();
        var queuedExecuted = false;
        var overflowExecuted = false;

        Task first = executor.ExecuteAsync(
            [0],
            async () =>
            {
                firstStarted.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            },
            TestCancellationToken);
        await firstStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        await executor.EnqueueAsync([0], () =>
        {
            queuedExecuted = true;
            return Task.CompletedTask;
        }, TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
        Task overflowAdmission = executor.EnqueueAsync([0], () =>
        {
            overflowExecuted = true;
            return Task.CompletedTask;
        }, TestCancellationToken);

        await executor.ExecuteAsync([1], () => Task.CompletedTask, TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);

        try
        {
            Assert.False(overflowAdmission.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
        await overflowAdmission.WaitAsync(OperationTimeout, TestCancellationToken);
        await executor.DisposeAsync();

        Assert.True(queuedExecuted);
        Assert.True(overflowExecuted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONED-TASK-EXECUTOR-DISPOSAL", "concurrent-drain-and-closed")]
    public async Task ConcurrentDisposal_DrainsAcceptedWorkAndRejectsEveryPartitionAsync()
    {
        var keyProviderInvocations = 0;
        var hashGenerator = new CountingHashGenerator();
        var executor = new PartitionedTaskExecutor<byte[]>(
            value =>
            {
                Interlocked.Increment(ref keyProviderInvocations);
                return value;
            },
            partitionCount: 2,
            hashGenerator: hashGenerator);
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        await executor.EnqueueAsync(
            [0],
            async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                executed = true;
            },
            TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Task firstDisposal = executor.DisposeAsync().AsTask();
        Task secondDisposal = executor.DisposeAsync().AsTask();
        Assert.False(firstDisposal.IsCompleted);
        Assert.False(secondDisposal.IsCompleted);

        ObjectDisposedException neverActivatedPartition = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            executor.ExecuteAsync([1], () => Task.CompletedTask, TestCancellationToken));

        release.TrySetResult();
        await Task.WhenAll(firstDisposal, secondDisposal).WaitAsync(OperationTimeout, TestCancellationToken);
        await executor.DisposeAsync();

        ObjectDisposedException activatedPartition = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            executor.EnqueueAsync([0], () => Task.CompletedTask, TestCancellationToken));

        Assert.Equal("PartitionedTaskExecutor", neverActivatedPartition.ObjectName);
        Assert.Equal("PartitionedTaskExecutor", activatedPartition.ObjectName);
        Assert.True(executed);
        Assert.Equal(1, keyProviderInvocations);
        Assert.Equal(1, hashGenerator.InvocationCount);
    }

    private static PartitionedTaskExecutor<byte[]> CreateExecutor(int partitionCount, int? partitionCapacity = null) =>
        new(value => value, partitionCount, partitionCapacity: partitionCapacity, hashGenerator: new FirstByteHashGenerator());

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref maximum);
            if (candidate <= observed)
                return;
        }
        while (Interlocked.CompareExchange(ref maximum, candidate, observed) != observed);
    }

    private sealed class FirstByteHashGenerator : IPartitionHashGenerator
    {
        public uint ComputeHash(ReadOnlySpan<byte> partitionKey) => partitionKey[0];
    }

    private sealed class CountingHashGenerator : IPartitionHashGenerator
    {
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public uint ComputeHash(ReadOnlySpan<byte> partitionKey)
        {
            Interlocked.Increment(ref _invocationCount);
            return partitionKey[0];
        }
    }

    private sealed class EmptyKeyHashGenerator : IPartitionHashGenerator
    {
        private int _emptyKeyInvocationCount;
        private int _invocationCount;

        public int EmptyKeyInvocationCount => Volatile.Read(ref _emptyKeyInvocationCount);

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public uint ComputeHash(ReadOnlySpan<byte> partitionKey)
        {
            Interlocked.Increment(ref _invocationCount);
            if (partitionKey.IsEmpty)
            {
                Interlocked.Increment(ref _emptyKeyInvocationCount);
                return 1;
            }

            return partitionKey[0];
        }
    }

    private sealed class ThrowingHashGenerator : IPartitionHashGenerator
    {
        public uint ComputeHash(ReadOnlySpan<byte> partitionKey) => throw new InvalidOperationException("Hashing was not expected.");
    }
}
