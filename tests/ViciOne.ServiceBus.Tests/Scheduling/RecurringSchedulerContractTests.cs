using System.Reflection;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class RecurringSchedulerContractTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/recurring-boundary");

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "constructors-and-command-values-reject-null")]
    public void ConstructorsAndCommandValues_RejectNullCollaborators()
    {
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        ISendEndpointProvider endpointProvider = DispatchProxy.Create<ISendEndpointProvider, UnexpectedInvocationProxy>();
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var message = new ScheduledMessage();

        Assert.Equal("sendEndpointProvider", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRecurringMessageScheduler(null!, DestinationAddress)).ParamName);
        Assert.Equal("schedulerAddress", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRecurringMessageScheduler(endpointProvider, null!)).ParamName);
        Assert.Equal("sendEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRecurringMessageScheduler((ISendEndpoint)null!)).ParamName);
        Assert.Equal("publishEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new PublishRecurringMessageScheduler(null!)).ParamName);

        Assert.Equal("schedule", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleRecurringMessageCommand<ScheduledMessage>(null!, DestinationAddress, message)).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleRecurringMessageCommand<ScheduledMessage>(schedule, null!, message)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleRecurringMessageCommand<ScheduledMessage>(schedule, DestinationAddress, null!)).ParamName);
        Assert.Equal("schedule", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledRecurringMessageHandle<ScheduledMessage>(null!, DestinationAddress, message)).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledRecurringMessageHandle<ScheduledMessage>(schedule, null!, message)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledRecurringMessageHandle<ScheduledMessage>(schedule, DestinationAddress, null!)).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleMessageCommand<ScheduledMessage>(new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero), null!, message,
                NewId.NextGuid())).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleMessageCommand<ScheduledMessage>(new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero), DestinationAddress, null!,
                NewId.NextGuid())).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledMessageHandle<ScheduledMessage>(NewId.NextGuid(), new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero), null!,
                message)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledMessageHandle<ScheduledMessage>(NewId.NextGuid(), new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero),
                DestinationAddress, null!)).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "control-identities-reject-null-or-whitespace")]
    public async Task ControlOperations_RejectMissingScheduleIdentifiersAsync(string? missingValue)
    {
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        IPublishEndpoint publishEndpoint = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        IRecurringMessageScheduler[] schedulers =
        [
            new EndpointRecurringMessageScheduler(endpoint),
            new PublishRecurringMessageScheduler(publishEndpoint)
        ];

        foreach (IRecurringMessageScheduler scheduler in schedulers)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                scheduler.CancelScheduledRecurringSendAsync(missingValue!, "group", TestContext.Current.CancellationToken));
            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                scheduler.PauseScheduledRecurringSendAsync("schedule", missingValue!, TestContext.Current.CancellationToken));
            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                scheduler.ResumeScheduledRecurringSendAsync(missingValue!, "group", TestContext.Current.CancellationToken));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "control-command-constructors-reject-invalid-identities")]
    public void ControlCommandConstructors_RejectMissingScheduleIdentifiers(string? missingValue)
    {
        var timestamp = new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Assert.ThrowsAny<ArgumentException>(() =>
            new CancelScheduledRecurringMessageCommand(missingValue!, "group", timestamp));
        Assert.ThrowsAny<ArgumentException>(() =>
            new PauseScheduledRecurringMessageCommand("schedule", missingValue!, timestamp));
        Assert.ThrowsAny<ArgumentException>(() =>
            new ResumeScheduledRecurringMessageCommand(missingValue!, "group", timestamp));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "control-handle-extensions-validate-receiver-first")]
    public async Task ControlHandleExtensions_RejectANullEndpointBeforeInspectingTheHandleAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("endpoint", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            PublishEndpointRecurringSchedulerExtensions.CancelScheduledRecurringSendAsync<ScheduledMessage>(null!, null!, cancellationToken))).ParamName);
        Assert.Equal("endpoint", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            PublishEndpointRecurringSchedulerExtensions.PauseScheduledRecurringSendAsync<ScheduledMessage>(null!, null!, cancellationToken))).ParamName);
        Assert.Equal("endpoint", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            PublishEndpointRecurringSchedulerExtensions.ResumeScheduledRecurringSendAsync<ScheduledMessage>(null!, null!, cancellationToken))).ParamName);
        Assert.Equal("endpoint", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SendEndpointRecurringSchedulerExtensions.CancelScheduledRecurringSendAsync<ScheduledMessage>(null!, null!, cancellationToken))).ParamName);
        Assert.Equal("endpoint", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SendEndpointRecurringSchedulerExtensions.PauseScheduledRecurringSendAsync<ScheduledMessage>(null!, null!, cancellationToken))).ParamName);
        Assert.Equal("endpoint", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SendEndpointRecurringSchedulerExtensions.ResumeScheduledRecurringSendAsync<ScheduledMessage>(null!, null!, cancellationToken))).ParamName);
    }

    private sealed record ScheduledMessage;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid boundary invoked {targetMethod?.Name}.");
    }
}
