using System.Collections.Concurrent;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineResponseAndFaultIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RESPOND", "sync-async-status-and-missing-fault-matrix")]
    public async Task RespondMatrix_ReturnsSyncAndAsyncResponsesAndFaultsTheMissingInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("respond-matrix", timeout);
        var machine = new ResponseMachine();
        ISagaStateMachineTestHarness<ResponseMachine, ResponseState> sagaHarness =
            harness.AddSagaStateMachine<ResponseMachine, ResponseState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            IRequestClient<ResponseStart> startClient = harness.CreateRequestClient<ResponseStart>();
            IRequestClient<ResponseStatusRequest> statusClient = harness.CreateRequestClient<ResponseStatusRequest>();

            Response<ResponseStarted> started = await startClient.GetResponseAsync<ResponseStarted>(
                new ResponseStart(correlationId),
                cancellationToken: cancellationToken);
            Assert.Equal(new ResponseStarted(correlationId, "sync"), started.Message);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Running, timeout, TestContext.Current.CancellationToken));

            Response<ResponseStatus> status = await statusClient.GetResponseAsync<ResponseStatus>(
                new ResponseStatusRequest(correlationId),
                cancellationToken: cancellationToken);
            Assert.Equal(new ResponseStatus(correlationId, machine.Running.Name, "async"), status.Message);

            Guid missingId = NewId.NextGuid();
            RequestFaultException missing = await Assert.ThrowsAsync<RequestFaultException>(() =>
                statusClient.GetResponseAsync<ResponseStatus>(new ResponseStatusRequest(missingId), cancellationToken));
            Assert.Contains(TypeCache<ResponseStatusRequest>.ShortName, missing.Message, StringComparison.Ordinal);

            ResponseState? instance = sagaHarness.Sagas.FindById(correlationId);
            Assert.NotNull(instance);
            Assert.Equal(1, instance.StartCount);
            Assert.Equal(1, instance.StatusCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Snapshot<ResponseStart>());
        Assert.Equal(2, harness.Consumed.Snapshot<ResponseStatusRequest>().Count());
        Assert.Single(harness.Sent.Snapshot<ResponseStarted>());
        Assert.Single(harness.Sent.Snapshot<ResponseStatus>());
        Assert.Single(harness.Sent.Snapshot<Fault<ResponseStatusRequest>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OUTBOX", "response-commit-fault-discard-and-single-terminal-fault")]
    public async Task Outbox_CommitsTheSuccessfulResponseAndPublishesOneFaultAfterAllRetriesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("outbox-response", timeout);
        var attempts = new AttemptRecorder();
        var machine = new OutboxMachine(attempts);
        var repository = new InMemorySagaRepository<OutboxState>();
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.UseMessageRetry(retry => retry.Immediate(5));
            endpoint.UseVolatileOutbox();
        };
        ISagaStateMachineTestHarness<OutboxMachine, OutboxState> sagaHarness =
            harness.AddSagaStateMachine<OutboxMachine, OutboxState>(machine, repository);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<OutboxStart> client = harness.CreateRequestClient<OutboxStart>();
            Guid successId = NewId.NextGuid();
            Response<OutboxStarted> success = await client.Advanced().GetResponseAsync<OutboxStarted>(
                new OutboxStart(successId, Fail: false),
                cancellationToken: cancellationToken);

            Assert.Equal(new OutboxStarted(successId, "committed"), success.Message);
            Assert.Equal(successId, await sagaHarness.WaitForSagaInStateAsync(successId, machine.Running, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, attempts.Count(successId));

            Guid failureId = NewId.NextGuid();
            await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.Advanced().GetResponseAsync<OutboxStarted>(
                    new OutboxStart(failureId, Fail: true),
                    cancellationToken: cancellationToken));

            Assert.Equal(6, attempts.Count(failureId));
            Assert.Null(await repository.LoadAsync(failureId, TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Sent.Snapshot<OutboxStarted>());
        ISentMessage<Fault<OutboxStart>> terminalFault = Assert.Single(
            harness.Sent.Snapshot<Fault<OutboxStart>>());
        Assert.True(terminalFault.Context.Message.Message.Fail);
        ExceptionInfo exception = Assert.Single(terminalFault.Context.Message.Exceptions);
        Assert.Equal(TypeCache<EventExecutionException>.ShortName, exception.ExceptionType);
        Assert.NotNull(exception.InnerException);
        ExceptionInfo innerException = exception.InnerException;
        Assert.Equal(TypeCache<ExpectedOutboxFailure>.ShortName, innerException.ExceptionType);
        Assert.Equal(ExpectedOutboxFailure.FailureMessage, innerException.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-MISSING", "substitute-response-for-missing-instance")]
    public async Task MissingInstance_ReturnsOnlyTheConfiguredSubstituteResponseAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("missing-substitute", timeout);
        var machine = new MissingResponseMachine();
        harness.AddSagaStateMachine<MissingResponseMachine, MissingResponseState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<MissingStatusRequest> client = harness.CreateRequestClient<MissingStatusRequest>();
            (Task<Response<ExistingStatus>> status, Task<Response<InstanceMissing>> missing) =
                await client.Advanced().GetResponseAsync<ExistingStatus, InstanceMissing>(
                    new MissingStatusRequest("service-a"),
                    cancellationToken: cancellationToken);

            Response<InstanceMissing> substitute = await missing.WaitAsync(timeout, cancellationToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => status);

            Assert.Equal(new InstanceMissing("service-a", "substitute"), substitute.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Snapshot<MissingStatusRequest>());
        Assert.Empty(harness.Sent.Snapshot<ExistingStatus>());
        Assert.Single(harness.Sent.Snapshot<InstanceMissing>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CATCH", "respond-publish-and-retain-or-remove-matrix")]
    public async Task Catch_RespondsAndPublishesBeforeEitherRetainingOrRemovingTheInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("catch-matrix", timeout);
        var machine = new CatchMachine();
        var repository = new InMemorySagaRepository<CatchState>();
        ISagaStateMachineTestHarness<CatchMachine, CatchState> sagaHarness =
            harness.AddSagaStateMachine<CatchMachine, CatchState>(machine, repository);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid retainedId = NewId.NextGuid();
            IRequestClient<RetainedCatchStart> retainedClient = harness.CreateRequestClient<RetainedCatchStart>();
            Response<CatchResponse> retained = await retainedClient.GetResponseAsync<CatchResponse>(
                new RetainedCatchStart(retainedId),
                cancellationToken);

            Assert.Equal(new CatchResponse(retainedId, "retained"), retained.Message);
            Assert.Equal(retainedId, await sagaHarness.WaitForSagaInStateAsync(retainedId, machine.Failed, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, Assert.IsType<CatchState>(sagaHarness.Sagas.FindById(retainedId)).CaughtCount);

            Guid removedId = NewId.NextGuid();
            Task<IConsumedMessage<RemovedCatchStart>> removedConsumed = sagaHarness.Consumed
                .SelectAsync<RemovedCatchStart>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IRequestClient<RemovedCatchStart> removedClient = harness.CreateRequestClient<RemovedCatchStart>();
            Response<CatchResponse> removed = await removedClient.GetResponseAsync<CatchResponse>(
                new RemovedCatchStart(removedId),
                cancellationToken);

            Assert.Equal(new CatchResponse(removedId, "removed"), removed.Message);
            Assert.Null((await removedConsumed.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null(await repository.LoadAsync(removedId, TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        CatchPublished[] published = harness.Published
            .Snapshot<CatchPublished>()
            .Select(message => message.Context.Message)
            .OrderBy(message => message.Mode, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [new CatchPublished(published[0].CorrelationId, "removed"), new CatchPublished(published[1].CorrelationId, "retained")],
            published);
        Assert.Equal(2, harness.Sent.Snapshot<CatchResponse>().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "published-fault-self-consumption-and-missing-fault")]
    public async Task FaultEvents_ArePublishedCorrelatedAndConsumedByTheMachineOrMissingPolicyExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("fault-events", timeout);
        var machine = new SelfFaultMachine();
        ISagaStateMachineTestHarness<SelfFaultMachine, SelfFaultState> sagaHarness =
            harness.AddSagaStateMachine<SelfFaultMachine, SelfFaultState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            Task<IPublishedMessage<Fault<ThrowingSignal>>> published = harness.Published
                .SelectAsync<Fault<ThrowingSignal>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.InputQueueSendEndpoint.SendAsync(new FaultBegin(correlationId), cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Waiting, timeout, TestContext.Current.CancellationToken));
            await harness.InputQueueSendEndpoint.SendAsync(new ThrowingSignal(correlationId), cancellationToken);

            Fault<ThrowingSignal> activityFault = (await published.WaitAsync(timeout, cancellationToken)).Context.Message;
            Assert.Equal(correlationId, activityFault.Message.CorrelationId);
            Assert.Equal(TypeCache<ExpectedStateMachineFailure>.ShortName, Assert.Single(activityFault.Exceptions).ExceptionType);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Failed, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, Assert.IsType<SelfFaultState>(sagaHarness.Sagas.FindById(correlationId)).FaultCount);

            Guid missingId = NewId.NextGuid();
            Task<IPublishedMessage<Fault<MissingFaultSignal>>> missingPublished = harness.Published
                .SelectAsync<Fault<MissingFaultSignal>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new MissingFaultSignal(missingId), cancellationToken);
            Fault<MissingFaultSignal> missingFault =
                (await missingPublished.WaitAsync(timeout, cancellationToken)).Context.Message;

            Assert.Equal(missingId, missingFault.Message.CorrelationId);
            Assert.Null(sagaHarness.Sagas.FindById(missingId));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<Fault<ThrowingSignal>>());
        Assert.Single(harness.Published.Snapshot<Fault<MissingFaultSignal>>());
        Assert.Single(sagaHarness.Consumed.Snapshot<Fault<ThrowingSignal>>());
    }

    private static InMemoryTestHarness CreateHarness(string prefix, TimeSpan timeout) =>
        new($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record ResponseStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ResponseStarted(Guid CorrelationId, string Mode);

    public sealed record ResponseStatusRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ResponseStatus(Guid CorrelationId, string Status, string Mode);

    public sealed class ResponseState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int StartCount { get; set; }

        public int StatusCount { get; set; }
    }

    public sealed class ResponseMachine : ViciOneServiceBusStateMachine<ResponseState>
    {
        public ResponseMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => StatusRequested, configuration => configuration.OnMissingInstance(missing => missing.Fault()));
            Initially(
                When(Started)
                    .Then(context => context.Saga.StartCount++)
                    .Respond(context => new ResponseStarted(context.Saga.CorrelationId, "sync"))
                    .TransitionTo(Running));
            DuringAny(
                When(StatusRequested)
                    .Then(context => context.Saga.StatusCount++)
                    .RespondAwaited(context => Task.FromResult(new ResponseStatus(
                        context.Saga.CorrelationId,
                        context.Saga.CurrentState,
                        "async"))));
        }

        public State Running { get; } = null!;

        public Event<ResponseStart> Started { get; } = null!;

        public Event<ResponseStatusRequest> StatusRequested { get; } = null!;
    }

    public sealed record OutboxStart(Guid CorrelationId, bool Fail) : CorrelatedBy<Guid>;

    public sealed record OutboxStarted(Guid CorrelationId, string Result);

    public sealed class OutboxState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class OutboxMachine : ViciOneServiceBusStateMachine<OutboxState>
    {
        public OutboxMachine(AttemptRecorder attempts)
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Started, context => context.Message.Fail)
                    .Then(context =>
                    {
                        attempts.Record(context.Message.CorrelationId);
                        throw new ExpectedOutboxFailure();
                    }),
                When(Started, context => !context.Message.Fail)
                    .Then(context => attempts.Record(context.Message.CorrelationId))
                    .Respond(context => new OutboxStarted(context.Saga.CorrelationId, "committed"))
                    .TransitionTo(Running));
        }

        public State Running { get; } = null!;

        public Event<OutboxStart> Started { get; } = null!;
    }

    public sealed class AttemptRecorder
    {
        private readonly ConcurrentDictionary<Guid, int> _attempts = new();

        public void Record(Guid correlationId) => _attempts.AddOrUpdate(correlationId, 1, (_, count) => count + 1);

        public int Count(Guid correlationId) => _attempts.GetValueOrDefault(correlationId);
    }

    public sealed class ExpectedOutboxFailure : Exception
    {
        public const string FailureMessage = "expected outbox failure";

        public ExpectedOutboxFailure()
            : base(FailureMessage)
        {
        }
    }

    public sealed record MissingStatusRequest(string ServiceName);

    public sealed record ExistingStatus(string ServiceName, string Status);

    public sealed record InstanceMissing(string ServiceName, string Result);

    public sealed class MissingResponseState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string ServiceName { get; set; } = string.Empty;
    }

    public sealed class MissingResponseMachine : ViciOneServiceBusStateMachine<MissingResponseState>
    {
        public MissingResponseMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Status, configuration => configuration
                .CorrelateBy(instance => instance.ServiceName, context => context.Message.ServiceName)
                .OnMissingInstance(missing => missing.ExecuteAwaited(context =>
                    context.RespondAsync(new InstanceMissing(context.Message.ServiceName, "substitute")))));
            During(
                Running,
                When(Status).Respond(context => new ExistingStatus(context.Saga.ServiceName, "running")));
        }

        public State Running { get; } = null!;

        public Event<MissingStatusRequest> Status { get; } = null!;
    }

    public sealed record RetainedCatchStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record RemovedCatchStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CatchResponse(Guid CorrelationId, string Mode);

    public sealed record CatchPublished(Guid CorrelationId, string Mode);

    public sealed class CatchState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int CaughtCount { get; set; }
    }

    public sealed class CatchMachine : ViciOneServiceBusStateMachine<CatchState>
    {
        public CatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Retained)
                    .Then(_ => throw new ExpectedCatchFailure())
                    .TransitionTo(Running)
                    .Catch<ExpectedCatchFailure>(caught => caught
                        .Then(context => context.Saga.CaughtCount++)
                        .Respond(context => new CatchResponse(context.Saga.CorrelationId, "retained"))
                        .Publish(context => new CatchPublished(context.Saga.CorrelationId, "retained"))
                        .TransitionTo(Failed)),
                When(Removed)
                    .Then(_ => throw new ExpectedCatchFailure())
                    .TransitionTo(Running)
                    .Catch<ExpectedCatchFailure>(caught => caught
                        .Then(context => context.Saga.CaughtCount++)
                        .Respond(context => new CatchResponse(context.Saga.CorrelationId, "removed"))
                        .Publish(context => new CatchPublished(context.Saga.CorrelationId, "removed"))
                        .Finalize()));
            SetCompletedWhenFinalized();
        }

        public State Running { get; } = null!;

        public State Failed { get; } = null!;

        public Event<RetainedCatchStart> Retained { get; } = null!;

        public Event<RemovedCatchStart> Removed { get; } = null!;
    }

    public sealed class ExpectedCatchFailure : Exception;

    public sealed record FaultBegin(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ThrowingSignal(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record MissingFaultSignal(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class SelfFaultState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int FaultCount { get; set; }
    }

    public sealed class SelfFaultMachine : ViciOneServiceBusStateMachine<SelfFaultState>
    {
        public SelfFaultMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Missing, configuration => configuration.OnMissingInstance(missing => missing.Fault()));
            Initially(When(Begin).TransitionTo(Waiting));
            During(
                Waiting,
                When(Throwing).Then(_ => throw new ExpectedStateMachineFailure()),
                When(ThrowingFaulted)
                    .Then(context => context.Saga.FaultCount++)
                    .TransitionTo(Failed));
        }

        public State Waiting { get; } = null!;

        public State Failed { get; } = null!;

        public Event<FaultBegin> Begin { get; } = null!;

        public Event<ThrowingSignal> Throwing { get; } = null!;

        public Event<Fault<ThrowingSignal>> ThrowingFaulted { get; } = null!;

        public Event<MissingFaultSignal> Missing { get; } = null!;
    }

    public sealed class ExpectedStateMachineFailure : Exception;
}
