using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineScheduleActivityContractTests
{
    private static readonly Uri InputAddress = new("loopback://localhost/saga-schedule-input");
    private static readonly DateTimeOffset DueAt = new(2046, 7, 8, 9, 10, 11, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "new-activity-schedules-before-continuing-without-old-cancel")]
    public async Task NewSchedule_AcceptsTheTokenBeforeContinuingWithoutCancellationAsync()
    {
        var saga = new Saga();
        var notice = new Notice("reminder");
        IPipe<SendContext<Notice>> pipe = Pipe.Empty<SendContext<Notice>>();
        Guid acceptedToken = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var trace = new List<string>();
        using var cancellation = new CancellationTokenSource();
        MessageSchedulerContext scheduler = Scheduler((method, args) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            Assert.Equal(DueAt, args[0]);
            Assert.Same(notice, args[1]);
            Assert.Same(pipe, args[2]);
            Assert.Equal(cancellation.Token, args[3]);
            trace.Add("schedule");
            return Task.FromResult<ScheduledMessage<Notice>>(Accepted(notice, acceptedToken));
        });
        IBehaviorContext<Saga> context = Context(saga, scheduler, cancellation.Token);
        IBehavior<Saga> next = Next(context, () =>
        {
            Assert.Equal(acceptedToken, saga.ScheduleId);
            trace.Add("next");
        });

        await Activity(notice, pipe).ExecuteAsync(context, next);

        Assert.Equal(acceptedToken, saga.ScheduleId);
        Assert.Equal(["schedule", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "replacement-accepts-new-token-before-old-cancel-and-next")]
    public async Task Replacement_WaitsForAcceptanceAndOldCancellationBeforeContinuingAsync()
    {
        Guid oldToken = Guid.Parse("20000000-0000-0000-0000-000000000002");
        Guid newToken = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var saga = new Saga { ScheduleId = oldToken };
        var notice = new Notice("replacement");
        IPipe<SendContext<Notice>> pipe = Pipe.Empty<SendContext<Notice>>();
        var scheduleGate = new TaskCompletionSource<ScheduledMessage<Notice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        using var cancellation = new CancellationTokenSource();
        MessageSchedulerContext scheduler = Scheduler((method, args) => method.Name switch
        {
            "ScheduleSendAsync" => ScheduleAsync(args),
            "CancelScheduledSendAsync" => CancelAsync(args),
            _ => throw new NotSupportedException(method.Name),
        });
        Task<ScheduledMessage<Notice>> ScheduleAsync(object?[] args)
        {
            Assert.Equal(DueAt, args[0]);
            Assert.Same(notice, args[1]);
            Assert.Same(pipe, args[2]);
            Assert.Equal(cancellation.Token, args[3]);
            trace.Add("schedule");
            return scheduleGate.Task;
        }
        Task CancelAsync(object?[] args)
        {
            Assert.Equal(InputAddress, args[0]);
            Assert.Equal(oldToken, args[1]);
            Assert.Equal(cancellation.Token, args[2]);
            Assert.Equal(newToken, saga.ScheduleId);
            trace.Add("cancel");
            cancelStarted.SetResult();
            return cancelGate.Task;
        }
        IBehaviorContext<Saga> context = Context(saga, scheduler, cancellation.Token);
        IBehavior<Saga> next = Next(context, () => trace.Add("next"));

        Task pending = Activity(notice, pipe).ExecuteAsync(context, next);
        Assert.Equal(oldToken, saga.ScheduleId);
        Assert.Equal(["schedule"], trace);
        scheduleGate.SetResult(Accepted(notice, newToken));
        await cancelStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Equal(["schedule", "cancel"], trace);
        cancelGate.SetResult();
        await pending;
        Assert.Equal(["schedule", "cancel", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "own-scheduled-delivery-does-not-cancel-its-token")]
    public async Task OwnDelivery_ReplacesTheTokenWithoutCancelingItsPreviousScheduleAsync()
    {
        Guid oldToken = Guid.Parse("40000000-0000-0000-0000-000000000004");
        Guid newToken = Guid.Parse("50000000-0000-0000-0000-000000000005");
        var saga = new Saga { ScheduleId = oldToken };
        var notice = new Notice("self-delivery");
        var trace = new List<string>();
        MessageSchedulerContext scheduler = Scheduler((method, _) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            trace.Add("schedule");
            return Task.FromResult<ScheduledMessage<Notice>>(Accepted(notice, newToken));
        });
        IBehaviorContext<Saga> context = Context(saga, scheduler, CancellationToken.None, oldToken);
        IBehavior<Saga> next = Next(context, () => trace.Add("next"));

        await Activity(notice, Pipe.Empty<SendContext<Notice>>()).ExecuteAsync(context, next);

        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Equal(["schedule", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "failed-scheduling-retains-old-token-and-skips-next")]
    public async Task SchedulerFailure_RetainsTheOldTokenAndSkipsContinuationAsync()
    {
        Guid oldToken = Guid.Parse("60000000-0000-0000-0000-000000000006");
        var saga = new Saga { ScheduleId = oldToken };
        var notice = new Notice("failure");
        var failure = new InvalidOperationException("scheduler rejected the message");
        var calls = 0;
        MessageSchedulerContext scheduler = Scheduler((method, _) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            calls++;
            return Task.FromException<ScheduledMessage<Notice>>(failure);
        });
        IBehaviorContext<Saga> context = Context(saga, scheduler, CancellationToken.None);
        IBehavior<Saga> next = Next(context, () => throw new InvalidOperationException("The next behavior must not run."));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Activity(notice, Pipe.Empty<SendContext<Notice>>()).ExecuteAsync(context, next));

        Assert.Same(failure, actual);
        Assert.Equal(oldToken, saga.ScheduleId);
        Assert.Equal(1, calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "failed-old-cancellation-retains-accepted-new-token-and-skips-next")]
    public async Task OldCancellationFailure_RetainsTheAcceptedTokenAndSkipsContinuationAsync()
    {
        Guid oldToken = Guid.Parse("80000000-0000-0000-0000-000000000008");
        Guid newToken = Guid.Parse("90000000-0000-0000-0000-000000000009");
        var saga = new Saga { ScheduleId = oldToken };
        var notice = new Notice("cancel-failure");
        var failure = new InvalidOperationException("old schedule could not be canceled");
        var trace = new List<string>();
        using var cancellation = new CancellationTokenSource();
        MessageSchedulerContext scheduler = Scheduler((method, args) => method.Name switch
        {
            "ScheduleSendAsync" => ScheduleAsync(args),
            "CancelScheduledSendAsync" => CancelAsync(args),
            _ => throw new NotSupportedException(method.Name),
        });
        Task<ScheduledMessage<Notice>> ScheduleAsync(object?[] args)
        {
            Assert.Same(notice, args[1]);
            Assert.Equal(cancellation.Token, args[3]);
            trace.Add("schedule");
            return Task.FromResult<ScheduledMessage<Notice>>(Accepted(notice, newToken));
        }
        Task CancelAsync(object?[] args)
        {
            Assert.Equal(InputAddress, args[0]);
            Assert.Equal(oldToken, args[1]);
            Assert.Equal(cancellation.Token, args[2]);
            Assert.Equal(newToken, saga.ScheduleId);
            trace.Add("cancel");
            return Task.FromException(failure);
        }
        IBehaviorContext<Saga> context = Context(saga, scheduler, cancellation.Token);
        IBehavior<Saga> next = Next(context, () => throw new InvalidOperationException("The next behavior must not run."));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Activity(notice, Pipe.Empty<SendContext<Notice>>()).ExecuteAsync(context, next));

        Assert.Same(failure, actual);
        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Equal(["schedule", "cancel"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "pre-canceled-context-skips-message-factory-scheduler-and-next")]
    public async Task PreCanceledContext_SkipsMessageFactorySchedulerAndContinuationAsync()
    {
        Guid oldToken = Guid.Parse("a0000000-0000-0000-0000-00000000000a");
        var saga = new Saga { ScheduleId = oldToken };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factoryCalls = 0;
        MessageSchedulerContext scheduler = Scheduler((method, _) =>
            throw new InvalidOperationException($"Scheduler must not be called: {method.Name}"));
        IBehaviorContext<Saga> context = Context(saga, scheduler, cancellation.Token);
        IBehavior<Saga> next = Next(context, () => throw new InvalidOperationException("The next behavior must not run."));
        var activity = new ScheduleActivity<Saga, Notice>(new Schedule(), _ => DueAt,
            new ContextMessageFactory<IBehaviorContext<Saga>, Notice>(_ =>
            {
                factoryCalls++;
                return Task.FromResult(new InitializedMessage<Notice>(new Notice("forbidden"), Pipe.Empty<SendContext<Notice>>()));
            }));

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            activity.ExecuteAsync(context, next));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(oldToken, saga.ScheduleId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "cancellation-after-entry-before-factory-skips-untyped-dispatch")]
    public async Task CancellationAfterEntry_PreventsUntypedMessageFactoryAndDispatchAsync()
    {
        Guid oldToken = Guid.Parse("12000000-0000-0000-0000-000000000012");
        var saga = new Saga { ScheduleId = oldToken };
        using var cancellation = new CancellationTokenSource();
        var getterCalls = 0;
        var factoryCalls = 0;
        var schedule = new Schedule
        {
            BeforeGetTokenId = () =>
            {
                getterCalls++;
                cancellation.Cancel();
            },
        };
        MessageSchedulerContext scheduler = Scheduler((method, _) =>
            throw new InvalidOperationException($"Scheduler must not be called: {method.Name}"));
        IBehaviorContext<Saga> context = Context(saga, scheduler, cancellation.Token);
        IBehavior<Saga> next = Next(context, () => throw new InvalidOperationException("The next behavior must not run."));
        var activity = new ScheduleActivity<Saga, Notice>(schedule, _ => DueAt,
            new ContextMessageFactory<IBehaviorContext<Saga>, Notice>(_ =>
            {
                factoryCalls++;
                return Task.FromResult(new InitializedMessage<Notice>(new Notice("forbidden"), Pipe.Empty<SendContext<Notice>>()));
            }));

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            activity.ExecuteAsync(context, next));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(1, getterCalls);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(oldToken, saga.ScheduleId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "constructor-rejects-missing-schedule-time-and-message-factory")]
    public void Constructor_RejectsEachMissingCollaborator()
    {
        var schedule = new Schedule();
        ScheduleTimeProvider<Saga> timeProvider = _ => DueAt;
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Notice>(_ =>
            Task.FromResult(new InitializedMessage<Notice>(new Notice("valid"), Pipe.Empty<SendContext<Notice>>())));

        Assert.Equal("schedule", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleActivity<Saga, Notice>(null!, timeProvider, factory)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleActivity<Saga, Notice>(schedule, null!, factory)).ParamName);
        Assert.Equal("messageFactory", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleActivity<Saga, Notice>(schedule, timeProvider, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "typed-activity-replaces-token-before-old-cancel-and-continuation")]
    public async Task TypedSchedule_ReplacesTheTokenBeforeCancellationAndContinuationAsync()
    {
        Guid oldToken = Guid.Parse("b0000000-0000-0000-0000-00000000000b");
        Guid newToken = Guid.Parse("c0000000-0000-0000-0000-00000000000c");
        var saga = new Saga { ScheduleId = oldToken };
        var notice = new Notice("typed-replacement");
        IPipe<SendContext<Notice>> pipe = Pipe.Empty<SendContext<Notice>>();
        var trace = new List<string>();
        using var cancellation = new CancellationTokenSource();
        MessageSchedulerContext scheduler = Scheduler((method, args) => method.Name switch
        {
            "ScheduleSendAsync" => ScheduleAsync(args),
            "CancelScheduledSendAsync" => CancelAsync(args),
            _ => throw new NotSupportedException(method.Name),
        });
        Task<ScheduledMessage<Notice>> ScheduleAsync(object?[] args)
        {
            Assert.Equal(DueAt, args[0]);
            Assert.Same(notice, args[1]);
            Assert.Same(pipe, args[2]);
            Assert.Equal(cancellation.Token, args[3]);
            trace.Add("schedule");
            return Task.FromResult<ScheduledMessage<Notice>>(Accepted(notice, newToken));
        }
        Task CancelAsync(object?[] args)
        {
            Assert.Equal(InputAddress, args[0]);
            Assert.Equal(oldToken, args[1]);
            Assert.Equal(cancellation.Token, args[2]);
            Assert.Equal(newToken, saga.ScheduleId);
            trace.Add("cancel");
            return Task.CompletedTask;
        }
        IBehaviorContext<Saga, Notice> context = TypedContext(saga, scheduler, cancellation.Token);
        IBehavior<Saga, Notice> next = Proxy<IBehavior<Saga, Notice>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            Assert.Same(context, Assert.Single(args));
            Assert.Equal(newToken, saga.ScheduleId);
            trace.Add("next");
            return Task.CompletedTask;
        });

        await TypedActivity(notice, pipe).ExecuteAsync(context, next);

        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Equal(["schedule", "cancel", "next"], trace);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "typed-activity-cancels-only-foreign-previous-schedule")]
    public async Task TypedSchedule_CancelsOnlyAForeignPreviousScheduleAsync(bool hasPrevious, bool ownDelivery)
    {
        Guid oldToken = Guid.Parse("e0000000-0000-0000-0000-00000000000e");
        Guid newToken = Guid.Parse("f0000000-0000-0000-0000-00000000000f");
        Guid foreignToken = Guid.Parse("11000000-0000-0000-0000-000000000011");
        var saga = new Saga { ScheduleId = hasPrevious ? oldToken : null };
        var notice = new Notice("typed-ownership");
        var trace = new List<string>();
        MessageSchedulerContext scheduler = Scheduler((method, args) => method.Name switch
        {
            "ScheduleSendAsync" => ScheduleAsync(),
            "CancelScheduledSendAsync" => CancelAsync(args),
            _ => throw new NotSupportedException(method.Name),
        });
        Task<ScheduledMessage<Notice>> ScheduleAsync()
        {
            trace.Add("schedule");
            return Task.FromResult<ScheduledMessage<Notice>>(Accepted(notice, newToken));
        }
        Task CancelAsync(object?[] args)
        {
            Assert.True(hasPrevious);
            Assert.False(ownDelivery);
            Assert.Equal(oldToken, args[1]);
            Assert.Equal(newToken, saga.ScheduleId);
            trace.Add("cancel");
            return Task.CompletedTask;
        }
        Guid? headerToken = hasPrevious ? ownDelivery ? oldToken : foreignToken : null;
        IBehaviorContext<Saga, Notice> context = TypedContext(saga, scheduler, CancellationToken.None, headerToken);
        IBehavior<Saga, Notice> next = Proxy<IBehavior<Saga, Notice>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            Assert.Same(context, Assert.Single(args));
            Assert.Equal(newToken, saga.ScheduleId);
            trace.Add("next");
            return Task.CompletedTask;
        });

        await TypedActivity(notice, Pipe.Empty<SendContext<Notice>>()).ExecuteAsync(context, next);

        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Equal(hasPrevious && !ownDelivery ? ["schedule", "cancel", "next"] : ["schedule", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "typed-pre-canceled-context-skips-factory-scheduler-and-next")]
    public async Task TypedPreCanceledContext_SkipsFactorySchedulerAndContinuationAsync()
    {
        Guid oldToken = Guid.Parse("d0000000-0000-0000-0000-00000000000d");
        var saga = new Saga { ScheduleId = oldToken };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factoryCalls = 0;
        MessageSchedulerContext scheduler = Scheduler((method, _) =>
            throw new InvalidOperationException($"Scheduler must not be called: {method.Name}"));
        IBehaviorContext<Saga, Notice> context = TypedContext(saga, scheduler, cancellation.Token);
        IBehavior<Saga, Notice> next = Proxy<IBehavior<Saga, Notice>>((method, _) =>
            throw new InvalidOperationException($"Next must not be called: {method.Name}"));
        var activity = new ScheduleActivity<Saga, Notice, Notice>(new Schedule(), _ => DueAt,
            new ContextMessageFactory<IBehaviorContext<Saga, Notice>, Notice>(_ =>
            {
                factoryCalls++;
                return Task.FromResult(new InitializedMessage<Notice>(new Notice("forbidden"), Pipe.Empty<SendContext<Notice>>()));
            }));

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            activity.ExecuteAsync(context, next));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(oldToken, saga.ScheduleId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "cancellation-after-entry-before-factory-skips-typed-dispatch")]
    public async Task CancellationAfterEntry_PreventsTypedMessageFactoryAndDispatchAsync()
    {
        Guid oldToken = Guid.Parse("13000000-0000-0000-0000-000000000013");
        var saga = new Saga { ScheduleId = oldToken };
        using var cancellation = new CancellationTokenSource();
        var getterCalls = 0;
        var factoryCalls = 0;
        var schedule = new Schedule
        {
            BeforeGetTokenId = () =>
            {
                getterCalls++;
                cancellation.Cancel();
            },
        };
        MessageSchedulerContext scheduler = Scheduler((method, _) =>
            throw new InvalidOperationException($"Scheduler must not be called: {method.Name}"));
        IBehaviorContext<Saga, Notice> context = TypedContext(saga, scheduler, cancellation.Token);
        IBehavior<Saga, Notice> next = Proxy<IBehavior<Saga, Notice>>((method, _) =>
            throw new InvalidOperationException($"Next must not be called: {method.Name}"));
        var activity = new ScheduleActivity<Saga, Notice, Notice>(schedule, _ => DueAt,
            new ContextMessageFactory<IBehaviorContext<Saga, Notice>, Notice>(_ =>
            {
                factoryCalls++;
                return Task.FromResult(new InitializedMessage<Notice>(new Notice("forbidden"), Pipe.Empty<SendContext<Notice>>()));
            }));

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            activity.ExecuteAsync(context, next));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(1, getterCalls);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(oldToken, saga.ScheduleId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "typed-constructor-rejects-missing-schedule-time-and-message-factory")]
    public void TypedConstructor_RejectsEachMissingCollaborator()
    {
        var schedule = new Schedule();
        ScheduleTimeProvider<Saga, Notice> timeProvider = _ => DueAt;
        var factory = new ContextMessageFactory<IBehaviorContext<Saga, Notice>, Notice>(_ =>
            Task.FromResult(new InitializedMessage<Notice>(new Notice("valid"), Pipe.Empty<SendContext<Notice>>())));

        Assert.Equal("schedule", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleActivity<Saga, Notice, Notice>(null!, timeProvider, factory)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleActivity<Saga, Notice, Notice>(schedule, null!, factory)).ParamName);
        Assert.Equal("messageFactory", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleActivity<Saga, Notice, Notice>(schedule, timeProvider, null!)).ParamName);
    }

    private static ScheduleActivity<Saga, Notice> Activity(Notice notice, IPipe<SendContext<Notice>> pipe) =>
        new(new Schedule(), _ => DueAt,
            new ContextMessageFactory<IBehaviorContext<Saga>, Notice>(_ =>
                Task.FromResult(new InitializedMessage<Notice>(notice, pipe))));

    private static ScheduleActivity<Saga, Notice, Notice> TypedActivity(Notice notice, IPipe<SendContext<Notice>> pipe) =>
        new(new Schedule(), _ => DueAt,
            new ContextMessageFactory<IBehaviorContext<Saga, Notice>, Notice>(_ =>
                Task.FromResult(new InitializedMessage<Notice>(notice, pipe))));

    private static ScheduledMessage<Notice> Accepted(Notice notice, Guid token) =>
        new ScheduledMessageHandle<Notice>(token, DueAt, InputAddress, notice);

    private static MessageSchedulerContext Scheduler(Func<MethodInfo, object?[], object?> handler) =>
        Proxy<MessageSchedulerContext>(handler);

    private static IBehaviorContext<Saga> Context(Saga saga, MessageSchedulerContext scheduler,
        CancellationToken cancellationToken, Guid? scheduledToken = null)
    {
        Headers headers = SchedulingHeaders(scheduledToken);
        ReceiveContext receive = SchedulingReceiveContext();
        return Proxy<IBehaviorContext<Saga>>((method, args) => method.Name switch
        {
            "get_Saga" => saga,
            "get_CancellationToken" => cancellationToken,
            "get_Headers" => headers,
            "get_ReceiveContext" => receive,
            "TryGetPayload" => SetSchedulerPayload(method, args, scheduler),
            _ => throw new NotSupportedException(method.Name),
        });
    }

    private static IBehaviorContext<Saga, Notice> TypedContext(Saga saga, MessageSchedulerContext scheduler,
        CancellationToken cancellationToken, Guid? scheduledToken = null)
    {
        Headers headers = SchedulingHeaders(scheduledToken);
        ReceiveContext receive = SchedulingReceiveContext();
        return Proxy<IBehaviorContext<Saga, Notice>>((method, args) => method.Name switch
        {
            "get_Saga" => saga,
            "get_CancellationToken" => cancellationToken,
            "get_Headers" => headers,
            "get_ReceiveContext" => receive,
            "TryGetPayload" => SetSchedulerPayload(method, args, scheduler),
            _ => throw new NotSupportedException(method.Name),
        });
    }

    private static Headers SchedulingHeaders(Guid? scheduledToken) =>
        Proxy<Headers>((method, args) => method.Name switch
        {
            "Get" => SchedulingHeader(method, args, scheduledToken),
            _ => throw new NotSupportedException(method.Name),
        });

    private static ReceiveContext SchedulingReceiveContext() =>
        Proxy<ReceiveContext>((method, _) => method.Name switch
        {
            "get_InputAddress" => InputAddress,
            _ => throw new NotSupportedException(method.Name),
        });

    private static Guid? SchedulingHeader(MethodInfo method, object?[] args, Guid? scheduledToken)
    {
        Assert.Equal(typeof(Guid), Assert.Single(method.GetGenericArguments()));
        Assert.Equal(MessageHeaders.SchedulingTokenId, args[0]);
        Assert.Null(args[1]);
        return scheduledToken;
    }

    private static bool SetSchedulerPayload(MethodInfo method, object?[] args, MessageSchedulerContext scheduler)
    {
        Assert.Equal(typeof(MessageSchedulerContext), Assert.Single(method.GetGenericArguments()));
        args[0] = scheduler;
        return true;
    }

    private static IBehavior<Saga> Next(IBehaviorContext<Saga> expected, Action action) =>
        Proxy<IBehavior<Saga>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            Assert.Same(expected, Assert.Single(args));
            action();
            return Task.CompletedTask;
        });

    private static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T value = DispatchProxy.Create<T, CallProxy>();
        ((CallProxy)(object)value).Handler = handler;
        return value;
    }

    public class CallProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(Assert.IsAssignableFrom<MethodInfo>(targetMethod), args ?? []);
    }

    public sealed class Saga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = Guid.Parse("70000000-0000-0000-0000-000000000007");
        public Guid? RequestId { get; set; }
        public Guid? ScheduleId { get; set; }
    }

    public sealed record Notice(string Value);

    public sealed class Schedule : ISchedule<Saga, Notice>
    {
        public Action? BeforeGetTokenId { get; set; }
        public string Name => "notice";
        public IEvent<Notice> Received { get; set; } = null!;
        public IEvent<Notice> AnyReceived { get; set; } = null!;
        public TimeSpan GetDelay(IBehaviorContext<Saga> context) => TimeSpan.Zero;
        public Guid? GetTokenId(Saga instance)
        {
            BeforeGetTokenId?.Invoke();
            return instance.ScheduleId;
        }
        public void SetTokenId(Saga instance, Guid? tokenId) => instance.ScheduleId = tokenId;
    }
}
