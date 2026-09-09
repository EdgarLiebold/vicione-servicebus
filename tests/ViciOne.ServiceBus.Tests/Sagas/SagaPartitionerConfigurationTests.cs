using System.Reflection;
using System.Text;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaPartitionerConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "saga-guid-and-text-keys-have-exact-binary-contracts")]
    public async Task SagaPartitioner_GuidAndTextKeysUseTheirExactBinaryContractsAsync()
    {
        var saga = new PartitionedSaga
        {
            CorrelationId = NewId.NextGuid(),
            TextKey = "Å",
        };
        using InMemorySagaConsumeContext<PartitionedSaga, PartitionedSagaMessage> context =
            await CreateContextAsync(saga);

        var guidConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PartitionedSaga>>();
        guidConfiguration.UsePartitioner(4, sagaContext => sagaContext.Saga.CorrelationId);
        var explicitTextConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PartitionedSaga>>();
        explicitTextConfiguration.UsePartitioner(4, sagaContext => sagaContext.Saga.TextKey, Encoding.BigEndianUnicode);
        var defaultTextConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PartitionedSaga>>();
        defaultTextConfiguration.UsePartitioner(4, sagaContext => sagaContext.Saga.TextKey);

        Assert.Equal(saga.CorrelationId.ToByteArray(), SelectKey(guidConfiguration.Specification, context));
        Assert.Equal(new byte[] { 0x00, 0xc5 }, SelectKey(explicitTextConfiguration.Specification, context));
        Assert.Equal(new byte[] { 0xc3, 0x85 }, SelectKey(defaultTextConfiguration.Specification, context));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "saga-null-text-key-is-rejected-before-downstream")]
    public async Task SagaPartitioner_RejectsANullTextKeyBeforeInvokingTheDownstreamPipeAsync()
    {
        var downstreamInvocationCount = 0;
        IPipe<SagaConsumeContext<PartitionedSaga>> pipe = Pipe.New<SagaConsumeContext<PartitionedSaga>>(configuration =>
        {
            configuration.UsePartitioner(2, context => context.Saga.TextKey);
            configuration.UseExecute(_ => Interlocked.Increment(ref downstreamInvocationCount));
        });
        var saga = new PartitionedSaga
        {
            CorrelationId = NewId.NextGuid(),
            TextKey = null!,
        };
        using InMemorySagaConsumeContext<PartitionedSaga, PartitionedSagaMessage> context =
            await CreateContextAsync(saga);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipe.SendAsync(context));

        Assert.Equal("The partition key provider returned null.", exception.Message);
        Assert.Equal(0, Volatile.Read(ref downstreamInvocationCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITIONER", "saga-overloads-reject-invalid-required-arguments")]
    public void SagaPartitionerOverloads_RejectEveryInvalidRequiredArgument()
    {
        var configuration = new RecordingPipeConfigurator<SagaConsumeContext<PartitionedSaga>>();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SagaPipelineConfigurationExtensions.UsePartitioner<PartitionedSaga>(
                null!,
                2,
                context => context.Saga.CorrelationId)).ParamName);
        Assert.Equal("keyProvider", Assert.Throws<ArgumentNullException>(() =>
            configuration.UsePartitioner(2, (Func<SagaConsumeContext<PartitionedSaga>, Guid>)null!)).ParamName);
        Assert.Equal("partitionCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            configuration.UsePartitioner(0, context => context.Saga.CorrelationId)).ParamName);

        configuration = new RecordingPipeConfigurator<SagaConsumeContext<PartitionedSaga>>();
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SagaPipelineConfigurationExtensions.UsePartitioner<PartitionedSaga>(
                null!,
                2,
                context => context.Saga.TextKey)).ParamName);
        Assert.Equal("keyProvider", Assert.Throws<ArgumentNullException>(() =>
            configuration.UsePartitioner(2, (Func<SagaConsumeContext<PartitionedSaga>, string>)null!)).ParamName);
        Assert.Equal("partitionCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            configuration.UsePartitioner(0, context => context.Saga.TextKey)).ParamName);
    }

    private static async Task<InMemorySagaConsumeContext<PartitionedSaga, PartitionedSagaMessage>> CreateContextAsync(
        PartitionedSaga saga)
    {
        ConsumeContext<PartitionedSagaMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new PartitionedSagaMessage(),
            TestContext.Current.CancellationToken);
        var instance = new SagaInstance<PartitionedSaga>(saga);
        await instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        return new InMemorySagaConsumeContext<PartitionedSaga, PartitionedSagaMessage>(consumeContext, instance);
    }

    private static byte[] SelectKey(
        IPipeSpecification<SagaConsumeContext<PartitionedSaga>>? specification,
        SagaConsumeContext<PartitionedSaga> context)
    {
        Assert.NotNull(specification);
        FieldInfo keyProviderField = specification.GetType().GetField(
            "_keyProvider",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The saga partition-key provider field was not found.");
        var keyProvider = Assert.IsAssignableFrom<Delegate>(keyProviderField.GetValue(specification));
        return Assert.IsType<byte[]>(keyProvider.DynamicInvoke(context));
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

    public sealed class PartitionedSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public string TextKey { get; set; } = string.Empty;
    }

    public sealed record PartitionedSagaMessage;
}
