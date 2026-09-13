using System.Reflection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

public sealed class JobServiceEventExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-EVENT-ACCESS", "every-lifecycle-event-deserializes-its-job-payload")]
    public void GetJob_DeserializesEveryLifecycleEventFromItsOwnPayload()
    {
        var expected = new EventJob("invoice-42");
        var payload = new Dictionary<string, object> { ["value"] = expected.Value };

        Assert.Same(expected, CreateContext<StartJob>(payload, expected).GetJob<EventJob>());
        Assert.Same(expected, CreateContext<FaultJob>(payload, expected).GetJob<EventJob>());
        Assert.Same(expected, CreateContext<CompleteJob>(payload, expected).GetJob<EventJob>());
        Assert.Same(expected, CreateContext<JobCompleted>(payload, expected).GetJob<EventJob>());
        Assert.Same(expected, CreateContext<JobFaulted>(payload, expected).GetJob<EventJob>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-EVENT-ACCESS", "every-lifecycle-event-rejects-a-missing-context")]
    public void GetJob_RejectsMissingLifecycleEventContexts()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            JobServiceEventExtensions.GetJob<EventJob>((ConsumeContext<StartJob>)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            JobServiceEventExtensions.GetJob<EventJob>((ConsumeContext<FaultJob>)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            JobServiceEventExtensions.GetJob<EventJob>((ConsumeContext<CompleteJob>)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            JobServiceEventExtensions.GetJob<EventJob>((ConsumeContext<JobCompleted>)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            JobServiceEventExtensions.GetJob<EventJob>((ConsumeContext<JobFaulted>)null!)).ParamName);
    }

    private static ConsumeContext<TContract> CreateContext<TContract>(
        IReadOnlyDictionary<string, object> payload,
        EventJob result)
        where TContract : class
    {
        TContract message = DispatchProxy.Create<TContract, JobEventProxy>();
        ((JobEventProxy)(object)message).Payload = payload;
        TestConsumeContext<TContract> context = DispatchProxy.Create<TestConsumeContext<TContract>, ConsumeContextProxy<TContract>>();
        var contextProxy = (ConsumeContextProxy<TContract>)(object)context;
        contextProxy.Message = message;
        contextProxy.SerializerContext = DispatchProxy.Create<SerializerContext, SerializerContextProxy>();
        ((SerializerContextProxy)(object)contextProxy.SerializerContext).Result = result;
        return context;
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private class ConsumeContextProxy<T> : DispatchProxy
        where T : class
    {
        public T Message { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Message" => Message,
            "get_SerializerContext" => SerializerContext,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private class JobEventProxy : DispatchProxy
    {
        public IReadOnlyDictionary<string, object> Payload { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Job" => Payload,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private class SerializerContextProxy : DispatchProxy
    {
        public object Result { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IObjectDeserializer.DeserializeObject))
                throw new NotSupportedException(targetMethod?.Name);

            Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(args![0]);
            Assert.Equal(typeof(EventJob), targetMethod.GetGenericArguments()[0]);
            return Result;
        }
    }

    private sealed record EventJob(string Value);
}
