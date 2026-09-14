using System.Reflection;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class PartitionerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "same-key-serialized-different-key-concurrent")]
    public async Task Partitioner_SerializesEqualKeysWhileDifferentPartitionsCanOverlapAsync()
    {
        await using var partitioner = new PipePartitioner(2, new FirstByteHashGenerator());
        var firstEntered = NewSignal();
        var secondEntered = NewSignal();
        var otherPartitionEntered = NewSignal();
        var releaseFirst = NewSignal();
        var keyZeroInvocation = 0;
        var total = 0;
        IPipe<PartitionContext> pipe = Pipe.New<PartitionContext>(configuration =>
        {
            configuration.UsePartitioner(partitioner, context => context.Key);
            configuration.UseExecuteAwaited(async context =>
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
                    await releaseFirst.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                }
                else
                    secondEntered.SetResult();
            });
        });

        Task first = pipe.SendAsync(new PartitionContext([0]));
        await firstEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        Task second = pipe.SendAsync(new PartitionContext([0]));
        Task other = pipe.SendAsync(new PartitionContext([1]));
        await otherPartitionEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.False(secondEntered.Task.IsCompleted);
        await other.WaitAsync(OperationTimeout, TestCancellationToken);
        releaseFirst.SetResult();
        await Task.WhenAll(first, second, secondEntered.Task).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(2, keyZeroInvocation);
        Assert.Equal(3, total);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "invalid-configuration-and-null-key")]
    public async Task Partitioner_RejectsInvalidConstructionAndANullRuntimeKeyAsync()
    {
        var hash = new FirstByteHashGenerator();
        Assert.Throws<ArgumentOutOfRangeException>(() => new PipePartitioner(0, hash));
        Assert.Throws<ArgumentNullException>(() => new PipePartitioner(2, null!));

        await using var partitioner = new PipePartitioner(2, hash);
        IPipe<PartitionContext> pipe = Pipe.New<PartitionContext>(configuration =>
        {
            configuration.UsePartitioner(partitioner, _ => (byte[])null!);
            configuration.UseExecute(_ => { });
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipe.SendAsync(new PartitionContext([0])));

        Assert.Equal("The partition key provider returned null.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "string-key-overload-delivery-and-null-boundary")]
    public async Task Partitioner_StringKeysDeliverEveryContextAndRejectANullRuntimeKeyAsync()
    {
        var delivered = 0;
        IPipe<StringPartitionContext> pipe = Pipe.New<StringPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(8, context => context.Key);
            configuration.UseExecute(_ => Interlocked.Increment(ref delivered));
        });

        await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(index => pipe.SendAsync(new StringPartitionContext(index.ToString()))));

        Assert.Equal(100, delivered);

        IPipe<StringPartitionContext> nullKeyPipe = Pipe.New<StringPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(2, _ => (string)null!);
            configuration.UseExecute(_ => { });
        });
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullKeyPipe.SendAsync(new StringPartitionContext("ignored")));

        Assert.Equal("The partition key provider returned null.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "shared-text-overload-honors-explicit-encoding")]
    public async Task SharedTextPartitioner_UsesTheExplicitEncodingToCreateHashKeysAsync()
    {
        var hashGenerator = new CaptureHashGenerator();
        await using var partitioner = new PipePartitioner(2, hashGenerator);
        IPipe<StringPartitionContext> pipe = Pipe.New<StringPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(partitioner, context => context.Key, Encoding.BigEndianUnicode);
            configuration.UseExecute(_ => { });
        });

        await pipe.SendAsync(new StringPartitionContext("Å"));

        Assert.Equal(new byte[] { 0x00, 0xc5 }, hashGenerator.CapturedKey);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "dedicated-text-overload-honors-explicit-encoding")]
    public async Task DedicatedTextPartitioner_UsesTheExplicitEncodingForPartitionSelectionAsync()
    {
        var firstEntered = NewSignal();
        var secondEntered = NewSignal();
        var release = NewSignal();
        IPipe<StringPartitionContext> pipe = Pipe.New<StringPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(2, context => context.Key, Encoding.BigEndianUnicode);
            configuration.UseExecuteAwaited(async context =>
            {
                if (context.Key == "A")
                {
                    firstEntered.TrySetResult();
                    await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                }
                else
                    secondEntered.TrySetResult();
            });
        });

        Task first = pipe.SendAsync(new StringPartitionContext("A"));
        await firstEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        Task second = pipe.SendAsync(new StringPartitionContext("D"));
        try
        {
            await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestCancellationToken);
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(first, second).WaitAsync(OperationTimeout, TestCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "empty-key-is-hashed-by-the-configured-algorithm")]
    public async Task EmptyPartitionKey_IsPassedToTheConfiguredHashGeneratorAsync()
    {
        var hashGenerator = new CaptureHashGenerator(hash: 1);
        await using var partitioner = new PipePartitioner(2, hashGenerator);
        IPipe<PartitionContext> pipe = Pipe.New<PartitionContext>(configuration =>
        {
            configuration.UsePartitioner(partitioner, context => context.Key);
            configuration.UseExecute(_ => { });
        });

        await pipe.SendAsync(new PartitionContext([]));

        Assert.NotNull(hashGenerator.CapturedKey);
        Assert.Empty(hashGenerator.CapturedKey);
        Assert.Equal(1, hashGenerator.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "all-pipeline-overloads-reject-invalid-arguments")]
    public async Task PipelinePartitionerOverloads_RejectEveryInvalidRequiredArgumentAsync()
    {
        await using var shared = new PipePartitioner(2);

        AssertParameter<ArgumentOutOfRangeException>("partitionCount", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(0, (Func<PartitionContext, byte[]>)(_ => []))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(2, (Func<PartitionContext, byte[]>)null!)));
        AssertParameter<ArgumentNullException>("partitioner", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner((IPartitioner)null!, (Func<PartitionContext, byte[]>)(_ => []))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(shared, (Func<PartitionContext, byte[]>)null!)));

        AssertParameter<ArgumentOutOfRangeException>("partitionCount", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(0, (Func<PartitionContext, Guid>)(_ => Guid.Empty))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(2, (Func<PartitionContext, Guid>)null!)));
        AssertParameter<ArgumentNullException>("partitioner", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner((IPartitioner)null!, (Func<PartitionContext, Guid>)(_ => Guid.Empty))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(shared, (Func<PartitionContext, Guid>)null!)));

        AssertParameter<ArgumentOutOfRangeException>("partitionCount", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(0, (Func<PartitionContext, string>)(_ => "key"))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(2, (Func<PartitionContext, string>)null!)));
        AssertParameter<ArgumentNullException>("partitioner", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner((IPartitioner)null!, (Func<PartitionContext, string>)(_ => "key"))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(shared, (Func<PartitionContext, string>)null!)));

        AssertParameter<ArgumentOutOfRangeException>("partitionCount", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(0, (Func<PartitionContext, long>)(_ => 0L))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(2, (Func<PartitionContext, long>)null!)));
        AssertParameter<ArgumentNullException>("partitioner", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner((IPartitioner)null!, (Func<PartitionContext, long>)(_ => 0L))));
        AssertParameter<ArgumentNullException>("keyProvider", () => Pipe.New<PartitionContext>(configuration =>
            configuration.UsePartitioner(shared, (Func<PartitionContext, long>)null!)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "signed-integer-keys-use-platform-independent-little-endian-bytes")]
    public async Task Partitioner_IntegerKeysHaveAStablePlatformIndependentBinaryRepresentationAsync()
    {
        var hashGenerator = new CaptureHashGenerator();
        await using var partitioner = new PipePartitioner(2, hashGenerator);
        IPipe<LongPartitionContext> pipe = Pipe.New<LongPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(partitioner, context => context.Key);
            configuration.UseExecute(_ => { });
        });

        await pipe.SendAsync(new LongPartitionContext(0x0102030405060708));

        Assert.Equal(new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }, hashGenerator.CapturedKey);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "guid-keys-use-the-guid-binary-contract")]
    public async Task Partitioner_GuidKeyOverloadsUseTheExactGuidBinaryRepresentationAsync()
    {
        var key = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        byte[] expected = key.ToByteArray();
        var hashGenerator = new CaptureHashGenerator();
        await using var shared = new PipePartitioner(2, hashGenerator);
        IPipe<GuidPartitionContext> sharedPipe = Pipe.New<GuidPartitionContext>(configuration =>
        {
            configuration.UsePartitioner(shared, context => context.Key);
            configuration.UseExecute(_ => { });
        });

        await sharedPipe.SendAsync(new GuidPartitionContext(key));

        Assert.Equal(expected, hashGenerator.CapturedKey);

        var dedicatedConfiguration = new RecordingPipeConfigurator<GuidPartitionContext>();
        dedicatedConfiguration.UsePartitioner(2, context => context.Key);
        var specification = Assert.IsType<PartitionerPipeSpecification<GuidPartitionContext>>(
            dedicatedConfiguration.Specification);
        FieldInfo keyProviderField = typeof(PartitionerPipeSpecification<GuidPartitionContext>).GetField(
            "_keyProvider",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The partition-key provider field was not found.");
        var keyProvider = Assert.IsType<PartitionKeyProvider<GuidPartitionContext>>(
            keyProviderField.GetValue(specification));

        Assert.Equal(expected, keyProvider(new GuidPartitionContext(key)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "explicit-admission-cancellation")]
    public async Task Partitioner_ObservesTheExplicitAdmissionCancellationTokenWhileQueuedAsync()
    {
        await using var partitioner = new PipePartitioner(1, new FirstByteHashGenerator());
        IPartitioner<PartitionContext> typedPartitioner = partitioner.GetPartitioner<PartitionContext>(context => context.Key);
        var firstEntered = NewSignal();
        var releaseFirst = NewSignal();
        var invocation = 0;
        IPipe<PartitionContext> next = Pipe.ExecuteAwaited<PartitionContext>(async _ =>
        {
            if (Interlocked.Increment(ref invocation) == 1)
            {
                firstEntered.SetResult();
                await releaseFirst.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            }
        });

        Task first = typedPartitioner.SendAsync(new PartitionContext([0]), next, TestContext.Current.CancellationToken);
        await firstEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task queued = typedPartitioner.SendAsync(new PartitionContext([0]), next, cancellation.Token);
        Assert.False(queued.IsCompleted);

        try
        {
            cancellation.Cancel();
            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                queued.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));

            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.Equal(1, Volatile.Read(ref invocation));
        }
        finally
        {
            releaseFirst.TrySetResult();
            await first.WaitAsync(OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "typed-and-untyped-probes-report-shared-partition-outcomes")]
    public async Task Partitioner_ProbesReportTheSharedIdentityCountAndOutcomesAsync()
    {
        await using var partitioner = new PipePartitioner(2, new FirstByteHashGenerator());
        IPartitioner<PartitionContext> typedPartitioner = partitioner.GetPartitioner<PartitionContext>(context => context.Key);

        await typedPartitioner.SendAsync(
            new PartitionContext([1]),
            Pipe.Execute<PartitionContext>(_ => { }),
            TestCancellationToken);

        string untypedProbe = JsonSerializer.Serialize(
            partitioner.GetProbeResult(TestCancellationToken).Results);
        string typedProbe = JsonSerializer.Serialize(
            typedPartitioner.GetProbeResult(TestCancellationToken).Results);

        Assert.Equal(untypedProbe, typedProbe);
        Assert.Contains("\"partitionCount\":2", untypedProbe, StringComparison.Ordinal);
        Assert.Contains("\"partition-0\"", untypedProbe, StringComparison.Ordinal);
        Assert.Contains("\"partition-1\"", untypedProbe, StringComparison.Ordinal);
        Assert.Contains("\"attemptCount\":1", untypedProbe, StringComparison.Ordinal);
        Assert.Contains("\"successCount\":1", untypedProbe, StringComparison.Ordinal);
        Assert.Contains("\"failureCount\":0", untypedProbe, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "draining-idempotent-disposal")]
    public async Task Partitioner_DisposalDrainsAcceptedOperationsAndRejectsNewOnesAsync()
    {
        var partitioner = new PipePartitioner(1, new FirstByteHashGenerator());
        var keyProviderInvocations = 0;
        IPartitioner<PartitionContext> typedPartitioner = partitioner.GetPartitioner<PartitionContext>(context =>
        {
            Interlocked.Increment(ref keyProviderInvocations);
            return context.Key;
        });
        var firstEntered = NewSignal();
        var releaseFirst = NewSignal();
        var invocation = 0;
        IPipe<PartitionContext> next = Pipe.ExecuteAwaited<PartitionContext>(async _ =>
        {
            if (Interlocked.Increment(ref invocation) == 1)
            {
                firstEntered.SetResult();
                await releaseFirst.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            }
        });

        Task first = typedPartitioner.SendAsync(new PartitionContext([0]), next, TestContext.Current.CancellationToken);
        await firstEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        Task second = typedPartitioner.SendAsync(new PartitionContext([0]), next, TestContext.Current.CancellationToken);

        Task firstDisposal = partitioner.DisposeAsync().AsTask();
        Task secondDisposal = partitioner.DisposeAsync().AsTask();
        Assert.False(firstDisposal.IsCompleted);
        Assert.False(secondDisposal.IsCompleted);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            typedPartitioner.SendAsync(new PartitionContext([0]), next, TestContext.Current.CancellationToken));
        Assert.Throws<ObjectDisposedException>(() =>
            partitioner.GetPartitioner<PartitionContext>(context => context.Key));

        releaseFirst.SetResult();
        await Task.WhenAll(first, second, firstDisposal, secondDisposal).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(2, invocation);
        Assert.Equal(2, keyProviderInvocations);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static void AssertParameter<TException>(string parameterName, Action action)
        where TException : ArgumentException
    {
        TException exception = Assert.Throws<TException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private sealed class PartitionContext(byte[] key) : BasePipeContext
    {
        public byte[] Key { get; } = key;
    }

    private sealed class StringPartitionContext(string key) : BasePipeContext
    {
        public string Key { get; } = key;
    }

    private sealed class LongPartitionContext(long key) : BasePipeContext
    {
        public long Key { get; } = key;
    }

    private sealed class GuidPartitionContext(Guid key) : BasePipeContext
    {
        public Guid Key { get; } = key;
    }

    private sealed class RecordingPipeConfigurator<TContext> : IPipeConfigurator<TContext>
        where TContext : class, PipeContext
    {
        public IPipeSpecification<TContext>? Specification { get; private set; }

        public void AddPipeSpecification(IPipeSpecification<TContext> specification)
        {
            Assert.Null(Specification);
            Specification = specification;
        }
    }

    private sealed class CaptureHashGenerator(uint hash = 0) : IPartitionHashGenerator
    {
        public byte[]? CapturedKey { get; private set; }

        public int InvocationCount { get; private set; }

        public uint ComputeHash(ReadOnlySpan<byte> partitionKey)
        {
            InvocationCount++;
            CapturedKey = partitionKey.ToArray();
            return hash;
        }
    }

    private sealed class FirstByteHashGenerator : IPartitionHashGenerator
    {
        public uint ComputeHash(ReadOnlySpan<byte> partitionKey) => partitionKey[0];
    }
}
