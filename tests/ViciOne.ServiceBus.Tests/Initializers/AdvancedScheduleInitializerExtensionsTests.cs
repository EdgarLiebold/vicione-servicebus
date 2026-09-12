using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class AdvancedScheduleInitializerExtensionsTests
{
    static readonly Uri Destination = new("loopback://localhost/scheduled");
    static readonly DateTimeOffset DueAt = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULE-INITIALIZER", "exact-overload-forwarding")]
    public async Task InitializerOverloads_ForwardExactValuesPipesAndCancellationAsync()
    {
        IMessageScheduler scheduler = CreateProxy<IAdvancedMessageScheduler>(out RecordingSchedulerProxy proxy);
        proxy.ThrowOnInvocation = false;
        var values = new { Value = "message" };
        IPipe<SendContext<MessageContract>> typedPipe = Pipe.Empty<SendContext<MessageContract>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _ = await AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, Destination, DueAt, values, cancellationToken);
        _ = await AdvancedScheduleInitializerExtensions.ScheduleSendAsync(
            scheduler, Destination, DueAt, values, typedPipe, cancellationToken);
        _ = await AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, Destination, DueAt, values, pipe, cancellationToken);
        _ = await AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            scheduler, DueAt, values, cancellationToken);
        _ = await AdvancedScheduleInitializerExtensions.SchedulePublishAsync(
            scheduler, DueAt, values, typedPipe, cancellationToken);
        _ = await AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            scheduler, DueAt, values, pipe, cancellationToken);

        Assert.Equal(6, proxy.InvocationCount);
        AssertInvocation(proxy.Invocations[0], isSend: true, values, null, cancellationToken);
        AssertInvocation(proxy.Invocations[1], isSend: true, values, typedPipe, cancellationToken);
        AssertInvocation(proxy.Invocations[2], isSend: true, values, pipe, cancellationToken);
        AssertInvocation(proxy.Invocations[3], isSend: false, values, null, cancellationToken);
        AssertInvocation(proxy.Invocations[4], isSend: false, values, typedPipe, cancellationToken);
        AssertInvocation(proxy.Invocations[5], isSend: false, values, pipe, cancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULE-INITIALIZER", "capability-validation")]
    public void InitializerOverloads_RejectMissingAndUnsupportedSchedulers()
    {
        var values = new { Value = "message" };
        IMessageScheduler basicScheduler = CreateProxy<IMessageScheduler>(out _);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("scheduler", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            null!, Destination, DueAt, values, cancellationToken));
        AssertNull("scheduler", () => AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            null!, DueAt, values, cancellationToken));
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
                basicScheduler, Destination, DueAt, values, cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
                basicScheduler, DueAt, values, cancellationToken);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULE-INITIALIZER", "send-required-inputs-before-provider-use")]
    public void ScheduleSendOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        IMessageScheduler scheduler = CreateProxy<IAdvancedMessageScheduler>(out RecordingSchedulerProxy proxy);
        var values = new { Value = "message" };
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("destination", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, null!, DueAt, values, cancellationToken));
        AssertNull("values", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, Destination, DueAt, null!, cancellationToken));

        AssertNull("destination", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, null!, DueAt, values, Pipe.Empty<SendContext<MessageContract>>(), cancellationToken));
        AssertNull("values", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, Destination, DueAt, null!, Pipe.Empty<SendContext<MessageContract>>(), cancellationToken));
        AssertNull("pipe", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, Destination, DueAt, values, (IPipe<SendContext<MessageContract>>)null!, cancellationToken));

        AssertNull("destination", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, null!, DueAt, values, Pipe.Empty<SendContext>(), cancellationToken));
        AssertNull("values", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, Destination, DueAt, null!, Pipe.Empty<SendContext>(), cancellationToken));
        AssertNull("pipe", () => AdvancedScheduleInitializerExtensions.ScheduleSendAsync<MessageContract>(
            scheduler, Destination, DueAt, values, (IPipe<SendContext>)null!, cancellationToken));

        Assert.Equal(0, proxy.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULE-INITIALIZER", "publish-required-inputs-before-provider-use")]
    public void SchedulePublishOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        IMessageScheduler scheduler = CreateProxy<IAdvancedMessageScheduler>(out RecordingSchedulerProxy proxy);
        var values = new { Value = "message" };
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("values", () => AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            scheduler, DueAt, null!, cancellationToken));
        AssertNull("values", () => AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            scheduler, DueAt, null!, Pipe.Empty<SendContext<MessageContract>>(), cancellationToken));
        AssertNull("pipe", () => AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            scheduler, DueAt, values, (IPipe<SendContext<MessageContract>>)null!, cancellationToken));
        AssertNull("values", () => AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            scheduler, DueAt, null!, Pipe.Empty<SendContext>(), cancellationToken));
        AssertNull("pipe", () => AdvancedScheduleInitializerExtensions.SchedulePublishAsync<MessageContract>(
            scheduler, DueAt, values, (IPipe<SendContext>)null!, cancellationToken));

        Assert.Equal(0, proxy.InvocationCount);
    }

    static TContract CreateProxy<TContract>(out RecordingSchedulerProxy proxy)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, RecordingSchedulerProxy>();
        proxy = (RecordingSchedulerProxy)(object)contract;
        return contract;
    }

    static void AssertNull(string parameterName, Action operation) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);

    static void AssertInvocation(Invocation invocation, bool isSend, object values, object? pipe,
        CancellationToken cancellationToken)
    {
        Assert.Equal(typeof(MessageContract), Assert.Single(invocation.Method.GetGenericArguments()));
        int valuesIndex = isSend ? 2 : 1;
        Assert.Equal(DueAt, invocation.Arguments[isSend ? 1 : 0]);
        if (isSend)
            Assert.Equal(Destination, invocation.Arguments[0]);
        Assert.Same(values, invocation.Arguments[valuesIndex]);
        if (pipe is null)
            Assert.Equal(cancellationToken, invocation.Arguments[valuesIndex + 1]);
        else
        {
            Assert.Same(pipe, invocation.Arguments[valuesIndex + 1]);
            Assert.Equal(cancellationToken, invocation.Arguments[valuesIndex + 2]);
        }
    }

    sealed record MessageContract(string Value);

    sealed record Invocation(MethodInfo Method, object?[] Arguments);

    class RecordingSchedulerProxy : DispatchProxy
    {
        public List<Invocation> Invocations { get; } = [];

        public int InvocationCount => Invocations.Count;

        public bool ThrowOnInvocation { get; set; } = true;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Invocations.Add(new Invocation(targetMethod, args ?? []));
            if (ThrowOnInvocation)
                throw new InvalidOperationException($"The provider must not be invoked for invalid input: {targetMethod.Name}");

            Type resultType = targetMethod.ReturnType.GetGenericArguments()[0];
            MethodInfo factory = typeof(RecordingSchedulerProxy)
                .GetMethod(nameof(CompletedTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return factory.Invoke(null, null);
        }

        static Task<TResult?> CompletedTaskAsync<TResult>() => Task.FromResult<TResult?>(default);
    }
}
