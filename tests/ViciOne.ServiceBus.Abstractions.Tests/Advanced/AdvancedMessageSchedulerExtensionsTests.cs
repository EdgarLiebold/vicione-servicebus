using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class AdvancedMessageSchedulerExtensionsTests
{
    static readonly Uri Destination = new("loopback://localhost/scheduled");
    static readonly DateTimeOffset DueAt = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER", "capability-validation")]
    public void Advanced_RejectsMissingAndUnsupportedSchedulers()
    {
        Assert.Equal(
            "scheduler",
            Assert.Throws<ArgumentNullException>(() => AdvancedMessageSchedulerExtensions.Advanced(null!)).ParamName);

        IMessageScheduler basic = CreateProxy<IMessageScheduler>(out _);
        Assert.Throws<NotSupportedException>(() => basic.Advanced());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER", "send-required-inputs")]
    public void ScheduleSendOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        IMessageScheduler scheduler = CreateProxy<IAdvancedMessageScheduler>(out RecordingSchedulerProxy proxy);
        var message = new ScheduledContract("value");
        IPipe<SendContext<ScheduledContract>> typedPipe = Pipe.Empty<SendContext<ScheduledContract>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("destination", () => scheduler.ScheduleSendAsync(null!, DueAt, message, typedPipe, cancellationToken), proxy);
        AssertNull("message", () => scheduler.ScheduleSendAsync<ScheduledContract>(Destination, DueAt, null!, typedPipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, message, (IPipe<SendContext<ScheduledContract>>)null!, cancellationToken), proxy);

        AssertNull("destination", () => scheduler.ScheduleSendAsync(null!, DueAt, message, pipe, cancellationToken), proxy);
        AssertNull("message", () => scheduler.ScheduleSendAsync<ScheduledContract>(Destination, DueAt, null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, message, (IPipe<SendContext>)null!, cancellationToken), proxy);

        AssertNull("destination", () => scheduler.ScheduleSendAsync(null!, DueAt, (object)message, typeof(ScheduledContract), cancellationToken), proxy);
        AssertNull("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, typeof(ScheduledContract), cancellationToken), proxy);
        AssertNull("messageType", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, (Type)null!, cancellationToken), proxy);

        AssertNull("destination", () => scheduler.ScheduleSendAsync(null!, DueAt, (object)message, pipe, cancellationToken), proxy);
        AssertNull("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, (IPipe<SendContext>)null!, cancellationToken), proxy);

        AssertNull("destination", () => scheduler.ScheduleSendAsync(null!, DueAt, (object)message, typeof(ScheduledContract), pipe, cancellationToken), proxy);
        AssertNull("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, typeof(ScheduledContract), pipe, cancellationToken), proxy);
        AssertNull("messageType", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, (Type)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, typeof(ScheduledContract), null!, cancellationToken), proxy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER", "publish-required-inputs")]
    public void SchedulePublishOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        IMessageScheduler scheduler = CreateProxy<IAdvancedMessageScheduler>(out RecordingSchedulerProxy proxy);
        var message = new ScheduledContract("value");
        IPipe<SendContext<ScheduledContract>> typedPipe = Pipe.Empty<SendContext<ScheduledContract>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("message", () => scheduler.SchedulePublishAsync<ScheduledContract>(DueAt, null!, typedPipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.SchedulePublishAsync(DueAt, message, (IPipe<SendContext<ScheduledContract>>)null!, cancellationToken), proxy);
        AssertNull("message", () => scheduler.SchedulePublishAsync<ScheduledContract>(DueAt, null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.SchedulePublishAsync(DueAt, message, (IPipe<SendContext>)null!, cancellationToken), proxy);

        AssertNull("message", () => scheduler.SchedulePublishAsync(DueAt, (object)null!, typeof(ScheduledContract), cancellationToken), proxy);
        AssertNull("messageType", () => scheduler.SchedulePublishAsync(DueAt, (object)message, (Type)null!, cancellationToken), proxy);
        AssertNull("message", () => scheduler.SchedulePublishAsync(DueAt, (object)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.SchedulePublishAsync(DueAt, (object)message, (IPipe<SendContext>)null!, cancellationToken), proxy);
        AssertNull("message", () => scheduler.SchedulePublishAsync(DueAt, (object)null!, typeof(ScheduledContract), pipe, cancellationToken), proxy);
        AssertNull("messageType", () => scheduler.SchedulePublishAsync(DueAt, (object)message, (Type)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => scheduler.SchedulePublishAsync(DueAt, (object)message, typeof(ScheduledContract), null!, cancellationToken), proxy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER", "cancellation-identity-validation")]
    public void CancellationOverloads_RejectMissingIdentityBeforeProviderUse()
    {
        IMessageScheduler scheduler = CreateProxy<IAdvancedMessageScheduler>(out RecordingSchedulerProxy proxy);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("destination", () => scheduler.CancelScheduledSendAsync(null!, Guid.NewGuid(), cancellationToken), proxy);
        AssertArgument("tokenId", () => scheduler.CancelScheduledSendAsync(Destination, Guid.Empty, cancellationToken), proxy);
        AssertArgument("tokenId", () => scheduler.CancelScheduledPublishAsync<ScheduledContract>(Guid.Empty, cancellationToken), proxy);
        AssertNull("messageType", () => scheduler.CancelScheduledPublishAsync((Type)null!, Guid.NewGuid(), cancellationToken), proxy);
        AssertArgument("tokenId", () => scheduler.CancelScheduledPublishAsync(typeof(ScheduledContract), Guid.Empty, cancellationToken), proxy);
    }

    static TContract CreateProxy<TContract>(out RecordingSchedulerProxy proxy)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, RecordingSchedulerProxy>();
        proxy = (RecordingSchedulerProxy)(object)contract;
        return contract;
    }

    static void AssertNull(string parameterName, Action operation, RecordingSchedulerProxy proxy)
    {
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);
        Assert.Equal(0, proxy.InvocationCount);
    }

    static void AssertArgument(string parameterName, Action operation, RecordingSchedulerProxy proxy)
    {
        Assert.Equal(parameterName, Assert.Throws<ArgumentException>(operation).ParamName);
        Assert.Equal(0, proxy.InvocationCount);
    }

    sealed record ScheduledContract(string Value);

    class RecordingSchedulerProxy : DispatchProxy
    {
        public int InvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            InvocationCount++;
            throw new InvalidOperationException($"The provider must not be invoked for invalid input: {targetMethod?.Name}");
        }
    }
}
