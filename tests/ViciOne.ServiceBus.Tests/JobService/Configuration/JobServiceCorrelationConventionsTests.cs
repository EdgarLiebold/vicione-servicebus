using System.Reflection;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobServiceCorrelationConventionsTests
{
    private static readonly Guid JobTypeId = Guid.Parse("11000000-0000-0000-0000-000000000011");
    private static readonly Guid JobId = Guid.Parse("22000000-0000-0000-0000-000000000022");
    private static readonly Guid AttemptId = Guid.Parse("33000000-0000-0000-0000-000000000033");

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CORRELATION", "every-coordination-contract-selects-its-owning-identity")]
    public void EveryCoordinationContract_SelectsTheExactCorrelationIdentity()
    {
        JobServiceCorrelationConventions.Register();

        AssertCorrelation<IAllocateJobSlot>(JobTypeId, static proxy => proxy.JobTypeId = Guid.Empty);
        AssertCorrelation<IJobSlotReleased>(JobTypeId, static proxy => proxy.JobTypeId = Guid.Empty);
        AssertCorrelation<ISetConcurrentJobLimit>(JobTypeId, static proxy => proxy.JobTypeId = Guid.Empty);

        AssertCorrelation<ICancelJob>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<Fault<IAllocateJobSlot>>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<Fault<IStartJobAttempt>>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IFinalizeJob>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IGetJobState>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobAttemptCanceled>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobAttemptCompleted>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobAttemptFaulted>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobAttemptStarted>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobCanceled>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobCompleted>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobRetryDelayElapsed>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobSlotAllocated>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobSlotUnavailable>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobSlotWaitElapsed>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IJobSubmitted>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IRetryJob>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IRunJob>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<ISaveJobCheckpoint>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<ISetJobProgress>(JobId, static proxy => proxy.JobId = Guid.Empty);
        AssertCorrelation<IStartJob>(JobId, static proxy => proxy.JobId = Guid.Empty);

        AssertCorrelation<IStartJobAttempt>(AttemptId, static proxy => proxy.AttemptId = Guid.Empty);
        AssertCorrelation<IFinalizeJobAttempt>(AttemptId, static proxy => proxy.AttemptId = Guid.Empty);
        AssertCorrelation<ICancelJobAttempt>(AttemptId, static proxy => proxy.AttemptId = Guid.Empty);
        AssertCorrelation<Fault<IStartJob>>(AttemptId, static proxy => proxy.AttemptId = Guid.Empty);
        AssertCorrelation<IJobAttemptStatus>(AttemptId, static proxy => proxy.AttemptId = Guid.Empty);
        AssertCorrelation<IJobStatusCheckRequested>(AttemptId, static proxy => proxy.AttemptId = Guid.Empty);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CORRELATION", "registration-is-idempotent-after-topology-freeze")]
    public void Registration_RemainsIdempotentAfterTheGlobalTopologyFreezes()
    {
        JobServiceCorrelationConventions.Register();
        IMessageCorrelationId<IAllocateJobSlot> before = GetResolver<IAllocateJobSlot>();

        Parallel.For(0, 64, _ => JobServiceCorrelationConventions.Register());

        Assert.Same(before, GetResolver<IAllocateJobSlot>());
    }

    private static void AssertCorrelation<T>(Guid expected, Action<CoordinationContractProxy> clearSelected)
        where T : class
    {
        IMessageCorrelationId<T> resolver = GetResolver<T>();
        T selected = CreateContract<T>(out _);
        Assert.True(resolver.TryGetCorrelationId(selected, out Guid actual));
        Assert.Equal(expected, actual);

        T empty = CreateContract<T>(out CoordinationContractProxy emptyProxy);
        clearSelected(emptyProxy);
        Assert.False(resolver.TryGetCorrelationId(empty, out Guid emptyValue));
        Assert.Equal(Guid.Empty, emptyValue);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(
            () => resolver.TryGetCorrelationId(null!, out _)).ParamName);
    }

    private static IMessageCorrelationId<T> GetResolver<T>()
        where T : class
    {
        Assert.True(GlobalTopology.Send.GetMessageTopology<T>().TryGetConvention(
            out ICorrelationIdMessageSendTopologyConvention<T>? convention));
        Assert.NotNull(convention);
        Assert.True(convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<T>? resolver));
        return Assert.IsAssignableFrom<IMessageCorrelationId<T>>(resolver);
    }

    private static T CreateContract<T>(out CoordinationContractProxy proxy)
        where T : class
    {
        T contract = DispatchProxy.Create<T, CoordinationContractProxy>();
        proxy = (CoordinationContractProxy)(object)contract;
        proxy.JobTypeId = JobTypeId;
        proxy.JobId = JobId;
        proxy.AttemptId = AttemptId;
        return contract;
    }

    private class CoordinationContractProxy : DispatchProxy
    {
        public Guid JobTypeId { get; set; }
        public Guid JobId { get; set; }
        public Guid AttemptId { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_JobTypeId" => JobTypeId,
                "get_JobId" => JobId,
                "get_AttemptId" => AttemptId,
                "get_Message" when targetMethod.ReturnType.IsInterface => CreateNested(targetMethod.ReturnType),
                _ => throw new NotSupportedException($"Unexpected job contract member: {targetMethod?.Name}"),
            };
        }

        private object CreateNested(Type contract)
        {
            object nested = DispatchProxy.Create(contract, typeof(CoordinationContractProxy));
            var proxy = (CoordinationContractProxy)nested;
            proxy.JobTypeId = JobTypeId;
            proxy.JobId = JobId;
            proxy.AttemptId = AttemptId;
            return nested;
        }
    }
}
