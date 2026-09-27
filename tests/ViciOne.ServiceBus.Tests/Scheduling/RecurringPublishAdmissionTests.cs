using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class RecurringPublishAdmissionTests
{
    private static readonly Uri SchedulerAddress = new("loopback://localhost/admission-scheduler");
    private static readonly Uri PublishAddress = new("loopback://localhost/admission-published");
    private static readonly Uri SendAddress = new("loopback://localhost/admission-explicit");
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    public static IEnumerable<object[]> Cases()
    {
        foreach (bool publishCommand in new[] { false, true })
            foreach (bool missingTopology in new[] { false, true })
                foreach (Form form in Enum.GetValues<Form>())
                    yield return [publishCommand, missingTopology, form];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "publish-admission-rejects-before-effects-and-recovers")]
    public async Task UnavailablePublishTarget_RejectsBeforeCommandAndAllowsRecoveryAsync(
        bool publishCommand, bool missingTopology, Form form)
    {
        using var caller = new CancellationTokenSource();
        var endpoint = DispatchProxy.Create<CombinedEndpoint, EndpointBoundary>();
        var delivery = (EndpointBoundary)(object)endpoint;
        delivery.PublishCommand = publishCommand;
        var provider = DispatchProxy.Create<ISendEndpointProvider, ProviderBoundary>();
        var resolution = (ProviderBoundary)(object)provider;
        resolution.Endpoint = endpoint;
        var topology = DispatchProxy.Create<IBusTopology, TopologyBoundary>();
        var addresses = (TopologyBoundary)(object)topology;
        IRecurringMessageScheduler scheduler = publishCommand
            ? new PublishRecurringMessageScheduler(endpoint, missingTopology ? null : topology)
            : new EndpointRecurringMessageScheduler(provider, SchedulerAddress, missingTopology ? null : topology);
        var schedule = new Schedule();
        var message = new RecurringCompletionPayload { Value = "admitted-53", Count = 53 };
        var values = new AdmissionValues();
        int pipeCalls = 0;
        IPipe<SendContext<RecurringCompletionPayload>> typed = Pipe.Execute<SendContext<RecurringCompletionPayload>>(context =>
        {
            pipeCalls++;
            Assert.Equal("admitted-53", context.Message.Value);
            Assert.Equal(53, context.Message.Count);
            context.Headers.Set("admission", "accepted");
        });
        IPipe<SendContext> untyped = Pipe.Execute<SendContext>(context =>
        {
            pipeCalls++;
            context.Headers.Set("admission", "accepted");
        });

        async Task<ScheduledRecurringMessage> InvokeAsync(bool publish)
        {
            if (publish)
                return form switch
                {
                    Form.Generic => await scheduler.ScheduleRecurringPublishAsync(schedule, message, typed, caller.Token),
                    Form.Runtime => await scheduler.ScheduleRecurringPublishAsync(schedule, (object)message, untyped, caller.Token),
                    Form.Declared => await scheduler.ScheduleRecurringPublishAsync(schedule, message, typeof(RecurringCompletionContract), untyped, caller.Token),
                    Form.Initialized => await scheduler.ScheduleRecurringPublishAsync<RecurringCompletionPayload>(schedule, values, typed, caller.Token),
                    _ => throw new ArgumentOutOfRangeException(nameof(form))
                };

            return form switch
            {
                Form.Generic => await scheduler.ScheduleRecurringSendAsync(SendAddress, schedule, message, typed, caller.Token),
                Form.Runtime => await scheduler.ScheduleRecurringSendAsync(SendAddress, schedule, (object)message, untyped, caller.Token),
                Form.Declared => await scheduler.ScheduleRecurringSendAsync(SendAddress, schedule, message, typeof(RecurringCompletionContract), untyped, caller.Token),
                Form.Initialized => await scheduler.ScheduleRecurringSendAsync<RecurringCompletionPayload>(SendAddress, schedule, values, typed, caller.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(form))
            };
        }

        if (missingTopology)
            await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(true).WaitAsync(Timeout, TestContext.Current.CancellationToken));
        else
            await Assert.ThrowsAsync<ArgumentException>(() => InvokeAsync(true).WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Type contract = form == Form.Declared ? typeof(RecurringCompletionContract) : typeof(RecurringCompletionPayload);
        Assert.Equal(missingTopology ? Array.Empty<Type>() : [contract], addresses.Lookups);
        Assert.Empty(resolution.Lookups);
        Assert.Empty(delivery.Commands);
        Assert.Equal(0, values.ValueReads);
        Assert.Equal(0, values.CountReads);
        Assert.Equal(0, pipeCalls);

        addresses.Available = true;
        ScheduledRecurringMessage handle = await InvokeAsync(!missingTopology).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.Equal(missingTopology ? Array.Empty<Type>() : [contract, contract], addresses.Lookups);
        if (publishCommand)
            Assert.Empty(resolution.Lookups);
        else
            Assert.Equal((SchedulerAddress, caller.Token), Assert.Single(resolution.Lookups));
        MessageSendContext<ScheduleRecurringMessage> context = Assert.Single(delivery.Commands);
        ScheduleRecurringMessage command = context.Message;
        Assert.Equal(caller.Token, context.CancellationToken);
        Assert.Same(schedule, command.Schedule);
        Assert.Same(schedule, handle.Schedule);
        Assert.Equal(missingTopology ? SendAddress : PublishAddress, command.Destination);
        Assert.Equal(command.Destination, handle.Destination);
        Assert.Equal(typeof(ScheduleRecurringMessageCommand<>).MakeGenericType(contract), command.GetType());
        string[] expectedTypes = form == Form.Declared
            ? ["urn:message:ViciOne.ServiceBus.Tests.Scheduling:RecurringCompletionContract"]
            : ["urn:message:ViciOne.ServiceBus.Tests.Scheduling:RecurringCompletionPayload",
                "urn:message:ViciOne.ServiceBus.Tests.Scheduling:RecurringCompletionContract"];
        Assert.Equal(expectedTypes.Order(), command.PayloadType.Order());
        var payload = Assert.IsType<RecurringCompletionPayload>(command.Payload);
        Assert.Equal("admitted-53", payload.Value);
        Assert.Equal(53, payload.Count);
        Assert.Same(payload, Assert.IsAssignableFrom<ScheduledRecurringMessage<RecurringCompletionContract>>(handle).Payload);
        if (form == Form.Initialized)
        {
            Assert.True(values.ValueReads > 0);
            Assert.True(values.CountReads > 0);
        }
        else
        {
            Assert.Same(message, payload);
            Assert.Equal(0, values.ValueReads);
            Assert.Equal(0, values.CountReads);
        }
        Assert.Equal(1, pipeCalls);
        Assert.Equal("accepted", context.Headers.Get<string>("admission"));
    }

    public enum Form { Generic, Runtime, Declared, Initialized }

    public sealed class AdmissionValues
    {
        public int ValueReads { get; private set; }
        public int CountReads { get; private set; }
        public string Value { get { ValueReads++; return "admitted-53"; } }
        public int Count { get { CountReads++; return 53; } }
    }

    private sealed class Schedule : RecurringSchedule
    {
        public string TimeZoneId => "UTC";
        public DateTimeOffset StartTime => new(2045, 3, 4, 5, 6, 7, TimeSpan.Zero);
        public DateTimeOffset? EndTime => null;
        public string ScheduleId => "admission-53";
        public string ScheduleGroup => "admission-group";
        public string CronExpression => "0 0 6 ? * *";
        public string Description => "Admission recovery";
        public MissedEventPolicy MisfirePolicy => MissedEventPolicy.Skip;
    }

    private interface CombinedEndpoint : IAdvancedSendEndpoint, IPublishEndpoint, IAdvancedPublishEndpoint;

    private class ProviderBoundary : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;
        public List<(Uri Address, CancellationToken Token)> Lookups { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(ISendEndpointProvider.GetSendEndpointAsync), targetMethod?.Name);
            Assert.NotNull(args);
            Lookups.Add((Assert.IsType<Uri>(args[0]), Assert.IsType<CancellationToken>(args[^1])));
            return Task.FromResult(Endpoint);
        }
    }

    private class TopologyBoundary : DispatchProxy
    {
        public bool Available { get; set; }
        public List<Type> Lookups { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            Assert.Equal(nameof(IBusTopology.TryGetPublishAddress), targetMethod.Name);
            Assert.NotNull(args);
            Lookups.Add(targetMethod.IsGenericMethod ? Assert.Single(targetMethod.GetGenericArguments()) : Assert.IsAssignableFrom<Type>(args[0]));
            args[^1] = Available ? PublishAddress : null;
            return Available;
        }
    }

    private class EndpointBoundary : DispatchProxy
    {
        public bool PublishCommand { get; set; }
        public List<MessageSendContext<ScheduleRecurringMessage>> Commands { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            Assert.Equal(PublishCommand ? nameof(IPublishEndpoint.PublishAsync) : nameof(ISendEndpoint.SendAsync), targetMethod.Name);
            Assert.Equal(typeof(ScheduleRecurringMessage), Assert.Single(targetMethod.GetGenericArguments()));
            Assert.NotNull(args);
            return SendAsync(args);
        }

        private async Task SendAsync(object?[] args)
        {
            var command = Assert.IsAssignableFrom<ScheduleRecurringMessage>(args[0]);
            var context = new MessageSendContext<ScheduleRecurringMessage>(command, Assert.IsType<CancellationToken>(args[^1]));
            Commands.Add(context);
            Assert.Equal(3, args.Length);
            if (args[1] is IPipe<PublishContext<ScheduleRecurringMessage>> publishPipe)
                await publishPipe.SendAsync(context);
            else if (args[1] is IPipe<SendContext<ScheduleRecurringMessage>> sendPipe)
                await sendPipe.SendAsync(context);
            else
                await Assert.IsAssignableFrom<IPipe<SendContext>>(args[1]).SendAsync(context);
        }
    }
}
