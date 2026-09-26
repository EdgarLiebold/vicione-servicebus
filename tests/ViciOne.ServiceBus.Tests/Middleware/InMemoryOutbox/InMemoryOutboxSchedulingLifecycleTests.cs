using System.Reflection;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.InMemoryOutbox;

public sealed class InMemoryOutboxSchedulingLifecycleTests
{
    private static readonly Uri Destination = new("loopback://localhost/lifecycle-target");
    private static readonly Uri ControlDestination = new("loopback://localhost/lifecycle-control");
    private static readonly Uri PublishDestination = new("loopback://localhost/lifecycle-publish");
    private static readonly DateTimeOffset DueAt = new(2047, 3, 4, 5, 6, 7, TimeSpan.FromHours(2));
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static TheoryData<int, int, bool> SchedulingCases
    {
        get
        {
            var cases = new TheoryData<int, int, bool>();
            for (int route = 0; route < 3; route++)
                for (int form = 0; form < 10; form++)
                {
                    cases.Add(route, form, false);
                    cases.Add(route, form, true);
                }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(SchedulingCases))]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-SCHEDULER", "message-forms-preserve-schedule-and-checkpoint-lifecycle")]
    public async Task MessageForms_PreserveScheduleAndCheckpointLifecycleAsync(int route, int form, bool rollback)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var provider = new RecordingProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, PublishTopology>();
        var topologyCalls = (PublishTopology)(object)topology;
        var realScheduler = new MessageScheduler(provider, topology);
        ConsumeContext<Command> consumed = InMemoryOutboxTestContextFactory.Create(new Command { Value = "input" }, token, realScheduler);
        var outbox = new InMemoryOutboxConsumeContext<Command>(consumed);
        Assert.True(outbox.TryGetPayload(out MessageSchedulerContext? scheduler));
        Assert.NotNull(scheduler);
        Uri expectedDestination = route switch
        {
            0 => Destination,
            1 => consumed.Advanced().ReceiveContext.InputAddress,
            2 => PublishDestination,
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };
        ScheduledMessage<Command> control = await realScheduler.ScheduleSendAsync(
            ControlDestination, DueAt.AddDays(1), new Command { Value = "control", Count = 91 }, token);
        OutboxCheckpoint checkpoint = outbox.CreateCheckpoint();
        var message = new Command { Value = "Grüße-47", Count = 37 };
        object values = new { Value = "Grüße-47", Count = 37, __Header_Tenant_Code = "north" };
        IPipe<SendContext<Command>> typedPipe = Pipe.Execute<SendContext<Command>>(context => context.Headers.Set("lifecycle-pipe", "typed"));
        IPipe<SendContext> untypedPipe = Pipe.Execute<SendContext>(context => context.Headers.Set("lifecycle-pipe", "untyped"));

        ScheduledMessage scheduled = await ScheduleAsync(scheduler, route, form, message, values, typedPipe, untypedPipe,
            operationCancellation.Token).WaitAsync(Timeout, token);

        Assert.Equal(2, provider.Accepted.Count);
        Observation accepted = provider.Accepted[1];
        Type expectedContract = form is 4 or 6 ? typeof(ICommand) : typeof(Command);
        Assert.Equal(expectedContract, accepted.Contract);
        Assert.Equal(expectedDestination, accepted.Destination);
        Assert.Equal(DueAt, accepted.DueAt);
        Assert.Equal(operationCancellation.Token, accepted.CancellationToken);
        Assert.Equal(accepted.Token, scheduled.TokenId);
        Assert.Equal(expectedDestination, scheduled.Destination);
        Assert.Equal(DueAt, scheduled.DueAt);
        var actualMessage = Assert.IsAssignableFrom<ICommand>(accepted.Message);
        Assert.Equal("Grüße-47", actualMessage.Value);
        Assert.Equal(37, actualMessage.Count);
        if (form < 7)
            Assert.Same(message, accepted.Message);
        Assert.Equal(form >= 7 ? "north" : null, accepted.Context.Headers.Get<string>("Tenant-Code"));
        string? pipeMarker = form is 1 or 8 ? "typed" : form is 2 or 5 or 6 or 9 ? "untyped" : null;
        Assert.Equal(pipeMarker, accepted.Context.Headers.Get<string>("lifecycle-pipe"));
        Assert.Equal(route == 2 ? new[] { expectedContract } : Array.Empty<Type>(), topologyCalls.Contracts);
        Assert.Equal(new[] { control.TokenId, scheduled.TokenId }, provider.Active.Select(x => x.Token));

        await scheduler.CancelScheduledSendAsync(ControlDestination, control.TokenId, operationCancellation.Token);
        Assert.Empty(provider.Cancellations);
        Assert.False(outbox.ClearToSend.IsCompleted);

        if (rollback)
        {
            await outbox.DiscardPendingActionsAsync(checkpoint, token).WaitAsync(Timeout, token);
            Assert.Equal(new[] { (expectedDestination, scheduled.TokenId) }, provider.Cancellations.Select(x => (x.Destination, x.Token)));
            Assert.Equal(control.TokenId, Assert.Single(provider.Active).Token);
            Assert.False(outbox.ClearToSend.IsCompleted);
        }

        await outbox.ExecutePendingActionsAsync(false, token).WaitAsync(Timeout, token);
        await outbox.ExecutePendingActionsAsync(false, token).WaitAsync(Timeout, token);
        Assert.True(outbox.ClearToSend.IsCompletedSuccessfully);
        CancellationObservation cancellation = Assert.Single(provider.Cancellations);
        Assert.Equal(rollback ? expectedDestination : ControlDestination, cancellation.Destination);
        Assert.Equal(rollback ? scheduled.TokenId : control.TokenId, cancellation.Token);
        if (!rollback)
            Assert.Equal(operationCancellation.Token, cancellation.CancellationToken);
        Assert.Equal(rollback ? control.TokenId : scheduled.TokenId, Assert.Single(provider.Active).Token);

        await scheduler.CancelScheduledSendAsync(rollback ? ControlDestination : expectedDestination,
            rollback ? control.TokenId : scheduled.TokenId, operationCancellation.Token).WaitAsync(Timeout, token);
        Assert.Empty(provider.Active);
        Assert.Equal(2, provider.Cancellations.Count);
        Assert.Equal(operationCancellation.Token, provider.Cancellations[1].CancellationToken);
    }

    public static TheoryData<int, int, bool> FailureCases
    {
        get
        {
            var cases = new TheoryData<int, int, bool>();
            for (int route = 0; route < 3; route++)
                foreach (int form in new[] { 1, 6, 9 })
                {
                    cases.Add(route, form, false);
                    cases.Add(route, form, true);
                }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-SCHEDULER", "provider-failure-preserves-cause-without-phantom-cleanup")]
    public async Task ProviderFailure_PreservesCauseWithoutPhantomCleanupAsync(int route, int form, bool canceled)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var provider = new RecordingProvider();
        var realScheduler = new MessageScheduler(provider, DispatchProxy.Create<IBusTopology, PublishTopology>());
        ConsumeContext<Command> consumed = InMemoryOutboxTestContextFactory.Create(new Command(), token, realScheduler);
        var outbox = new InMemoryOutboxConsumeContext<Command>(consumed);
        Assert.True(outbox.TryGetPayload(out MessageSchedulerContext? scheduler));
        Assert.NotNull(scheduler);
        ScheduledMessage<Command> control = await realScheduler.ScheduleSendAsync(
            ControlDestination, DueAt.AddDays(1), new Command { Value = "control" }, token);
        OutboxCheckpoint checkpoint = outbox.CreateCheckpoint();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        Exception expected = canceled
            ? new OperationCanceledException("Provider admission canceled", providerCancellation.Token)
            : new InvalidOperationException("Provider admission rejected");
        provider.Failure = expected;
        var message = new Command { Value = "failed", Count = 23 };
        object values = new { Value = "failed", Count = 23 };
        IPipe<SendContext<Command>> typed = Pipe.Empty<SendContext<Command>>();
        IPipe<SendContext> untyped = Pipe.Empty<SendContext>();

        if (canceled)
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ScheduleAsync(scheduler, route, form, message, values, typed, untyped, token).WaitAsync(Timeout, token));
            Assert.Equal(providerCancellation.Token, actual.CancellationToken);
        }
        else
        {
            Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ScheduleAsync(scheduler, route, form, message, values, typed, untyped, token).WaitAsync(Timeout, token));
            Assert.Same(expected, actual);
        }
        Assert.Equal(control.TokenId, Assert.Single(provider.Accepted).Token);
        Assert.Equal(control.TokenId, Assert.Single(provider.Active).Token);
        await outbox.DiscardPendingActionsAsync(checkpoint, token).WaitAsync(Timeout, token);
        Assert.Empty(provider.Cancellations);

        provider.Failure = null;
        ScheduledMessage recovered = await ScheduleAsync(scheduler, route, form, message, values, typed, untyped, token)
            .WaitAsync(Timeout, token);
        Assert.Equal(2, provider.Accepted.Count);
        Assert.Equal(provider.Accepted[1].Token, recovered.TokenId);
        await outbox.DiscardPendingActionsAsync(checkpoint, token).WaitAsync(Timeout, token);
        CancellationObservation cleanup = Assert.Single(provider.Cancellations);
        Assert.Equal(recovered.TokenId, cleanup.Token);
        Assert.Equal(provider.Accepted[1].Destination, cleanup.Destination);
        Assert.Equal(control.TokenId, Assert.Single(provider.Active).Token);
    }

    private static async Task<ScheduledMessage> ScheduleAsync(MessageSchedulerContext scheduler, int route, int form,
        Command message, object values, IPipe<SendContext<Command>> typed, IPipe<SendContext> untyped, CancellationToken token)
    {
        return (route, form) switch
        {
            (0, 0) => await scheduler.ScheduleSendAsync(Destination, DueAt, message, token),
            (0, 1) => await scheduler.ScheduleSendAsync(Destination, DueAt, message, typed, token),
            (0, 2) => await scheduler.ScheduleSendAsync(Destination, DueAt, message, untyped, token),
            (0, 3) => await scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, token),
            (0, 4) => await scheduler.ScheduleSendAsync(Destination, DueAt, message, typeof(ICommand), token),
            (0, 5) => await scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, untyped, token),
            (0, 6) => await scheduler.ScheduleSendAsync(Destination, DueAt, message, typeof(ICommand), untyped, token),
            (0, 7) => await scheduler.ScheduleSendAsync<Command>(Destination, DueAt, values, token),
            (0, 8) => await scheduler.ScheduleSendAsync(Destination, DueAt, values, typed, token),
            (0, 9) => await scheduler.ScheduleSendAsync<Command>(Destination, DueAt, values, untyped, token),
            (1, 0) => await scheduler.ScheduleSendAsync(DueAt, message, token),
            (1, 1) => await scheduler.ScheduleSendAsync(DueAt, message, typed, token),
            (1, 2) => await scheduler.ScheduleSendAsync(DueAt, message, untyped, token),
            (1, 3) => await scheduler.ScheduleSendAsync(DueAt, (object)message, token),
            (1, 4) => await scheduler.ScheduleSendAsync(DueAt, message, typeof(ICommand), token),
            (1, 5) => await scheduler.ScheduleSendAsync(DueAt, (object)message, untyped, token),
            (1, 6) => await scheduler.ScheduleSendAsync(DueAt, message, typeof(ICommand), untyped, token),
            (1, 7) => await scheduler.ScheduleSendAsync<Command>(DueAt, values, token),
            (1, 8) => await scheduler.ScheduleSendAsync(DueAt, values, typed, token),
            (1, 9) => await scheduler.ScheduleSendAsync<Command>(DueAt, values, untyped, token),
            (2, 0) => await scheduler.SchedulePublishAsync(DueAt, message, token),
            (2, 1) => await scheduler.SchedulePublishAsync(DueAt, message, typed, token),
            (2, 2) => await scheduler.SchedulePublishAsync(DueAt, message, untyped, token),
            (2, 3) => await scheduler.SchedulePublishAsync(DueAt, (object)message, token),
            (2, 4) => await scheduler.SchedulePublishAsync(DueAt, message, typeof(ICommand), token),
            (2, 5) => await scheduler.SchedulePublishAsync(DueAt, (object)message, untyped, token),
            (2, 6) => await scheduler.SchedulePublishAsync(DueAt, message, typeof(ICommand), untyped, token),
            (2, 7) => await scheduler.SchedulePublishAsync<Command>(DueAt, values, token),
            (2, 8) => await scheduler.SchedulePublishAsync(DueAt, values, typed, token),
            (2, 9) => await scheduler.SchedulePublishAsync<Command>(DueAt, values, untyped, token),
            _ => throw new ArgumentOutOfRangeException(nameof(form)),
        };
    }

    public interface ICommand
    {
        string? Value { get; }
        int Count { get; }
    }

    public sealed class Command : ICommand
    {
        public string? Value { get; set; }
        public int Count { get; set; }
    }

    private sealed record Observation(Guid Token, Uri Destination, DateTimeOffset DueAt, Type Contract,
        object Message, SendContext Context, CancellationToken CancellationToken);

    private sealed record CancellationObservation(Uri Destination, Guid Token, CancellationToken CancellationToken);

    private sealed class RecordingProvider : IScheduleMessageProvider
    {
        public Exception? Failure { get; set; }
        public List<Observation> Accepted { get; } = [];
        public List<Observation> Active { get; } = [];
        public List<CancellationObservation> Cancellations { get; } = [];

        public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt,
            T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is { } failure)
                throw failure;
            var context = new InMemorySendContext<T>(message, cancellationToken);
            await pipe.SendAsync(context);
            var id = new Guid(Accepted.Count + 1, 0, 0, new byte[8]);
            var observation = new Observation(id, destinationAddress, dueAt, typeof(T), message, context, cancellationToken);
            Accepted.Add(observation);
            Active.Add(observation);
            return new ScheduledMessageHandle<T>(id, dueAt, destinationAddress, message);
        }

        public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Cancellations.Add(new CancellationObservation(destinationAddress, tokenId, cancellationToken));
            Active.RemoveAll(x => x.Destination == destinationAddress && x.Token == tokenId);
            return Task.CompletedTask;
        }
    }

    private class PublishTopology : DispatchProxy
    {
        public List<Type> Contracts { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name != "TryGetPublishAddress")
                throw new NotSupportedException(targetMethod.Name);
            Contracts.Add(targetMethod.IsGenericMethod ? targetMethod.GetGenericArguments()[0] : (Type)args![0]!);
            args![^1] = PublishDestination;
            return true;
        }
    }
}
