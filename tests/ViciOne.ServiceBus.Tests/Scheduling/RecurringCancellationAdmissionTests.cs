using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class RecurringCancellationAdmissionTests
{
    private static readonly Uri SchedulerAddress = new("loopback://localhost/cancel-admission-scheduler");
    private static readonly Uri SendAddress = new("loopback://localhost/cancel-admission-send");
    private static readonly Uri PublishAddress = new("loopback://localhost/cancel-admission-publish");
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    public static IEnumerable<object[]> ScheduleCases()
    {
        foreach (bool publishCommand in new[] { false, true })
            foreach (bool publishPayload in new[] { false, true })
                foreach (int pipeShape in Enumerable.Range(0, 3))
                    yield return [publishCommand, publishPayload, pipeShape];
    }

    public static IEnumerable<object[]> ControlCases()
    {
        foreach (bool publishCommand in new[] { false, true })
            foreach (Control control in Enum.GetValues<Control>())
                yield return [publishCommand, control];
    }

    public static IEnumerable<object[]> DelayedResolutionCases()
    {
        foreach (int pipeShape in Enumerable.Range(0, 3))
            yield return [false, pipeShape];
        foreach (Control control in Enum.GetValues<Control>())
            yield return [true, (int)control];
    }

    [Theory]
    [MemberData(nameof(ScheduleCases))]
    [RequirementCoverage("REQ-VSB-RECURRING-CANCELLATION-ADMISSION", "pre-canceled-schedule-never-reaches-command-transport")]
    public async Task PreCanceledSchedule_SuppressesTransportAndAllowsHealthySuccessorAsync(
        bool publishCommand, bool publishPayload, int pipeShape)
    {
        var endpoint = DispatchProxy.Create<CombinedEndpoint, EndpointBoundary>();
        var recorded = (EndpointBoundary)(object)endpoint;
        var topology = DispatchProxy.Create<IBusTopology, TopologyBoundary>();
        var addresses = (TopologyBoundary)(object)topology;
        addresses.ThrowOnLookup = true;
        var provider = DispatchProxy.Create<ISendEndpointProvider, ProviderBoundary>();
        var resolution = (ProviderBoundary)(object)provider;
        resolution.Resolve = _ => Task.FromResult<ISendEndpoint>(endpoint);
        IRecurringMessageScheduler scheduler = publishCommand
            ? new PublishRecurringMessageScheduler(endpoint, topology)
            : new EndpointRecurringMessageScheduler(provider, SchedulerAddress, topology);
        var schedule = new Schedule();
        var message = new RecurringCompletionPayload { Value = "admission-71", Count = 71 };
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ScheduleAsync(scheduler, publishPayload, pipeShape, schedule, message, canceled.Token));

        Assert.Equal(canceled.Token, failure.CancellationToken);
        Assert.Empty(recorded.Commands);
        Assert.Equal(0, addresses.Calls);
        Assert.Equal(0, resolution.Calls);

        addresses.ThrowOnLookup = false;
        using var healthy = new CancellationTokenSource();
        ScheduledRecurringMessage handle = await ScheduleAsync(scheduler, publishPayload, pipeShape, schedule, message, healthy.Token)
            .WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);

        Command command = Assert.Single(recorded.Commands);
        Assert.Equal(healthy.Token, command.Token);
        Assert.Equal(typeof(ScheduleRecurringMessage), command.Contract);
        Assert.Equal(typeof(ScheduleRecurringMessageCommand<RecurringCompletionPayload>), command.Message.GetType());
        var scheduled = Assert.IsAssignableFrom<ScheduleRecurringMessage>(command.Message);
        Assert.Same(schedule, scheduled.Schedule);
        Assert.Same(message, Assert.IsType<RecurringCompletionPayload>(scheduled.Payload));
        Assert.Equal(publishPayload ? PublishAddress : SendAddress, scheduled.Destination);
        Assert.Equal(scheduled.Destination, handle.Destination);
        Assert.Same(schedule, handle.Schedule);
        Assert.Equal(publishPayload ? 1 : 0, addresses.Calls);
        Assert.Equal(publishCommand ? 0 : 1, resolution.Calls);
    }

    [Theory]
    [MemberData(nameof(ControlCases))]
    [RequirementCoverage("REQ-VSB-RECURRING-CANCELLATION-ADMISSION", "pre-canceled-control-skips-resolution-and-transport")]
    public async Task PreCanceledControl_SkipsEndpointResolutionAndAllowsHealthySuccessorAsync(bool publishCommand, Control control)
    {
        var endpoint = DispatchProxy.Create<CombinedEndpoint, EndpointBoundary>();
        var recorded = (EndpointBoundary)(object)endpoint;
        var provider = DispatchProxy.Create<ISendEndpointProvider, ProviderBoundary>();
        var resolution = (ProviderBoundary)(object)provider;
        resolution.Resolve = _ => Task.FromResult<ISendEndpoint>(endpoint);
        IRecurringMessageScheduler scheduler = publishCommand
            ? new PublishRecurringMessageScheduler(endpoint)
            : new EndpointRecurringMessageScheduler(provider, SchedulerAddress);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ControlAsync(scheduler, control, "schedule-71", "group-72", canceled.Token));

        Assert.Equal(canceled.Token, failure.CancellationToken);
        Assert.Equal(0, resolution.Calls);
        Assert.Empty(recorded.Commands);

        using var healthy = new CancellationTokenSource();
        await ControlAsync(scheduler, control, "successor-73", "group-74", healthy.Token)
            .WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(publishCommand ? 0 : 1, resolution.Calls);
        Command command = Assert.Single(recorded.Commands);
        Assert.Equal(healthy.Token, command.Token);
        Assert.Equal(ControlContract(control), command.Contract);
        AssertControlIdentity(command.Message, control, "successor-73", "group-74");
    }

    [Theory]
    [MemberData(nameof(DelayedResolutionCases))]
    [RequirementCoverage("REQ-VSB-RECURRING-CANCELLATION-ADMISSION", "cancellation-during-endpoint-resolution-suppresses-late-command")]
    public async Task CancellationDuringEndpointResolution_SuppressesLateCommandAndAllowsRecoveryAsync(bool control, int form)
    {
        var endpoint = DispatchProxy.Create<CombinedEndpoint, EndpointBoundary>();
        var recorded = (EndpointBoundary)(object)endpoint;
        var pendingResolution = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = DispatchProxy.Create<ISendEndpointProvider, ProviderBoundary>();
        var resolution = (ProviderBoundary)(object)provider;
        resolution.Resolve = _ => pendingResolution.Task;
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(provider, SchedulerAddress);
        var schedule = new Schedule();
        var message = new RecurringCompletionPayload { Value = "admission-71", Count = 71 };
        using var canceled = new CancellationTokenSource();

        Task operation = control
            ? ControlAsync(scheduler, (Control)form, "schedule-71", "group-72", canceled.Token)
            : ScheduleAsync(scheduler, false, form, schedule, message, canceled.Token);
        Assert.Equal(1, resolution.Calls);
        Assert.Equal(canceled.Token, resolution.LastToken);
        Assert.False(operation.IsCompleted);
        Assert.Empty(recorded.Commands);

        canceled.Cancel();
        pendingResolution.SetResult(endpoint);
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operation.WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken));

        Assert.Equal(canceled.Token, failure.CancellationToken);
        Assert.Empty(recorded.Commands);

        resolution.Resolve = _ => Task.FromResult<ISendEndpoint>(endpoint);
        using var healthy = new CancellationTokenSource();
        if (control)
            await ControlAsync(scheduler, (Control)form, "successor-73", "group-74", healthy.Token)
                .WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);
        else
            await ScheduleAsync(scheduler, false, form, schedule, message, healthy.Token)
                .WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(2, resolution.Calls);
        Command command = Assert.Single(recorded.Commands);
        Assert.Equal(healthy.Token, command.Token);
        if (control)
        {
            Assert.Equal(ControlContract((Control)form), command.Contract);
            AssertControlIdentity(command.Message, (Control)form, "successor-73", "group-74");
        }
        else
        {
            Assert.Equal(typeof(ScheduleRecurringMessage), command.Contract);
            Assert.Equal(typeof(ScheduleRecurringMessageCommand<RecurringCompletionPayload>), command.Message.GetType());
            var scheduled = Assert.IsAssignableFrom<ScheduleRecurringMessage>(command.Message);
            Assert.Equal(SendAddress, scheduled.Destination);
            Assert.Same(message, scheduled.Payload);
        }
    }

    private static Task<ScheduledRecurringMessage<RecurringCompletionPayload>> ScheduleAsync(
        IRecurringMessageScheduler scheduler, bool publishPayload, int pipeShape, Schedule schedule,
        RecurringCompletionPayload message, CancellationToken token)
    {
        if (publishPayload)
            return pipeShape switch
            {
                0 => scheduler.ScheduleRecurringPublishAsync(schedule, message, token),
                1 => scheduler.ScheduleRecurringPublishAsync(schedule, message,
                    Pipe.Empty<SendContext<RecurringCompletionPayload>>(), token),
                2 => scheduler.ScheduleRecurringPublishAsync(schedule, message, Pipe.Empty<SendContext>(), token),
                _ => throw new ArgumentOutOfRangeException(nameof(pipeShape))
            };

        return pipeShape switch
        {
            0 => scheduler.ScheduleRecurringSendAsync(SendAddress, schedule, message, token),
            1 => scheduler.ScheduleRecurringSendAsync(SendAddress, schedule, message,
                Pipe.Empty<SendContext<RecurringCompletionPayload>>(), token),
            2 => scheduler.ScheduleRecurringSendAsync(SendAddress, schedule, message, Pipe.Empty<SendContext>(), token),
            _ => throw new ArgumentOutOfRangeException(nameof(pipeShape))
        };
    }

    private static Task ControlAsync(IRecurringMessageScheduler scheduler, Control control, string id, string group,
        CancellationToken token) => control switch
        {
            Control.Cancel => scheduler.CancelScheduledRecurringSendAsync(id, group, token),
            Control.Pause => scheduler.PauseScheduledRecurringSendAsync(id, group, token),
            Control.Resume => scheduler.ResumeScheduledRecurringSendAsync(id, group, token),
            _ => throw new ArgumentOutOfRangeException(nameof(control))
        };

    private static Type ControlContract(Control control) => control switch
    {
        Control.Cancel => typeof(CancelScheduledRecurringMessage),
        Control.Pause => typeof(PauseScheduledRecurringMessage),
        Control.Resume => typeof(ResumeScheduledRecurringMessage),
        _ => throw new ArgumentOutOfRangeException(nameof(control))
    };

    private static void AssertControlIdentity(object value, Control control, string id, string group)
    {
        (string ScheduleId, string ScheduleGroup) actual = control switch
        {
            Control.Cancel => (Assert.IsAssignableFrom<CancelScheduledRecurringMessage>(value).ScheduleId,
                ((CancelScheduledRecurringMessage)value).ScheduleGroup),
            Control.Pause => (Assert.IsAssignableFrom<PauseScheduledRecurringMessage>(value).ScheduleId,
                ((PauseScheduledRecurringMessage)value).ScheduleGroup),
            Control.Resume => (Assert.IsAssignableFrom<ResumeScheduledRecurringMessage>(value).ScheduleId,
                ((ResumeScheduledRecurringMessage)value).ScheduleGroup),
            _ => throw new ArgumentOutOfRangeException(nameof(control))
        };
        Assert.Equal((id, group), actual);
    }

    public enum Control { Cancel, Pause, Resume }

    private sealed class Schedule : RecurringSchedule
    {
        public string TimeZoneId => "UTC";
        public DateTimeOffset StartTime => new(2045, 3, 4, 5, 6, 7, TimeSpan.Zero);
        public DateTimeOffset? EndTime => null;
        public string ScheduleId => "admission-71";
        public string ScheduleGroup => "group-72";
        public string CronExpression => "0 0 6 ? * *";
        public string Description => "Cancellation admission";
        public MissedEventPolicy MisfirePolicy => MissedEventPolicy.Skip;
    }

    private interface CombinedEndpoint : IAdvancedSendEndpoint, IPublishEndpoint, IAdvancedPublishEndpoint;

    private sealed record Command(Type Contract, object Message, CancellationToken Token);

    private class EndpointBoundary : DispatchProxy
    {
        public List<Command> Commands { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            Assert.NotNull(args);
            Assert.Contains(targetMethod.Name, new[] { nameof(ISendEndpoint.SendAsync), nameof(IPublishEndpoint.PublishAsync) });
            Commands.Add(new Command(Assert.Single(targetMethod.GetGenericArguments()), args[0]!, Assert.IsType<CancellationToken>(args[^1])));
            return Task.CompletedTask;
        }
    }

    private class ProviderBoundary : DispatchProxy
    {
        public Func<CancellationToken, Task<ISendEndpoint>> Resolve { get; set; } = null!;
        public int Calls { get; private set; }
        public CancellationToken LastToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(ISendEndpointProvider.GetSendEndpointAsync), targetMethod?.Name);
            Assert.NotNull(args);
            Assert.Equal(SchedulerAddress, args[0]);
            LastToken = Assert.IsType<CancellationToken>(args[^1]);
            Calls++;
            return Resolve(LastToken);
        }
    }

    private class TopologyBoundary : DispatchProxy
    {
        public bool ThrowOnLookup { get; set; }
        public int Calls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(IBusTopology.TryGetPublishAddress), targetMethod?.Name);
            Assert.NotNull(args);
            Calls++;
            if (ThrowOnLookup)
                throw new InvalidOperationException("A canceled schedule reached topology resolution.");
            args[^1] = PublishAddress;
            return true;
        }
    }
}
