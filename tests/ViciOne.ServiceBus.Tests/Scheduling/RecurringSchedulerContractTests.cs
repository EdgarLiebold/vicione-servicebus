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
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "endpoint-factories-reject-null")]
    public void EndpointFactories_RejectNullEndpoints()
    {
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            RecurringMessageSchedulerExtensions.CreateRecurringMessageScheduler((ISendEndpoint)null!)).ParamName);
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            RecurringMessageSchedulerExtensions.CreateRecurringMessageScheduler((IPublishEndpoint)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "endpoint-factories-preserve-kind-clock-and-topology")]
    public async Task EndpointFactories_PreserveSchedulerKindClockAndTopologyAsync()
    {
        ISendEndpoint sendEndpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        IPublishEndpoint publishEndpoint = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        IBusTopology busTopology = DispatchProxy.Create<IBusTopology, MissingPublishAddressTopologyProxy>();
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var timeProvider = new TestTimeProvider();

        var sendScheduler = Assert.IsType<EndpointRecurringMessageScheduler>(
            sendEndpoint.CreateRecurringMessageScheduler(busTopology, timeProvider));
        var publishScheduler = Assert.IsType<PublishRecurringMessageScheduler>(
            publishEndpoint.CreateRecurringMessageScheduler(busTopology, timeProvider));

        Assert.Same(timeProvider, sendScheduler.TimeProvider);
        Assert.Same(timeProvider, publishScheduler.TimeProvider);

        Assert.Contains("publish address", (await Assert.ThrowsAsync<ArgumentException>(() =>
            sendScheduler.ScheduleRecurringPublishAsync(schedule, new ScheduledMessage(), CancellationToken.None))).Message);
        Assert.Contains("publish address", (await Assert.ThrowsAsync<ArgumentException>(() =>
            publishScheduler.ScheduleRecurringPublishAsync(schedule, new ScheduledMessage(), CancellationToken.None))).Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "every-scheduling-overload-validates-schedule-first")]
    public async Task EverySchedulingOverload_RejectsANullScheduleBeforeOtherInputsAsync()
    {
        ISendEndpoint sendEndpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        IPublishEndpoint publishEndpoint = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        object[] schedulers =
        [
            new EndpointRecurringMessageScheduler(sendEndpoint),
            new PublishRecurringMessageScheduler(publishEndpoint)
        ];
        var inspectedOverloads = 0;

        foreach (object scheduler in schedulers)
        {
            MethodInfo[] methods = scheduler.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.Name.StartsWith("ScheduleRecurring", StringComparison.Ordinal)
                    && method.GetParameters().Any(parameter => parameter.Name == "schedule"))
                .ToArray();

            Assert.NotEmpty(methods);

            foreach (MethodInfo candidate in methods)
            {
                MethodInfo method = candidate.IsGenericMethodDefinition
                    ? candidate.MakeGenericMethod(typeof(ScheduledMessage))
                    : candidate;
                object?[] arguments = method.GetParameters()
                    .Select<ParameterInfo, object?>(parameter => parameter.ParameterType == typeof(Uri)
                        ? DestinationAddress
                        : parameter.ParameterType == typeof(CancellationToken)
                            ? CancellationToken.None
                            : null)
                    .ToArray();

                Exception exception = await CaptureInvocationExceptionAsync(method, scheduler, arguments);

                Assert.Equal("schedule", Assert.IsType<ArgumentNullException>(exception).ParamName);
                inspectedOverloads++;
            }
        }

        Assert.Equal(40, inspectedOverloads);
    }

    private static async Task<Exception> CaptureInvocationExceptionAsync(MethodInfo method, object target, object?[] arguments)
    {
        try
        {
            object? result = method.Invoke(target, arguments);
            if (result is Task task)
                await task;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return exception.InnerException;
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"{method.Name} accepted a null recurring schedule.");
    }

    private sealed record ScheduledMessage;

    private sealed class TestTimeProvider : TimeProvider
    {
    }

    private class MissingPublishAddressTopologyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IBusTopology.TryGetPublishAddress))
                return false;

            throw new InvalidOperationException($"The topology boundary invoked {targetMethod?.Name}.");
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid boundary invoked {targetMethod?.Name}.");
    }
}
