using System.Linq.Expressions;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class SagaTestHarnessBehaviorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA", "creation-consumption-state-and-publication")]
    public async Task ClassicSagaHarness_ObservesCreationStateConsumptionAndPublicationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        SagaTestHarness<ClassicSaga> sagaHarness = harness.AddSaga<ClassicSaga>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new StartSaga(sagaId, "expected", [new SagaValue("first"), new SagaValue("second")]),
                cancellationToken);

            Guid? existing = await sagaHarness.WaitForSagaAsync(sagaId, timeout, TestContext.Current.CancellationToken);
            ClassicSaga? created = sagaHarness.Created.FindById(sagaId);
            ClassicSaga? observed = sagaHarness.Sagas.FindById(sagaId);
            IPublishedMessage<SagaStarted> published = await harness.Published
                .SelectAsync<SagaStarted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(sagaId, existing);
            Assert.NotNull(created);
            Assert.Same(created, observed);
            Assert.Equal("expected", created.Value);
            Assert.Equal(["first", "second"], created.Values);
            Assert.True(await harness.Sent.AnyAsync<StartSaga>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<StartSaga>(cancellationToken));
            Assert.True(await sagaHarness.Consumed.AnyAsync<StartSaga>(cancellationToken));
            Assert.Equal(sagaId, published.Context.Message.CorrelationId);
            Assert.Equal("expected", published.Context.Message.Value);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA", "match-and-nonexistence")]
    public async Task ClassicSagaHarness_MatchAndNotExistsExposeRepositoryStateExactlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        SagaTestHarness<ClassicSaga> sagaHarness = harness.AddSaga<ClassicSaga>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new StartSaga(sagaId, "match", [new SagaValue("value")]),
                cancellationToken);

            IReadOnlyList<Guid> matching = await sagaHarness.WaitForSagasAsync(x => x.Value == "match", timeout, TestContext.Current.CancellationToken);
            Guid missingId = NewId.NextGuid();
            Guid? missing = await sagaHarness.WaitForSagaRemovalAsync(missingId, timeout, TestContext.Current.CancellationToken);

            Assert.Equal([sagaId], matching);
            Assert.Equal(missingId, missing);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE", "request-fault-correlation-and-transition")]
    public async Task StateMachineHarness_CorrelatesTheRequestFaultAndObservesTheResultingStateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        var requestReceived = new TaskCompletionSource<ConsumeContext<ExecuteRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        var machine = new RequestStateMachine(new Uri(harness.BaseAddress, "execute-request"));
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint("execute-request", endpoint =>
            endpoint.Handler<ExecuteRequest>(context =>
            {
                requestReceived.TrySetResult(context);
                return Task.FromException(new ExpectedRequestException("request failed"));
            }));
        ISagaStateMachineTestHarness<RequestStateMachine, RequestState> sagaHarness =
            harness.AddSagaStateMachine<RequestStateMachine, RequestState>(machine);

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new StartRequest(sagaId, "key"), cancellationToken);

            IConsumedMessage<StartRequest> start = await harness.Consumed
                .SelectAsync<StartRequest>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(start.Exception);
            ConsumeContext<ExecuteRequest> request = await requestReceived.Task.WaitAsync(timeout, cancellationToken);
            IConsumedMessage<Fault<ExecuteRequest>> observedFault = await harness.Consumed
                .SelectAsync<Fault<ExecuteRequest>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Guid? failed = await sagaHarness.WaitForSagaInStateAsync(sagaId, machine.Failed, timeout, TestContext.Current.CancellationToken);
            RequestState? state = sagaHarness.Sagas.FindById(sagaId);

            Assert.Equal(harness.InputQueueAddress, request.ResponseAddress);
            Assert.NotNull(request.RequestId);
            Assert.Equal(sagaId, failed);
            Assert.NotNull(state);
            Assert.Equal("key", state.Key);
            Assert.Equal(machine.Failed.Name, state.CurrentState);
            Assert.NotNull(state.RequestId);
            Assert.Equal(request.MessageId, observedFault.Context.Message.FaultedMessageId);
            Assert.Equal(state.RequestId, observedFault.Context.RequestId);
            Assert.NotEqual(Guid.Empty, observedFault.Context.Message.FaultId);
            Assert.NotEmpty(observedFault.Context.Message.FaultMessageTypes);
            Assert.NotEqual(default, observedFault.Context.Message.Timestamp);
            Assert.Equal(TimeSpan.Zero, observedFault.Context.Message.Timestamp.Offset);
            Assert.Equal(request.Message.CorrelationId, observedFault.Context.Message.Message.CorrelationId);
            Assert.Equal(request.Message.Key, observedFault.Context.Message.Message.Key);
            Assert.Contains(observedFault.Context.Message.Exceptions, exception =>
                exception.ExceptionType.EndsWith(nameof(ExpectedRequestException), StringComparison.Ordinal)
                && exception.Message == "request failed");
            Assert.True(await harness.Consumed.AnyAsync<StartRequest>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<Fault<ExecuteRequest>>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE", "direct-request-response-and-state")]
    public async Task DirectStateMachineHarness_RespondsAndRecordsTheExactResultingStateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        var machine = new ResponsiveStateMachine();
        ISagaStateMachineTestHarness<ResponsiveStateMachine, ResponsiveState> sagaHarness =
            harness.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(machine);

        await harness.StartAsync(cancellationToken);
        try
        {
            IRequestClient<ResponsiveRequest> client = harness.CreateRequestClient<ResponsiveRequest>();

            Response<ResponsiveResponse> response = await client.GetResponseAsync<ResponsiveResponse>(
                new ResponsiveRequest(sagaId, "direct"),
                cancellationToken);
            Guid? responded = await sagaHarness.WaitForSagaInStateAsync(sagaId, machine.Responded, timeout, TestContext.Current.CancellationToken);
            ResponsiveState? instance = sagaHarness.Sagas.FindById(sagaId);

            Assert.Equal(sagaId, response.Message.CorrelationId);
            Assert.Equal("response:direct", response.Message.Value);
            Assert.Equal(sagaId, responded);
            Assert.NotNull(instance);
            Assert.Equal("direct", instance.Value);
            Assert.Equal(machine.Responded.Name, instance.CurrentState);
            Assert.True(await sagaHarness.Consumed.AnyAsync<ResponsiveRequest>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE-OBSERVATION", "exact-event-lifecycle-and-transition")]
    public async Task StateMachineObservations_RecordExactEventLifecycleAndTransitionAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        var machine = new ResponsiveStateMachine();
        ISagaStateMachineTestHarness<ResponsiveStateMachine, ResponsiveState> sagaHarness =
            harness.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(machine);

        await harness.StartAsync(cancellationToken);
        try
        {
            IRequestClient<ResponsiveRequest> client = harness.CreateRequestClient<ResponsiveRequest>();
            Response<ResponsiveResponse> response = await client.GetResponseAsync<ResponsiveResponse>(
                new ResponsiveRequest(sagaId, "observed"),
                cancellationToken);
            Assert.Equal(sagaId, response.Message.CorrelationId);
            Assert.Equal(sagaId, await sagaHarness.WaitForSagaInStateAsync(sagaId, machine.Responded, timeout, TestContext.Current.CancellationToken));
            IConsumedMessage<ResponsiveRequest> consumed = await sagaHarness.Consumed
                .SelectAsync<ResponsiveRequest>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(consumed.Exception);

            Assert.Collection(
                sagaHarness.Events.Where(observation => observation.SagaId == sagaId),
                started =>
                {
                    Assert.Equal(machine.Request.Name, started.EventName);
                    Assert.Equal(typeof(ResponsiveRequest), started.DataType);
                    Assert.Equal(StateMachineEventExecutionStatus.Started, started.Status);
                    Assert.Null(started.Exception);
                },
                completed =>
                {
                    Assert.Equal(machine.Request.Name, completed.EventName);
                    Assert.Equal(typeof(ResponsiveRequest), completed.DataType);
                    Assert.Equal(StateMachineEventExecutionStatus.Completed, completed.Status);
                    Assert.Null(completed.Exception);
                });
            Assert.Collection(
                sagaHarness.StateChanges.Where(change => change.SagaId == sagaId),
                initialized =>
                {
                    Assert.Null(initialized.PreviousState);
                    Assert.Equal(machine.Initial.Name, initialized.CurrentState);
                },
                transitioned =>
                {
                    Assert.Equal(machine.Initial.Name, transitioned.PreviousState);
                    Assert.Equal(machine.Responded.Name, transitioned.CurrentState);
                });
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE-OBSERVATION", "typed-event-fault-lifecycle")]
    public async Task StateMachineObservations_RecordTheExactTypedEventFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        var expected = new InvalidOperationException("state-machine event failed");
        using var harness = CreateHarness(timeout);
        var machine = new FaultingStateMachine(expected);
        ISagaStateMachineTestHarness<FaultingStateMachine, FaultingState> sagaHarness =
            harness.AddSagaStateMachine<FaultingStateMachine, FaultingState>(machine);

        await harness.StartAsync(cancellationToken);
        try
        {
            Task<IPublishedMessage<Fault<FaultingRequest>>> faulted = harness.Published
                .SelectAsync<Fault<FaultingRequest>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.InputQueueSendEndpoint.SendAsync(new FaultingRequest(sagaId), cancellationToken);
            IPublishedMessage<Fault<FaultingRequest>> fault = await faulted.WaitAsync(timeout, cancellationToken);
            IConsumedMessage<FaultingRequest> consumed = await sagaHarness.Consumed
                .SelectAsync<FaultingRequest>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Same(expected, consumed.Exception);
            Assert.Equal("state-machine event failed", Assert.Single(fault.Context.Message.Exceptions).Message);
            Assert.Collection(
                sagaHarness.Events.Where(observation => observation.SagaId == sagaId),
                started =>
                {
                    Assert.Equal(machine.Fail.Name, started.EventName);
                    Assert.Equal(typeof(FaultingRequest), started.DataType);
                    Assert.Equal(StateMachineEventExecutionStatus.Started, started.Status);
                    Assert.Null(started.Exception);
                },
                eventFault =>
                {
                    Assert.Equal(machine.Fail.Name, eventFault.EventName);
                    Assert.Equal(typeof(FaultingRequest), eventFault.DataType);
                    Assert.Equal(StateMachineEventExecutionStatus.Faulted, eventFault.Status);
                    Assert.Same(expected, eventFault.Exception);
                });
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-RETENTION", "bounded-saga-event-and-state-histories")]
    public async Task BoundedRetention_AppliesToSagaAndStateMachineHistoriesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid[] sagaIds = [NewId.NextGuid(), NewId.NextGuid(), NewId.NextGuid()];
        using var harness = CreateHarness(timeout);
        harness.ContextSaveMode = TestContextSaveMode.Bounded;
        harness.MaximumSavedContexts = 2;
        var machine = new ResponsiveStateMachine();
        ISagaStateMachineTestHarness<ResponsiveStateMachine, ResponsiveState> sagaHarness =
            harness.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(machine);

        await harness.StartAsync(cancellationToken);
        try
        {
            IRequestClient<ResponsiveRequest> client = harness.CreateRequestClient<ResponsiveRequest>();
            foreach ((Guid sagaId, int index) in sagaIds.Select((id, index) => (id, index)))
            {
                Response<ResponsiveResponse> response = await client.GetResponseAsync<ResponsiveResponse>(
                    new ResponsiveRequest(sagaId, $"bounded-{index}"),
                    cancellationToken);
                Assert.Equal(sagaId, response.Message.CorrelationId);
                Assert.Equal(sagaId, await sagaHarness.WaitForSagaInStateAsync(sagaId, machine.Responded, timeout, TestContext.Current.CancellationToken));
            }

            Assert.Equal(sagaIds[1..], sagaHarness.Consumed.Snapshot()
                .OfType<IConsumedMessage<ResponsiveRequest>>()
                .Select(message => message.Context.Message.CorrelationId));
            Assert.Equal(sagaIds[1..], sagaHarness.Created.Snapshot()
                .Select(instance => instance.Saga.CorrelationId));
            Assert.Equal(sagaIds[1..], sagaHarness.Sagas.Snapshot()
                .Select(instance => instance.Saga.CorrelationId));

            Assert.Collection(
                sagaHarness.Events,
                started =>
                {
                    Assert.Equal(sagaIds[2], started.SagaId);
                    Assert.Equal(StateMachineEventExecutionStatus.Started, started.Status);
                },
                completed =>
                {
                    Assert.Equal(sagaIds[2], completed.SagaId);
                    Assert.Equal(StateMachineEventExecutionStatus.Completed, completed.Status);
                });
            Assert.Collection(
                sagaHarness.StateChanges,
                initialized =>
                {
                    Assert.Equal(sagaIds[2], initialized.SagaId);
                    Assert.Null(initialized.PreviousState);
                    Assert.Equal(machine.Initial.Name, initialized.CurrentState);
                },
                transitioned =>
                {
                    Assert.Equal(sagaIds[2], transitioned.SagaId);
                    Assert.Equal(machine.Initial.Name, transitioned.PreviousState);
                    Assert.Equal(machine.Responded.Name, transitioned.CurrentState);
                });
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE", "query-correlation-existing-and-missing")]
    public async Task QueryCorrelatedStateMachineHarness_RecordsOnlyTheMatchedSagaAndItsResultingStateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        var machine = new QueryCorrelationStateMachine();
        ISagaStateMachineTestHarness<QueryCorrelationStateMachine, QueryCorrelationState> sagaHarness =
            harness.AddSagaStateMachine<QueryCorrelationStateMachine, QueryCorrelationState>(machine);

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new StartQuerySaga(sagaId, "alpha"),
                cancellationToken);
            Guid? running = await sagaHarness.WaitForSagaInStateAsync(sagaId, machine.Running, timeout, TestContext.Current.CancellationToken);
            IRequestClient<CheckQuerySaga> client = harness.CreateRequestClient<CheckQuerySaga>();

            Response<QuerySagaStatus> response = await client.GetResponseAsync<QuerySagaStatus>(
                new CheckQuerySaga("alpha"),
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new CheckQuerySaga("missing"), cancellationToken);
            IPublishedMessage<QuerySagaMissing> missing = await harness.Published
                .SelectAsync<QuerySagaMissing>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            QueryCorrelationState? instance = sagaHarness.Sagas.FindById(sagaId);
            ISagaInstance<QueryCorrelationState>[] created = sagaHarness.Created
                .Snapshot()
                .ToArray();
            IConsumedMessage<CheckQuerySaga>[] matchedQueries = sagaHarness.Consumed
                .Snapshot()
                .OfType<IConsumedMessage<CheckQuerySaga>>()
                .ToArray();

            Assert.Equal(sagaId, running);
            Assert.Equal(new QuerySagaStatus(sagaId, "alpha", 1), response.Message);
            Assert.Equal(new QuerySagaMissing("missing"), missing.Context.Message);
            Assert.Single(created);
            Assert.Equal(sagaId, created[0].Saga.CorrelationId);
            Assert.NotNull(instance);
            Assert.Equal("alpha", instance.Key);
            Assert.Equal(1, instance.CheckCount);
            Assert.Equal(machine.Running.Name, instance.CurrentState);
            Assert.Single(matchedQueries);
            Assert.Equal("alpha", matchedQueries[0].Context.Message.Key);
            Assert.Null(matchedQueries[0].Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA", "explicit-repository-and-named-endpoint")]
    public async Task ClassicSagaHarness_ExplicitRepositoryConsumesFromItsNamedEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        const string queueName = "explicit-classic-saga";
        Guid sagaId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        var repository = new InMemorySagaRepository<ClassicSaga>();
        SagaTestHarness<ClassicSaga> sagaHarness = harness.AddSaga(repository, queueName);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            ISendEndpoint endpoint = await harness.GetSendEndpointAsync(
                new Uri(harness.BaseAddress, queueName),
                TestContext.Current.CancellationToken);
            await endpoint.SendAsync(
                new StartSaga(sagaId, "explicit", [new SagaValue("repository")]),
                TestContext.Current.CancellationToken);

            Assert.Equal(sagaId, await sagaHarness.WaitForSagaAsync(
                sagaId,
                timeout,
                TestContext.Current.CancellationToken));
            Assert.True(await sagaHarness.Sagas.AnyAsync(TestContext.Current.CancellationToken));
            Assert.True(await sagaHarness.Sagas.AnyAsync(
                saga => saga.Value == "explicit",
                TestContext.Current.CancellationToken));
            Assert.Equal(
                sagaId,
                (await sagaHarness.Sagas
                    .SelectAsync(TestContext.Current.CancellationToken)
                    .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Saga.CorrelationId);
            Assert.Equal(
                sagaId,
                (await sagaHarness.Sagas
                    .SelectAsync(saga => saga.Value == "explicit", TestContext.Current.CancellationToken)
                    .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Saga.CorrelationId);
            Assert.Equal(
                sagaId,
                Assert.Single(sagaHarness.Sagas.Snapshot(saga => saga.Value == "explicit")).Saga.CorrelationId);
            ClassicSaga instance = Assert.IsType<ClassicSaga>(sagaHarness.Sagas.FindById(sagaId));
            Assert.Equal("explicit", instance.Value);
            Assert.Equal(["repository"], instance.Values);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE", "explicit-repository-and-named-endpoint")]
    public async Task StateMachineHarness_ExplicitRepositoryConsumesFromItsNamedEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        const string queueName = "explicit-state-machine";
        Guid sagaId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        var machine = new ResponsiveStateMachine();
        var repository = new InMemorySagaRepository<ResponsiveState>();
        using ISagaStateMachineTestHarness<ResponsiveStateMachine, ResponsiveState> sagaHarness =
            harness.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(machine, repository, queueName);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            ISendEndpoint endpoint = await harness.GetSendEndpointAsync(
                new Uri(harness.BaseAddress, queueName),
                TestContext.Current.CancellationToken);
            await endpoint.SendAsync(
                new ResponsiveRequest(sagaId, "explicit"),
                context => context.ResponseAddress = harness.BusAddress,
                TestContext.Current.CancellationToken);

            Assert.Equal(sagaId, await sagaHarness.WaitForSagaInStateAsync(
                sagaId,
                machine.Responded,
                timeout,
                TestContext.Current.CancellationToken));
            Assert.Equal(sagaId, await sagaHarness.WaitForSagaInStateAsync(
                sagaId,
                static stateMachine => stateMachine.Responded,
                timeout,
                TestContext.Current.CancellationToken));
            Assert.Equal(
                [sagaId],
                await sagaHarness.WaitForSagasInStateAsync(
                    state => state.Value == "explicit",
                    static stateMachine => stateMachine.Responded,
                    timeout,
                    TestContext.Current.CancellationToken));
            ResponsiveState instance = Assert.IsType<ResponsiveState>(sagaHarness.Sagas.FindById(sagaId));
            Assert.Equal("explicit", instance.Value);
            Assert.Equal(machine.Responded.Name, instance.CurrentState);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA", "registration-boundaries")]
    public void SagaRegistration_RejectsEveryMissingDependencyAndInvalidQueueName()
    {
        using var harness = CreateHarness(OperationTimeout());
        var machine = new ResponsiveStateMachine();
        var sagaRepository = new InMemorySagaRepository<ClassicSaga>();
        var stateMachineRepository = new InMemorySagaRepository<ResponsiveState>();

        Assert.Equal("harness", Assert.Throws<ArgumentNullException>(() =>
            SagaTestHarnessExtensions.AddSaga<ClassicSaga>(null!)).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            harness.AddSaga((ISagaRepository<ClassicSaga>)null!)).ParamName);
        Assert.Equal("queueName", Assert.Throws<ArgumentException>(() =>
            harness.AddSaga(sagaRepository, " ")).ParamName);
        Assert.Equal("harness", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineTestHarnessExtensions.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(
                null!,
                machine)).ParamName);
        Assert.Equal("stateMachine", Assert.Throws<ArgumentNullException>(() =>
            harness.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(null!)).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            harness.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(
                machine,
                (ISagaRepository<ResponsiveState>)null!)).ParamName);
        Assert.Equal("queueName", Assert.Throws<ArgumentException>(() =>
            harness.AddSagaStateMachine<ResponsiveStateMachine, ResponsiveState>(machine, stateMachineRepository, " ")).ParamName);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"testing-saga-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    public sealed record SagaValue(string Value);

    public sealed record StartSaga(Guid CorrelationId, string Value, IReadOnlyList<SagaValue> Values) : CorrelatedBy<Guid>;

    public sealed record SagaStarted(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed class ClassicSaga :
        ISaga,
        InitiatedBy<StartSaga>
    {
        public ClassicSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public string Value { get; private set; } = string.Empty;

        public IReadOnlyList<string> Values { get; private set; } = [];

        public async Task ConsumeAsync(ConsumeContext<StartSaga> context)
        {
            Value = context.Message.Value;
            Values = context.Message.Values.Select(value => value.Value).ToArray();
            await context.Advanced().PublishAsync(new SagaStarted(CorrelationId, Value));
        }
    }

    public sealed record StartRequest(Guid CorrelationId, string Key) : CorrelatedBy<Guid>;

    public sealed class ExecuteRequest : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }

        public string Key { get; set; } = string.Empty;
    }

    public sealed class ExecuteResponse : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class RequestState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public Guid? RequestId { get; set; }
    }

    public sealed class RequestStateMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public RequestStateMachine(Uri executeAddress)
        {
            ArgumentNullException.ThrowIfNull(executeAddress);
            InstanceState(instance => instance.CurrentState);

            Event(() => Start, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });

            Request(() => Execute, instance => instance.RequestId, configuration =>
            {
                configuration.ServiceAddress = executeAddress;
                configuration.Timeout = TimeSpan.Zero;
            });

            Initially(
                When(Start)
                    .Then(context => context.Saga.Key = context.Message.Key)
                    .Request(Execute, context => context.InitAsync<ExecuteRequest>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.Key,
                    }))
                    .TransitionTo(Execute.Pending));

            During(
                Execute.Pending,
                When(Execute.Faulted)
                    .TransitionTo(Failed));
        }

        public State Failed { get; } = null!;

        public Event<StartRequest> Start { get; } = null!;

        public Request<RequestState, ExecuteRequest, ExecuteResponse> Execute { get; } = null!;
    }

    public sealed record ResponsiveRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record ResponsiveResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record FaultingRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class FaultingState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class FaultingStateMachine : ViciOneServiceBusStateMachine<FaultingState>
    {
        public FaultingStateMachine(Exception failure)
        {
            ArgumentNullException.ThrowIfNull(failure);
            InstanceState(instance => instance.CurrentState);
            Event(() => Fail, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });

            Initially(When(Fail).Then(_ => throw failure));
        }

        public Event<FaultingRequest> Fail { get; } = null!;
    }

    public sealed class ResponsiveState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    public sealed class ResponsiveStateMachine : ViciOneServiceBusStateMachine<ResponsiveState>
    {
        public ResponsiveStateMachine()
        {
            InstanceState(instance => instance.CurrentState);

            Event(() => Request, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });

            Initially(
                When(Request)
                    .Then(context => context.Saga.Value = context.Message.Value)
                    .RespondAwaited(context => Task.FromResult(new ResponsiveResponse(
                        context.Saga.CorrelationId,
                        $"response:{context.Saga.Value}")))
                    .TransitionTo(Responded));
        }

        public State Responded { get; } = null!;

        public Event<ResponsiveRequest> Request { get; } = null!;
    }

    public sealed record StartQuerySaga(Guid CorrelationId, string Key);

    public sealed record CheckQuerySaga(string Key);

    public sealed record QuerySagaStatus(Guid CorrelationId, string Key, int CheckCount);

    public sealed record QuerySagaMissing(string Key);

    public sealed class QueryCorrelationState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public int CheckCount { get; set; }
    }

    public sealed class QueryCorrelationStateMachine : ViciOneServiceBusStateMachine<QueryCorrelationState>
    {
        public QueryCorrelationStateMachine()
        {
            InstanceState(instance => instance.CurrentState);

            Event(() => Start, configuration => configuration
                .CorrelateBy(instance => instance.Key, context => context.Message.Key)
                .SelectId(context => context.Message.CorrelationId));
            Event(() => Check, configuration => configuration
                .CorrelateBy(instance => instance.Key, context => context.Message.Key)
                .OnMissingInstance(missing => missing.ExecuteAwaited(context =>
                    context.Advanced().PublishAsync(new QuerySagaMissing(context.Message.Key), context.CancellationToken))));

            Initially(
                When(Start)
                    .Then(context => context.Saga.Key = context.Message.Key)
                    .TransitionTo(Running));

            During(
                Running,
                When(Check)
                    .Then(context => context.Saga.CheckCount++)
                    .RespondAwaited(context => Task.FromResult(new QuerySagaStatus(
                        context.Saga.CorrelationId,
                        context.Saga.Key,
                        context.Saga.CheckCount))));
        }

        public State Running { get; } = null!;

        public Event<StartQuerySaga> Start { get; } = null!;

        public Event<CheckQuerySaga> Check { get; } = null!;
    }

    private sealed class ExpectedRequestException(string message) : Exception(message);
}
