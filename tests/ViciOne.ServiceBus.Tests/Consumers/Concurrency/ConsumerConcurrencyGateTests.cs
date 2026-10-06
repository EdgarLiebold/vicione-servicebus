using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Concurrency;

public sealed class ConsumerConcurrencyGateTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-GATE", "serial-exclusion-and-release")]
    public async Task SerialGate_ExcludesTheSecondInvocationUntilTheFirstCompletesAsync()
    {
        using ConsumerConcurrencyGate<Message> gate = new(ConsumerConcurrencyPolicy.Serial);
        var firstEntered = NewSignal();
        var releaseFirst = NewSignal();
        var secondEntered = NewSignal();

        Task first = gate.ExecuteAsync(new Message(1), (firstEntered, releaseFirst), static async (state, cancellationToken) =>
        {
            state.firstEntered.TrySetResult();
            await state.releaseFirst.Task.WaitAsync(cancellationToken);
        }, TestContext.Current.CancellationToken).AsTask();
        await firstEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

        Task second = gate.ExecuteAsync(new Message(2), secondEntered, static (entered, _) =>
        {
            entered.TrySetResult();
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken).AsTask();

        Assert.False(secondEntered.Task.IsCompleted);
        releaseFirst.TrySetResult();
        await Task.WhenAll(first, second).WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(secondEntered.Task.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-GATE", "parallel-exact-maximum")]
    public async Task ParallelGate_AdmitsExactlyTheDeclaredMaximumAsync()
    {
        using ConsumerConcurrencyGate<Message> gate = new(ConsumerConcurrencyPolicy.Parallel(2));
        var release = NewSignal();
        var twoEntered = NewSignal();
        var thirdEntered = NewSignal();
        int entered = 0;
        int active = 0;
        int maximum = 0;

        async ValueTask HoldAsync(TaskCompletionSource signal, CancellationToken cancellationToken)
        {
            int total = Interlocked.Increment(ref entered);
            int current = Interlocked.Increment(ref active);
            UpdateMaximum(ref maximum, current);
            if (total == 2)
                twoEntered.TrySetResult();
            if (total == 3)
                thirdEntered.TrySetResult();
            signal.TrySetResult();

            try
            {
                await release.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        }

        Task one = gate.ExecuteAsync(new Message(1), NewSignal(), HoldAsync, TestContext.Current.CancellationToken).AsTask();
        Task two = gate.ExecuteAsync(new Message(2), NewSignal(), HoldAsync, TestContext.Current.CancellationToken).AsTask();
        await twoEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

        Task three = gate.ExecuteAsync(new Message(3), NewSignal(), HoldAsync, TestContext.Current.CancellationToken).AsTask();
        Assert.False(thirdEntered.Task.IsCompleted);
        Assert.Equal(2, Volatile.Read(ref maximum));

        release.TrySetResult();
        await Task.WhenAll(one, two, three).WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(thirdEntered.Task.IsCompletedSuccessfully);
        Assert.Equal(2, Volatile.Read(ref maximum));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-GATE", "waiting-cancellation-preserves-token-and-capacity")]
    public async Task CanceledWaiter_PreservesItsTokenAndConsumesNoSlotAsync()
    {
        using ConsumerConcurrencyGate<Message> gate = new(ConsumerConcurrencyPolicy.Serial);
        var entered = NewSignal();
        var release = NewSignal();
        Task active = gate.ExecuteAsync(new Message(1), (entered, release), static async (state, cancellationToken) =>
        {
            state.entered.TrySetResult();
            await state.release.Task.WaitAsync(cancellationToken);
        }, TestContext.Current.CancellationToken).AsTask();
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);

        using var cancellation = new CancellationTokenSource();
        Task waiting = gate.ExecuteAsync(new Message(2), 0, static (_, _) => ValueTask.CompletedTask, cancellation.Token).AsTask();
        cancellation.Cancel();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        release.TrySetResult();
        await active.WaitAsync(TestContext.Current.CancellationToken);

        int invoked = 0;
        await gate.ExecuteAsync(new Message(3), () => invoked++, static (next, _) =>
        {
            next();
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken);
        Assert.Equal(1, invoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-GATE", "pipeline-fault-releases-capacity")]
    public async Task PipelineFailure_ReleasesTheOwnedSlotAndPreservesTheExactFailureAsync()
    {
        using ConsumerConcurrencyGate<Message> gate = new(ConsumerConcurrencyPolicy.Serial);
        var expected = new InvalidOperationException("consumer failed");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gate.ExecuteAsync(new Message(1), expected, static (failure, _) => ValueTask.FromException(failure),
                TestContext.Current.CancellationToken).AsTask());

        Assert.Same(expected, actual);
        int invoked = 0;
        await gate.ExecuteAsync(new Message(2), () => invoked++, static (next, _) =>
        {
            next();
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken);
        Assert.Equal(1, invoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-PARTITION", "same-key-exclusion-different-key-parallelism")]
    public async Task PartitionedGate_ExcludesTheSameKeyButAdmitsADifferentPartitionAsync()
    {
        using PartitionedConsumerConcurrencyGate<Message, int> gate = new(8, static message => message.Key);
        var firstEntered = NewSignal();
        var releaseFirst = NewSignal();
        var sameEntered = NewSignal();
        var differentEntered = NewSignal();

        Task first = gate.ExecuteAsync(new Message(1), (firstEntered, releaseFirst), static async (state, cancellationToken) =>
        {
            state.firstEntered.TrySetResult();
            await state.releaseFirst.Task.WaitAsync(cancellationToken);
        }, TestContext.Current.CancellationToken).AsTask();
        await firstEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

        Task same = gate.ExecuteAsync(new Message(1), sameEntered, static (entered, _) =>
        {
            entered.TrySetResult();
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken).AsTask();
        Task different = gate.ExecuteAsync(new Message(2), differentEntered, static (entered, _) =>
        {
            entered.TrySetResult();
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken).AsTask();

        await differentEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(different.IsCompletedSuccessfully);
        Assert.False(sameEntered.Task.IsCompleted);

        releaseFirst.TrySetResult();
        await Task.WhenAll(first, same).WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(sameEntered.Task.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-PARTITION", "null-key-and-hash-boundaries")]
    public async Task PartitionedGate_RejectsNullKeysAndHandlesMinimumHashCodesAsync()
    {
        using PartitionedConsumerConcurrencyGate<NullableKeyMessage, string> nullGate = new(2, static message => message.Key!);
        InvalidOperationException nullFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullGate.ExecuteAsync(new NullableKeyMessage(null), 0, static (_, _) => ValueTask.CompletedTask,
                TestContext.Current.CancellationToken).AsTask());
        Assert.Contains("must not be null", nullFailure.Message, StringComparison.Ordinal);

        using PartitionedConsumerConcurrencyGate<Message, int> minimumHashGate = new(3, static message => message.Key, new MinimumHashComparer());
        int invoked = 0;
        await minimumHashGate.ExecuteAsync(new Message(7), () => invoked++, static (next, _) =>
        {
            next();
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken);
        Assert.Equal(1, invoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-GATE", "dispose-closes-admission-and-drains-owned-work")]
    public async Task Dispose_RejectsNewAdmissionButLetsAcceptedActiveAndWaitingWorkDrainAsync()
    {
        ConsumerConcurrencyGate<Message> gate = new(ConsumerConcurrencyPolicy.Serial);
        var firstEntered = NewSignal();
        var releaseFirst = NewSignal();
        var waitingEntered = NewSignal();
        Task first = gate.ExecuteAsync(new Message(1), (firstEntered, releaseFirst), static async (state, cancellationToken) =>
        {
            state.firstEntered.TrySetResult();
            await state.releaseFirst.Task.WaitAsync(cancellationToken);
        }, TestContext.Current.CancellationToken).AsTask();
        await firstEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task acceptedWaiting = gate.ExecuteAsync(new Message(2), waitingEntered, static (entered, _) =>
        {
            entered.TrySetResult();
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken).AsTask();

        gate.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            gate.ExecuteAsync(new Message(3), 0, static (_, _) => ValueTask.CompletedTask,
                TestContext.Current.CancellationToken).AsTask());
        Assert.False(waitingEntered.Task.IsCompleted);

        releaseFirst.TrySetResult();
        await Task.WhenAll(first, acceptedWaiting).WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(waitingEntered.Task.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-GATE", "constructor-boundaries")]
    public void Constructors_RejectNullPartitionedAndOutOfRangeConfiguration()
    {
        Assert.Equal("policy", Assert.Throws<ArgumentNullException>(() => new ConsumerConcurrencyGate<Message>(null!)).ParamName);
        Assert.Equal("policy", Assert.Throws<ArgumentException>(() =>
            new ConsumerConcurrencyGate<Message>(ConsumerConcurrencyPolicy.Partitioned(2))).ParamName);
        Assert.Equal("partitionCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PartitionedConsumerConcurrencyGate<Message, int>(0, static message => message.Key)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            new PartitionedConsumerConcurrencyGate<Message, int>(2, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-PARTITION", "dispose-rejects-before-selection-and-drains-accepted-work")]
    public async Task PartitionedDispose_ClosesAdmissionBeforeSelectionAndDrainsAcceptedWorkAsync()
    {
        int selections = 0;
        using PartitionedConsumerConcurrencyGate<Message, int> gate = new(2, message =>
        {
            Interlocked.Increment(ref selections);
            return message.Key;
        });
        var entered = NewSignal();
        var release = NewSignal();
        int waitingInvocations = 0;
        Task? first = null;
        Task? waiting = null;
        try
        {
            first = gate.ExecuteAsync(new Message(1), (entered, release), static async (state, token) =>
            {
                state.entered.TrySetResult();
                await state.release.Task.WaitAsync(token);
            }, TestContext.Current.CancellationToken).AsTask();
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            waiting = gate.ExecuteAsync(new Message(1), 0, (_, _) =>
            {
                Interlocked.Increment(ref waitingInvocations);
                return ValueTask.CompletedTask;
            }, TestContext.Current.CancellationToken).AsTask();
            Assert.Equal(2, Volatile.Read(ref selections));
            Assert.False(first.IsCompleted);
            Assert.False(waiting.IsCompleted);
            Assert.Equal(0, Volatile.Read(ref waitingInvocations));

            gate.Dispose();
            gate.Dispose();
            foreach (int key in new[] { 1, 2 })
            {
                ObjectDisposedException failure = Assert.Throws<ObjectDisposedException>(() =>
                {
                    _ = gate.ExecuteAsync(new Message(key), 0, static (_, _) => ValueTask.CompletedTask,
                        TestContext.Current.CancellationToken);
                });
                Assert.Equal(typeof(PartitionedConsumerConcurrencyGate<Message, int>).FullName, failure.ObjectName);
            }
            Assert.Equal(2, Volatile.Read(ref selections));
            Assert.False(waiting.IsCompleted);
            release.TrySetResult();
            await Task.WhenAll(first, waiting).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(first.IsCompletedSuccessfully);
            Assert.True(waiting.IsCompletedSuccessfully);
            Assert.Equal(1, Volatile.Read(ref waitingInvocations));
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(new[] { first, waiting }.OfType<Task>())
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    private static TaskCompletionSource NewSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref maximum);
            if (observed >= candidate)
                return;
        }
        while (Interlocked.CompareExchange(ref maximum, candidate, observed) != observed);
    }

    private sealed record Message(int Key);
    private sealed record NullableKeyMessage(string? Key);

    private sealed class MinimumHashComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;
        public int GetHashCode(int value) => int.MinValue;
    }
}
