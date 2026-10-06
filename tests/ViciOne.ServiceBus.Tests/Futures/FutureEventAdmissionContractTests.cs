using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureEventAdmissionContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-FUTURE-FAULTS", "terminal-fault-configuration-order-preserves-last-requested-timing")]
    public async Task TerminalFaultConfigurationOrder_AppliesTheLastRequestedTimingModeAsync(bool immediateAfterDeferred)
    {
        var machine = new RequestFuture(callbackBeforeOutcome: false, immediateAfterDeferred: immediateAfterDeferred);
        Guid first = Guid.NewGuid(), second = Guid.NewGuid();
        FutureState state = await StartAsync(machine, first, second);
        var outgoing = new OutgoingMessageRecorder();
        Task RaiseAsync(Guid id) => FutureBehaviorContextFactory.UseAsync(
            machine, machine.RequestFaulted, state, CreateFault(id),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context), outgoing,
            TestContext.Current.CancellationToken, requestId: state.CorrelationId);

        await RaiseAsync(first);
        Assert.Equal(new[] { first }, machine.FaultCallbacks);
        Assert.Equal(second, Assert.Single(state.Pending));
        Assert.NotNull(state.Faulted);
        Assert.Null(state.Completed);
        FutureMessage firstStored = state.Faults[first];
        if (immediateAfterDeferred)
        {
            Assert.Equal(1, machine.FaultFactoryCalls);
            Assert.Equal("faulted", Assert.IsType<Failure>(Assert.Single(outgoing.Messages)).Value);
        }
        else
        {
            Assert.Equal(0, machine.FaultFactoryCalls);
            Assert.Empty(outgoing.Messages);
            Assert.False(state.Faults.ContainsKey(state.CorrelationId));
            await RaiseAsync(second);
            Assert.Empty(state.Pending);
            Assert.Equal(new[] { first, second }, machine.FaultCallbacks);
            Assert.Equal(1, machine.FaultFactoryCalls);
            Assert.Equal("faulted", Assert.IsType<Failure>(Assert.Single(outgoing.Messages)).Value);
        }

        FutureMessage terminalStored = state.Faults[state.CorrelationId];
        await RaiseAsync(first);
        Assert.Same(firstStored, state.Faults[first]);
        Assert.Same(terminalStored, state.Faults[state.CorrelationId]);
        Assert.Equal(1, machine.FaultFactoryCalls);
        Assert.Single(outgoing.Messages);
        Assert.Equal(immediateAfterDeferred ? new[] { first } : new[] { first, second }, machine.FaultCallbacks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "tracked-response-admission-precedes-early-and-late-callbacks")]
    public async Task ResponseCallbacks_RunOnceForEachAcceptedOperationRegardlessOfConfigurationOrderAsync(bool callbackBeforeOutcome)
    {
        var machine = new RequestFuture(callbackBeforeOutcome);
        Guid first = Guid.NewGuid(), second = Guid.NewGuid();
        FutureState state = await StartAsync(machine, first, second);
        var outgoing = new OutgoingMessageRecorder();
        Task RaiseAsync(Guid id, string value, bool repeatSameContext = false) => FutureBehaviorContextFactory.UseAsync(
            machine, machine.ResponseReceived, state, new Response(id, value), async context =>
            {
                await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
                if (repeatSameContext)
                    await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
            }, outgoing, TestContext.Current.CancellationToken, requestId: state.CorrelationId);

        await RaiseAsync(Guid.NewGuid(), "unknown");
        Assert.Empty(machine.ResponseCallbacks);
        Assert.Empty(state.Results);
        Assert.Equal(2, state.Pending.Count);
        Assert.Equal(0, machine.ResultFactoryCalls);

        await RaiseAsync(first, "first", repeatSameContext: true);
        Assert.Equal(new[] { "first" }, machine.ResponseCallbacks);
        FutureMessage storedFirst = state.Results[first];
        Assert.Single(state.Results);
        Assert.Equal(second, Assert.Single(state.Pending));
        Assert.Equal(0, machine.ResultFactoryCalls);
        Assert.Empty(outgoing.Messages);
        await RaiseAsync(first, "forged");
        Assert.Same(storedFirst, state.Results[first]);
        Assert.Equal(new[] { "first" }, machine.ResponseCallbacks);

        await RaiseAsync(second, "second");
        Assert.Equal(new[] { "first", "second" }, machine.ResponseCallbacks);
        Assert.Empty(state.Pending);
        Assert.NotNull(state.Completed);
        Assert.Null(state.Faulted);
        Assert.Equal(1, machine.ResultFactoryCalls);
        Assert.Single(outgoing.Messages);
        FutureMessage terminal = state.Results[state.CorrelationId];
        await RaiseAsync(second, "terminal replay", repeatSameContext: true);
        Assert.Same(storedFirst, state.Results[first]);
        Assert.Same(terminal, state.Results[state.CorrelationId]);
        Assert.Equal(new[] { "first", "second" }, machine.ResponseCallbacks);
        Assert.Equal(1, machine.ResultFactoryCalls);
        Assert.Single(outgoing.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-FUTURE-FAULTS", "tracked-fault-admission-preserves-wait-for-all-and-callback-order")]
    public async Task RequestFaultCallbacks_AcceptRemainingWorkAfterFirstFaultAndRejectReplayAsync(bool callbackBeforeOutcome)
    {
        var machine = new RequestFuture(callbackBeforeOutcome);
        Guid first = Guid.NewGuid(), second = Guid.NewGuid();
        FutureState state = await StartAsync(machine, first, second);
        var outgoing = new OutgoingMessageRecorder();
        Task RaiseAsync(Guid id, bool repeatSameContext = false) => FutureBehaviorContextFactory.UseAsync(
            machine, machine.RequestFaulted, state, CreateFault(id), async context =>
            {
                await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
                if (repeatSameContext)
                    await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
            }, outgoing, TestContext.Current.CancellationToken, requestId: state.CorrelationId);

        await RaiseAsync(Guid.NewGuid());
        Assert.Empty(machine.FaultCallbacks);
        Assert.Empty(state.Faults);
        Assert.Null(state.Faulted);
        Assert.Equal(2, state.Pending.Count);
        await RaiseAsync(first, repeatSameContext: true);
        Assert.Equal(new[] { first }, machine.FaultCallbacks);
        Assert.Equal(second, Assert.Single(state.Pending));
        Assert.NotNull(state.Faulted);
        Assert.Null(state.Completed);
        Assert.Equal(0, machine.FaultFactoryCalls);
        Assert.Empty(outgoing.Messages);
        FutureMessage storedFirst = state.Faults[first];
        await RaiseAsync(first);
        Assert.Same(storedFirst, state.Faults[first]);
        Assert.Equal(new[] { first }, machine.FaultCallbacks);

        await RaiseAsync(second);
        Assert.Equal(new[] { first, second }, machine.FaultCallbacks);
        Assert.Empty(state.Pending);
        Assert.Null(state.Completed);
        Assert.Equal(1, machine.FaultFactoryCalls);
        Assert.Single(outgoing.Messages);
        FutureMessage terminal = state.Faults[state.CorrelationId];
        await RaiseAsync(second, repeatSameContext: true);
        Assert.Same(storedFirst, state.Faults[first]);
        Assert.Same(terminal, state.Faults[state.CorrelationId]);
        Assert.Equal(new[] { first, second }, machine.FaultCallbacks);
        Assert.Equal(1, machine.FaultFactoryCalls);
        Assert.Single(outgoing.Messages);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "tracked-routing-slip-terminal-admission-preserves-callbacks-and-outcome")]
    public async Task RoutingSlipTerminalEvents_RejectUnknownAndReplayButInvokeAcceptedCallbacksAsync(bool faulted, bool callbackBeforeOutcome)
    {
        var machine = new RoutingFuture(callbackBeforeOutcome);
        Guid trackingNumber = Guid.NewGuid();
        var state = new FutureState { CorrelationId = Guid.NewGuid(), Pending = [trackingNumber] };
        await FutureBehaviorContextFactory.UseAsync(machine, machine.CommandReceived, state,
            new Command(state.CorrelationId, trackingNumber, trackingNumber), async context =>
            {
                state.Command = context.CreateFutureMessage(context.Message);
                await machine.Accessor.SetAsync(context, machine.GetState(machine.WaitingForCompletion.Name),
                    TestContext.Current.CancellationToken);
            }, cancellationToken: TestContext.Current.CancellationToken);

        var outgoing = new OutgoingMessageRecorder();
        Task RaiseAsync(Guid id, bool repeatSameContext = false) => faulted
            ? FutureBehaviorContextFactory.UseAsync(machine, machine.SlipFaulted, state,
                (IRoutingSlipFaulted)new RoutingSlipFaultedMessage(id, DateTimeOffset.UnixEpoch, TimeSpan.Zero,
                    Array.Empty<IActivityException>(), new Dictionary<string, object>()),
                async context =>
                {
                    await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
                    if (repeatSameContext)
                        await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
                }, outgoing, TestContext.Current.CancellationToken)
            : FutureBehaviorContextFactory.UseAsync(machine, machine.SlipCompleted, state,
                (IRoutingSlipCompleted)new RoutingSlipCompletedMessage(id, DateTimeOffset.UnixEpoch, TimeSpan.Zero,
                    new Dictionary<string, object>()),
                async context =>
                {
                    await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
                    if (repeatSameContext)
                        await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
                }, outgoing, TestContext.Current.CancellationToken);

        await RaiseAsync(Guid.NewGuid());
        Assert.Equal(trackingNumber, Assert.Single(state.Pending));
        Assert.Empty(state.Results);
        Assert.Empty(state.Faults);
        Assert.Equal(0, machine.CompletedCallbacks);
        Assert.Equal(0, machine.FaultedCallbacks);
        Assert.Equal(0, machine.ResultFactoryCalls);
        Assert.Equal(0, machine.FaultFactoryCalls);
        await RaiseAsync(trackingNumber, repeatSameContext: true);
        Assert.Empty(state.Pending);
        Assert.Equal(faulted ? 0 : 1, machine.CompletedCallbacks);
        Assert.Equal(faulted ? 1 : 0, machine.FaultedCallbacks);
        Assert.Equal(faulted ? 0 : 1, machine.ResultFactoryCalls);
        Assert.Equal(faulted ? 1 : 0, machine.FaultFactoryCalls);
        Assert.Equal(faulted, state.Faulted.HasValue);
        Assert.Equal(!faulted, state.Completed.HasValue);
        FutureMessage stored = (faulted ? state.Faults : state.Results)[trackingNumber];
        FutureMessage terminal = (faulted ? state.Faults : state.Results)[state.CorrelationId];
        await RaiseAsync(trackingNumber);
        Assert.Same(stored, (faulted ? state.Faults : state.Results)[trackingNumber]);
        Assert.Same(terminal, (faulted ? state.Faults : state.Results)[state.CorrelationId]);
        Assert.Equal(faulted ? 0 : 1, machine.CompletedCallbacks);
        Assert.Equal(faulted ? 1 : 0, machine.FaultedCallbacks);
        Assert.Equal(faulted ? 0 : 1, machine.ResultFactoryCalls);
        Assert.Equal(faulted ? 1 : 0, machine.FaultFactoryCalls);
        Assert.Empty(outgoing.Messages);
    }

    private static async Task<FutureState> StartAsync(RequestFuture machine, Guid first, Guid second)
    {
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var outgoing = new OutgoingMessageRecorder();
        await FutureBehaviorContextFactory.UseAsync(machine, machine.CommandReceived, state,
            new Command(state.CorrelationId, first, second),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context), outgoing,
            TestContext.Current.CancellationToken, new Uri("loopback://localhost/admitted-outcome"), Guid.NewGuid());
        Assert.Equal(2, outgoing.Messages.Count);
        Assert.Equal(2, state.Pending.Count);
        Assert.Contains(first, state.Pending);
        Assert.Contains(second, state.Pending);
        return state;
    }

    private static Fault<Request> CreateFault(Guid requestId) => new FaultEvent<Request>
    {
        Message = new Request(requestId), FaultId = Guid.NewGuid(), FaultedMessageId = Guid.NewGuid(),
        Timestamp = DateTimeOffset.UnixEpoch, Exceptions = [], Host = HostMetadataCache.Host,
        FaultMessageTypes = MessageTypeCache<Request>.MessageTypeNames.ToArray(),
    };

    private sealed class RequestFuture : Future<Command, Outcome, Failure>
    {
        public List<string> ResponseCallbacks { get; } = [];
        public List<Guid> FaultCallbacks { get; } = [];
        public int ResultFactoryCalls { get; private set; }
        public int FaultFactoryCalls { get; private set; }
        public IEvent<Response> ResponseReceived { get; }
        public IEvent<Fault<Request>> RequestFaulted { get; }
        public RequestFuture(bool callbackBeforeOutcome, bool immediateAfterDeferred = false)
        {
            ConfigureCommand(configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            IFutureRequestConfigurator<Failure, Request, Request>? requestConfiguration = null;
            var request = SendRequests<Request, Request>(
                command => new[] { new Request(command.First), new Request(command.Second) }, configuration =>
                {
                    requestConfiguration = configuration;
                    configuration.SetRequestFactory(context => context.Message);
                    configuration.TrackPendingRequest(message => message.Id);
                    if (callbackBeforeOutcome)
                        configuration.WhenFaulted(binder => binder.Then(context => FaultCallbacks.Add(context.Message.Message.Id)));
                });
            RequestFaulted = request.Faulted;
            if (!callbackBeforeOutcome)
                requestConfiguration!.WhenFaulted(binder => binder.Then(context => FaultCallbacks.Add(context.Message.Message.Id)));
            IFutureResponseConfigurator<Outcome, Response>? responseConfiguration = null;
            ResponseReceived = request.OnResponseReceived<Response>(configuration =>
            {
                responseConfiguration = configuration;
                configuration.CompletePendingRequest(message => message.Id);
                if (callbackBeforeOutcome)
                    configuration.WhenReceived(binder => binder.Then(context => ResponseCallbacks.Add(context.Message.Value)));
            }).Completed;
            if (!callbackBeforeOutcome)
                responseConfiguration!.WhenReceived(binder => binder.Then(context => ResponseCallbacks.Add(context.Message.Value)));
            WhenAllCompleted(configuration => configuration.SetResultFactory(_ =>
            {
                ResultFactoryCalls++;
                return new Outcome("completed");
            }));
            WhenAllCompletedOrFaulted(configuration => configuration.SetFaultFactory(_ =>
            {
                FaultFactoryCalls++;
                return new Failure("faulted");
            }));
            if (immediateAfterDeferred)
                WhenAnyFaulted(configuration => configuration.SetFaultFactory(_ =>
                {
                    FaultFactoryCalls++;
                    return new Failure("faulted");
                }));
        }
    }

    private sealed class RoutingFuture : Future<Command, Outcome, Failure>
    {
        public int CompletedCallbacks { get; private set; }
        public int FaultedCallbacks { get; private set; }
        public int ResultFactoryCalls { get; private set; }
        public int FaultFactoryCalls { get; private set; }
        public IEvent<IRoutingSlipCompleted> SlipCompleted { get; }
        public IEvent<IRoutingSlipFaulted> SlipFaulted { get; }
        public RoutingFuture(bool callbackBeforeOutcome)
        {
            IFutureRoutingSlipConfigurator<Outcome, Failure, Command>? routingConfiguration = null;
            var handle = ExecuteRoutingSlip(configuration =>
            {
                routingConfiguration = configuration;
                configuration.TrackPendingRoutingSlip();
                if (callbackBeforeOutcome)
                {
                    configuration.WhenRoutingSlipCompleted(binder => binder.Then(_ => CompletedCallbacks++));
                    configuration.WhenRoutingSlipFaulted(binder => binder.Then(_ => FaultedCallbacks++));
                }
            });
            if (!callbackBeforeOutcome)
            {
                routingConfiguration!.WhenRoutingSlipCompleted(binder => binder.Then(_ => CompletedCallbacks++));
                routingConfiguration.WhenRoutingSlipFaulted(binder => binder.Then(_ => FaultedCallbacks++));
            }
            SlipCompleted = handle.Completed;
            SlipFaulted = handle.Faulted;
            WhenAllCompleted(configuration => configuration.SetResultFactory(_ =>
            {
                ResultFactoryCalls++;
                return new Outcome("completed");
            }));
            WhenAnyFaulted(configuration => configuration.SetFaultFactory(_ =>
            {
                FaultFactoryCalls++;
                return new Failure("faulted");
            }));
        }
    }

    public sealed record Command(Guid CorrelationId, Guid First, Guid Second) : ICorrelatedBy<Guid>;
    public sealed record Request(Guid Id);
    public sealed record Response(Guid Id, string Value);
    public sealed record Outcome(string Value);
    public sealed record Failure(string Value);
}
