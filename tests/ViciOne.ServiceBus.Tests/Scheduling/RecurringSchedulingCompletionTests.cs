using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class RecurringSchedulingCompletionTests
{
    private static readonly Uri ExplicitDestination = new("loopback://localhost/recurring-explicit");
    private static readonly Uri ConcreteDestination = new("loopback://localhost/recurring-concrete");
    private static readonly Uri ContractDestination = new("loopback://localhost/recurring-contract");
    private static readonly Guid Correlation = new("57110000-0000-0000-0000-000000000037");

    public static IEnumerable<object[]> Cases()
    {
        foreach (bool publishCommand in new[] { false, true })
            foreach (bool publishPayload in new[] { false, true })
                foreach (int form in Enumerable.Range(0, 11))
                    foreach (int outcome in Enumerable.Range(0, 3))
                        yield return [publishCommand, publishPayload, form, outcome];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "command-contract-and-pipes-await-endpoint-completion")]
    public async Task Scheduling_PreservesCommandAndWaitsForEndpointCompletionAsync(
        bool publishCommand, bool publishPayload, int form, int outcome)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var provider = new CancellationTokenSource();
        var endpoint = DispatchProxy.Create<CombinedEndpoint, EndpointBoundary>();
        var boundary = (EndpointBoundary)(object)endpoint;
        boundary.PublishCommand = publishCommand;
        var topology = DispatchProxy.Create<IBusTopology, TopologyBoundary>();
        var topologyBoundary = (TopologyBoundary)(object)topology;
        IRecurringMessageScheduler scheduler = publishCommand
            ? new PublishRecurringMessageScheduler(endpoint, topology)
            : new EndpointRecurringMessageScheduler(endpoint, topology);
        var schedule = new Schedule();
        var message = new RecurringCompletionPayload { Value = "Grüße-37", Count = 37 };
        object values = new { Value = "Grüße-37", Count = 37, __Header_Region_Code = "north" };
        var typedCalls = 0;
        var untypedCalls = 0;
        IPipe<SendContext<RecurringCompletionPayload>> typed = Pipe.Execute<SendContext<RecurringCompletionPayload>>(context =>
        {
            typedCalls++;
            Assert.Equal("Grüße-37", context.Message.Value);
            Assert.Equal(37, context.Message.Count);
            if (form < 7)
                Assert.Same(message, context.Message);
            context.CorrelationId = Correlation;
            context.Headers.Set("completion-pipe", "typed");
        });
        IPipe<SendContext> untyped = Pipe.Execute<SendContext>(context =>
        {
            untypedCalls++;
            context.CorrelationId = Correlation;
            context.Headers.Set("completion-pipe", "untyped");
        });
        Task<ScheduledRecurringMessage> pending = InvokeAsync(scheduler, publishPayload, form, schedule, message,
            values, typed, untyped, caller.Token);
        try
        {
            Task first = await Task.WhenAny(boundary.Entered.Task, pending)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (first == pending)
                await pending;
            MessageSendContext<ScheduleRecurringMessage> context = await boundary.Entered.Task
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            Assert.Equal(1, boundary.Calls);
            Assert.Equal(caller.Token, context.CancellationToken);
            Assert.True(context.CancellationToken.CanBeCanceled);
            ScheduleRecurringMessage command = context.Message;
            Assert.Same(schedule, command.Schedule);
            Assert.Equal("Europe/Berlin", command.Schedule.TimeZoneId);
            Assert.Equal(new DateTimeOffset(2039, 4, 5, 6, 7, 8, TimeSpan.Zero), command.Schedule.StartTime);
            Assert.Equal(new DateTimeOffset(2040, 5, 6, 7, 8, 9, TimeSpan.Zero), command.Schedule.EndTime);
            Assert.Equal("schedule-37", command.Schedule.ScheduleId);
            Assert.Equal("group-59", command.Schedule.ScheduleGroup);
            Assert.Equal("0 7 6 ? * MON-FRI", command.Schedule.CronExpression);
            Assert.Equal("Recurring Grüße", command.Schedule.Description);
            Assert.Equal(MissedEventPolicy.Skip, command.Schedule.MisfirePolicy);
            bool declared = form is 4 or 6;
            Type expectedContract = declared ? typeof(RecurringCompletionContract) : typeof(RecurringCompletionPayload);
            Uri expectedDestination = !publishPayload ? ExplicitDestination : declared ? ContractDestination : ConcreteDestination;
            Assert.Equal(expectedDestination, command.Destination);
            Assert.Equal(typeof(ScheduleRecurringMessageCommand<>).MakeGenericType(expectedContract), command.GetType());
            string[] expectedTypes = declared
                ? ["urn:message:ViciOne.ServiceBus.Tests.Scheduling:RecurringCompletionContract"]
                : ["urn:message:ViciOne.ServiceBus.Tests.Scheduling:RecurringCompletionPayload",
                    "urn:message:ViciOne.ServiceBus.Tests.Scheduling:RecurringCompletionContract"];
            Assert.Equal(expectedTypes.Order(), command.PayloadType.Order());
            if (publishPayload)
                Assert.Equal(expectedContract, Assert.Single(topologyBoundary.Lookups));
            else
                Assert.Empty(topologyBoundary.Lookups);
            var payload = Assert.IsType<RecurringCompletionPayload>(command.Payload);
            Assert.Equal("Grüße-37", payload.Value);
            Assert.Equal(37, payload.Count);
            if (form is < 7 or 10)
                Assert.Same(message, payload);
            Assert.Equal(form is >= 7 and <= 9 ? "north" : null, context.Headers.Get<string>("Region-Code"));
            bool typedExpected = form is 1 or 8;
            bool untypedExpected = form is 2 or 5 or 6 or 9;
            Assert.Equal(typedExpected ? 1 : 0, typedCalls);
            Assert.Equal(untypedExpected ? 1 : 0, untypedCalls);
            Assert.Equal(typedExpected ? "typed" : untypedExpected ? "untyped" : null,
                context.Headers.Get<string>("completion-pipe"));
            Assert.Equal(typedExpected || untypedExpected ? Correlation : (Guid?)null, context.CorrelationId);

            var failure = new InvalidOperationException("The recurring command endpoint rejected delivery.");
            if (outcome == 0)
            {
                boundary.Completion.SetResult();
                ScheduledRecurringMessage handle = await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Same(schedule, handle.Schedule);
                Assert.Equal(expectedDestination, handle.Destination);
                Assert.Same(payload, Assert.IsAssignableFrom<ScheduledRecurringMessage<RecurringCompletionContract>>(handle).Payload);
            }
            else if (outcome == 1)
            {
                boundary.Completion.SetException(failure);
                Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)));
            }
            else
            {
                provider.Cancel();
                boundary.Completion.SetCanceled(provider.Token);
                OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Equal(provider.Token, error.CancellationToken);
                Assert.False(caller.IsCancellationRequested);
                Assert.True(pending.IsCanceled);
            }
            Assert.Equal(1, boundary.Calls);
        }
        finally
        {
            boundary.Completion.TrySetResult();
        }
    }

    private static async Task<ScheduledRecurringMessage> InvokeAsync(IRecurringMessageScheduler scheduler, bool publish, int form,
        RecurringSchedule schedule, RecurringCompletionPayload message, object values,
        IPipe<SendContext<RecurringCompletionPayload>> typed, IPipe<SendContext> untyped, CancellationToken token)
    {
        if (publish)
            return form switch
            {
                0 => await scheduler.ScheduleRecurringPublishAsync(schedule, message, token),
                1 => await scheduler.ScheduleRecurringPublishAsync(schedule, message, typed, token),
                2 => await scheduler.ScheduleRecurringPublishAsync(schedule, message, untyped, token),
                3 => await scheduler.ScheduleRecurringPublishAsync(schedule, (object)message, token),
                4 => await scheduler.ScheduleRecurringPublishAsync(schedule, message, typeof(RecurringCompletionContract), token),
                5 => await scheduler.ScheduleRecurringPublishAsync(schedule, (object)message, untyped, token),
                6 => await scheduler.ScheduleRecurringPublishAsync(schedule, message, typeof(RecurringCompletionContract), untyped, token),
                7 => await scheduler.ScheduleRecurringPublishAsync<RecurringCompletionPayload>(schedule, values, token),
                8 => await scheduler.ScheduleRecurringPublishAsync<RecurringCompletionPayload>(schedule, values, typed, token),
                9 => await scheduler.ScheduleRecurringPublishAsync<RecurringCompletionPayload>(schedule, values, untyped, token),
                10 => await scheduler.ScheduleRecurringPublishAsync(schedule, message, Pipe.Empty<SendContext<RecurringCompletionPayload>>(), token),
                _ => throw new ArgumentOutOfRangeException(nameof(form))
            };

        return form switch
        {
            0 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, message, token),
            1 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, message, typed, token),
            2 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, message, untyped, token),
            3 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, (object)message, token),
            4 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, message, typeof(RecurringCompletionContract), token),
            5 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, (object)message, untyped, token),
            6 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, message, typeof(RecurringCompletionContract), untyped, token),
            7 => await scheduler.ScheduleRecurringSendAsync<RecurringCompletionPayload>(ExplicitDestination, schedule, values, token),
            8 => await scheduler.ScheduleRecurringSendAsync<RecurringCompletionPayload>(ExplicitDestination, schedule, values, typed, token),
            9 => await scheduler.ScheduleRecurringSendAsync<RecurringCompletionPayload>(ExplicitDestination, schedule, values, untyped, token),
            10 => await scheduler.ScheduleRecurringSendAsync(ExplicitDestination, schedule, message, Pipe.Empty<SendContext<RecurringCompletionPayload>>(), token),
            _ => throw new ArgumentOutOfRangeException(nameof(form))
        };
    }

    private sealed class Schedule : RecurringSchedule
    {
        public string TimeZoneId => "Europe/Berlin";
        public DateTimeOffset StartTime => new(2039, 4, 5, 6, 7, 8, TimeSpan.Zero);
        public DateTimeOffset? EndTime => new(2040, 5, 6, 7, 8, 9, TimeSpan.Zero);
        public string ScheduleId => "schedule-37";
        public string ScheduleGroup => "group-59";
        public string CronExpression => "0 7 6 ? * MON-FRI";
        public string Description => "Recurring Grüße";
        public MissedEventPolicy MisfirePolicy => MissedEventPolicy.Skip;
    }

    private interface CombinedEndpoint : IAdvancedSendEndpoint, IPublishEndpoint, IAdvancedPublishEndpoint;

    private class EndpointBoundary : DispatchProxy
    {
        public bool PublishCommand { get; set; }
        public int Calls { get; private set; }
        public TaskCompletionSource<MessageSendContext<ScheduleRecurringMessage>> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            Assert.Equal(PublishCommand ? nameof(IPublishEndpoint.PublishAsync) : nameof(ISendEndpoint.SendAsync), targetMethod.Name);
            Assert.Equal(typeof(ScheduleRecurringMessage), Assert.Single(targetMethod.GetGenericArguments()));
            Assert.NotNull(args);
            Calls++;
            return SendAsync(args);
        }

        private async Task SendAsync(object?[] args)
        {
            var command = Assert.IsAssignableFrom<ScheduleRecurringMessage>(args[0]);
            var context = new MessageSendContext<ScheduleRecurringMessage>(command, Assert.IsType<CancellationToken>(args[^1]));
            if (args.Length == 3)
            {
                if (args[1] is IPipe<PublishContext<ScheduleRecurringMessage>> publishPipe)
                    await publishPipe.SendAsync(context);
                else if (args[1] is IPipe<SendContext<ScheduleRecurringMessage>> sendPipe)
                    await sendPipe.SendAsync(context);
                else
                    await Assert.IsAssignableFrom<IPipe<SendContext>>(args[1]).SendAsync(context);
            }
            else
                Assert.Equal(2, args.Length);
            Entered.SetResult(context);
            await Completion.Task;
        }
    }

    private class TopologyBoundary : DispatchProxy
    {
        public List<Type> Lookups { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(IBusTopology.TryGetPublishAddress), targetMethod?.Name);
            Assert.NotNull(targetMethod);
            Assert.NotNull(args);
            Assert.Equal(targetMethod.IsGenericMethod ? 1 : 2, args.Length);
            Type contract = targetMethod.IsGenericMethod
                ? Assert.Single(targetMethod.GetGenericArguments())
                : Assert.IsAssignableFrom<Type>(args[0]);
            Lookups.Add(contract);
            args[^1] = contract == typeof(RecurringCompletionPayload) ? ConcreteDestination
                : contract == typeof(RecurringCompletionContract) ? ContractDestination
                : throw new InvalidOperationException($"Unexpected message contract: {contract}");
            return true;
        }
    }
}

public interface RecurringCompletionContract
{
    string Value { get; }
    int Count { get; }
}

public sealed class RecurringCompletionPayload : RecurringCompletionContract
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}
