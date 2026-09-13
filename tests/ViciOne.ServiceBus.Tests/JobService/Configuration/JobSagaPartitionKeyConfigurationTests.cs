using System.Reflection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobSagaPartitionKeyConfigurationTests
{
    private static readonly Guid JobTypeId = Guid.Parse("11000000-0000-0000-0000-000000000011");
    private static readonly Guid JobId = Guid.Parse("22000000-0000-0000-0000-000000000022");
    private static readonly Guid AttemptId = Guid.Parse("33000000-0000-0000-0000-000000000033");

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-PARTITION-TOPOLOGY", "every-coordination-contract-uses-its-owning-identity")]
    public async Task UseJobSagaPartitionKeyFormatters_MapsEveryCoordinationContractToItsOwningIdentityAsync()
    {
        var sendTopology = new SendTopology();
        Assert.True(sendTopology.TryAddConvention(new PartitionKeySendTopologyConvention()));
        IBusFactoryConfigurator bus = CreateBusConfigurator(sendTopology);

        bus.UseJobSagaPartitionKeyFormatters();

        await AssertPartitionKeyAsync<IAllocateJobSlot>(sendTopology, JobTypeId);
        await AssertPartitionKeyAsync<IJobSlotReleased>(sendTopology, JobTypeId);
        await AssertPartitionKeyAsync<ISetConcurrentJobLimit>(sendTopology, JobTypeId);

        await AssertPartitionKeyAsync<IJobSubmitted>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IJobSlotAllocated>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IJobSlotUnavailable>(sendTopology, JobId);
        await AssertPartitionKeyAsync<Fault<IAllocateJobSlot>>(sendTopology, JobId);
        await AssertPartitionKeyAsync<Fault<IStartJobAttempt>>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IJobCompleted>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IGetJobState>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IStartJob>(sendTopology, JobId);
        await AssertPartitionKeyAsync<ICancelJob>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IRetryJob>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IRunJob>(sendTopology, JobId);
        await AssertPartitionKeyAsync<ISaveJobCheckpoint>(sendTopology, JobId);
        await AssertPartitionKeyAsync<ISetJobProgress>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IJobSlotWaitElapsed>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IJobRetryDelayElapsed>(sendTopology, JobId);
        await AssertPartitionKeyAsync<ICompleteJob>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IFaultJob>(sendTopology, JobId);
        await AssertPartitionKeyAsync<IGetJobAttemptStatus>(sendTopology, JobId);

        await AssertPartitionKeyAsync<IJobAttemptCanceled>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<IJobAttemptCompleted>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<IJobAttemptFaulted>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<IJobAttemptStarted>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<IStartJobAttempt>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<IFinalizeJobAttempt>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<ICancelJobAttempt>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<Fault<IStartJob>>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<IJobAttemptStatus>(sendTopology, AttemptId);
        await AssertPartitionKeyAsync<IJobStatusCheckRequested>(sendTopology, AttemptId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-PARTITION-TOPOLOGY", "null-bus-configurator-rejected")]
    public void UseJobSagaPartitionKeyFormatters_RejectsANullBusConfigurator()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => JobSagaBusConfigurationExtensions.UseJobSagaPartitionKeyFormatters(null!));

        Assert.Equal("configurator", exception.ParamName);
    }

    private static async Task AssertPartitionKeyAsync<TMessage>(SendTopology topology, Guid expected)
        where TMessage : class
    {
        IMessageSendTopologyConfigurator<TMessage> messageTopology = topology.GetMessageTopology<TMessage>();
        Assert.True(messageTopology.TryGetConvention(out IPartitionKeyMessageSendTopologyConvention<TMessage>? convention));
        Assert.NotNull(convention);
        Assert.True(convention.TryGetMessageSendTopology(out IMessageSendTopology<TMessage>? configuredTopology));
        Assert.NotNull(configuredTopology);

        var builder = new RecordingTopologyPipeBuilder<TMessage>();
        configuredTopology.Apply(builder);
        var partitionKey = new RecordingPartitionKeyContext();
        var context = new MessageSendContext<TMessage>(CreateContract<TMessage>());
        context.GetOrAddPayload(() => partitionKey);

        await builder.Build().SendAsync(context);

        Assert.Equal(expected.ToString("N"), partitionKey.PartitionKey);
    }

    private static TContract CreateContract<TContract>()
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, JobContractProxy>();
        ((JobContractProxy)(object)contract).Initialize(JobTypeId, JobId, AttemptId);
        return contract;
    }

    private static IBusFactoryConfigurator CreateBusConfigurator(SendTopology sendTopology)
    {
        IBusFactoryConfigurator configurator = DispatchProxy.Create<IBusFactoryConfigurator, BusConfiguratorProxy>();
        ((BusConfiguratorProxy)(object)configurator).SendTopology = sendTopology;
        return configurator;
    }

    private sealed class RecordingTopologyPipeBuilder<TMessage> : ITopologyPipeBuilder<SendContext<TMessage>>
        where TMessage : class
    {
        private readonly PipeConfigurator<SendContext<TMessage>>.PipeBuilder _builder = new();

        public bool IsDelegated => false;

        public bool IsImplemented => false;

        public void AddFilter(IFilter<SendContext<TMessage>> filter) => _builder.AddFilter(filter);

        public ITopologyPipeBuilder<SendContext<TMessage>> CreateDelegatedBuilder() => this;

        public IPipe<SendContext<TMessage>> Build() => _builder.Build();
    }

    private sealed class RecordingPartitionKeyContext : PartitionKeySendContext
    {
        public string? PartitionKey { get; set; }
    }

    private class BusConfiguratorProxy : DispatchProxy
    {
        public ISendTopologyConfigurator SendTopology { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_SendTopology")
                return SendTopology;

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class JobContractProxy : DispatchProxy
    {
        private Guid _jobTypeId;
        private Guid _jobId;
        private Guid _attemptId;

        public void Initialize(Guid jobTypeId, Guid jobId, Guid attemptId)
        {
            _jobTypeId = jobTypeId;
            _jobId = jobId;
            _attemptId = attemptId;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_JobTypeId")
                return _jobTypeId;
            if (targetMethod.Name == "get_JobId")
                return _jobId;
            if (targetMethod.Name == "get_AttemptId")
                return _attemptId;
            if (targetMethod.Name == "get_Message" && targetMethod.ReturnType.IsInterface)
            {
                object nested = DispatchProxy.Create(targetMethod.ReturnType, typeof(JobContractProxy));
                ((JobContractProxy)nested).Initialize(_jobTypeId, _jobId, _attemptId);
                return nested;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
