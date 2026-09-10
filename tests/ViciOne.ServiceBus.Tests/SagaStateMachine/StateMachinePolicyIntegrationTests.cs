using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachinePolicyIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "saga-message-and-saga-message-pipe-scopes")]
    public async Task SagaConfiguration_InvokesAllThreeScopesOnceWithTheSameSagaAndMessageAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("configuration-scopes", timeout);
        var machine = new ConfigurationScopeMachine();
        var repository = new InMemorySagaRepository<ConfigurationScopeState>();
        var recorder = new ConfigurationScopeRecorder();
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.StateMachineSaga(
            machine,
            repository,
            saga =>
            {
                saga.UseExecute(context => recorder.RecordSaga(context.Saga));
                saga.Message<ConfigurationScopeStart>(message =>
                    message.UseExecute(context => recorder.RecordMessage(context.Message)));
                saga.SagaMessage<ConfigurationScopeStart>(message =>
                    message.UseExecute(context => recorder.RecordPair(context.Saga, context.Message)));
            });

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var start = new ConfigurationScopeStart(NewId.NextGuid());
            await harness.InputQueueSendEndpoint.SendAsync(start, cancellationToken);
            Assert.Equal(
                start.CorrelationId,
                await repository.ShouldContainSagaInStateAsync(start.CorrelationId, machine, machine.Running, timeout, cancellationToken: TestContext.Current.CancellationToken));

            Assert.Equal(1, recorder.SagaCount);
            Assert.Equal(1, recorder.MessageCount);
            Assert.Equal(1, recorder.PairCount);
            Assert.Same(recorder.Saga, recorder.PairSaga);
            Assert.Equal(start, recorder.Message);
            Assert.Equal(start, recorder.PairMessage);
            Assert.Equal(start.CorrelationId, recorder.Saga!.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Select<ConfigurationScopeStart>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-MISSING", "retry-before-create-then-normal-order-matrix")]
    public async Task MissingStatus_RetriesAfterTheFirstAttemptAndSucceedsOnceTheInstanceExistsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("missing-retry", timeout);
        var machine = new RetryStatusMachine();
        var repository = new InMemorySagaRepository<RetryStatusState>();
        var retryObserver = new GatedRetryObserver();
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.ConcurrentMessageLimit = 2;
            endpoint.UseMessageRetry(retry =>
            {
                retry.Immediate(5);
                retry.ConnectRetryObserver(retryObserver);
            });
            endpoint.StateMachineSaga(machine, repository);
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<RetryStatusRequest> client = harness.CreateRequestClient<RetryStatusRequest>();
            Guid delayedId = NewId.NextGuid();
            Task<Response<RetryStatusResponse>> delayed = client.GetResponseAsync<RetryStatusResponse>(
                new RetryStatusRequest("delayed"),
                cancellationToken);

            await retryObserver.FirstRetry.Task.WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new RetryStatusStart("delayed", delayedId), cancellationToken);
            Assert.Equal(delayedId, await repository.ShouldContainSagaInStateAsync(delayedId, machine, machine.Running, timeout, cancellationToken: TestContext.Current.CancellationToken));
            retryObserver.Release.TrySetResult();

            Response<RetryStatusResponse> delayedResponse = await delayed.WaitAsync(timeout, cancellationToken);
            Assert.Equal(new RetryStatusResponse(delayedId, "delayed", "Running"), delayedResponse.Message);
            Assert.Equal(1, retryObserver.RetryCount);

            foreach (string serviceName in new[] { "normal-a", "normal-b" })
            {
                Guid id = NewId.NextGuid();
                await harness.InputQueueSendEndpoint.SendAsync(new RetryStatusStart(serviceName, id), cancellationToken);
                Assert.Equal(id, await repository.ShouldContainSagaInStateAsync(id, machine, machine.Running, timeout, cancellationToken: TestContext.Current.CancellationToken));
                Response<RetryStatusResponse> response = await client.GetResponseAsync<RetryStatusResponse>(
                    new RetryStatusRequest(serviceName),
                    cancellationToken);
                Assert.Equal(new RetryStatusResponse(id, serviceName, "Running"), response.Message);
            }

            Assert.Equal(1, retryObserver.RetryCount);
        }
        finally
        {
            retryObserver.Release.TrySetResult();
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(3, harness.Sent.Select<RetryStatusResponse>(SnapshotOnlyToken()).Count());
        Assert.Empty(harness.Sent.Select<Fault<RetryStatusRequest>>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-IGNORE", "repeated-event-is-consumed-without-fault-or-state-change")]
    public async Task IgnoredRepeatedEvent_IsConsumedWithoutFaultAndLeavesTheInstanceUnchangedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("ignore-repeat", timeout);
        var machine = new IgnoreMachine();
        ISagaStateMachineTestHarness<IgnoreMachine, IgnoreState> sagaHarness =
            harness.StateMachineSaga<IgnoreState, IgnoreMachine>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            await harness.Bus.PublishAsync(new IgnoreStart(correlationId), cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.ExistsAsync(correlationId, machine.Running, timeout, TestContext.Current.CancellationToken));

            Task<IReceivedMessage<IgnoreStart>> second = sagaHarness.Consumed
                .SelectAsync<IgnoreStart>(cancellationToken)
                .Skip(1)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.Bus.PublishAsync(new IgnoreStart(correlationId), cancellationToken);
            Assert.Null((await second.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Equal(correlationId, await sagaHarness.ExistsAsync(correlationId, machine.Running, timeout, TestContext.Current.CancellationToken));

            IgnoreState? instance = sagaHarness.Sagas.Contains(correlationId);
            Assert.NotNull(instance);
            Assert.Equal(1, instance.StartCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(2, sagaHarness.Consumed.Select<IgnoreStart>(SnapshotOnlyToken()).Count());
        Assert.Empty(harness.Published.Select<Fault<IgnoreStart>>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RETRY", "ignored-exception-produces-one-attempt-and-one-fault")]
    public async Task RetryIgnoredException_ExecutesOncePublishesOneFaultAndKeepsThePriorStateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("retry-ignore", timeout);
        var attempts = new AttemptRecorder();
        var machine = new RetryIgnoreMachine(attempts);
        var retryObserver = new CountingRetryObserver();
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.UseMessageRetry(retry =>
        {
            retry.Ignore<ExpectedIgnoredFailure>();
            retry.Immediate(2);
            retry.ConnectRetryObserver(retryObserver);
        });
        ISagaStateMachineTestHarness<RetryIgnoreMachine, RetryIgnoreState> sagaHarness =
            harness.StateMachineSaga<RetryIgnoreState, RetryIgnoreMachine>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            await harness.InputQueueSendEndpoint.SendAsync(new RetryInitialize(correlationId), cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.ExistsAsync(correlationId, machine.Waiting, timeout, TestContext.Current.CancellationToken));
            Task<IPublishedMessage<Fault<RetryIgnoredStart>>> fault = harness.Published
                .SelectAsync<Fault<RetryIgnoredStart>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.InputQueueSendEndpoint.SendAsync(new RetryIgnoredStart(correlationId), cancellationToken);
            Fault<RetryIgnoredStart> published = (await fault.WaitAsync(timeout, cancellationToken)).Context.Message;

            Assert.Equal(correlationId, published.Message.CorrelationId);
            Assert.Equal(1, attempts.Count(correlationId));
            Assert.Equal(0, retryObserver.RetryCount);
            Assert.Equal(correlationId, await sagaHarness.ExistsAsync(correlationId, machine.Waiting, timeout, TestContext.Current.CancellationToken));
            ExceptionInfo exception = Assert.Single(published.Exceptions);
            Assert.Equal(TypeCache<ExpectedIgnoredFailure>.ShortName, exception.ExceptionType);
            Assert.Null(exception.InnerException);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Select<Fault<RetryIgnoredStart>>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CATCH", "container-activities-catch-finalize-and-remove")]
    public async Task ContainerActivities_ExecuteTheFailureAndCatchStagesThenFinalizeAndRemoveAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new ContainerActivityRecorder();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(recorder)
            .AddScoped<ContainerFailureActivity>()
            .AddScoped<ContainerCatchActivity>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<ContainerCatchMachine, ContainerCatchState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        ISagaStateMachineTestHarness<ContainerCatchMachine, ContainerCatchState> sagaHarness =
            harness.GetSagaStateMachineHarness<ContainerCatchMachine, ContainerCatchState>();
        ILoadSagaRepository<ContainerCatchState> repository = provider.GetRequiredService<ILoadSagaRepository<ContainerCatchState>>();

        try
        {
            Guid correlationId = NewId.NextGuid();
            Task<IReceivedMessage<ContainerCatchStart>> consumed = sagaHarness.Consumed
                .SelectAsync<ContainerCatchStart>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.Bus.PublishAsync(new ContainerCatchStart(correlationId), cancellationToken);

            Assert.Null((await consumed.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null(await repository.LoadAsync(correlationId, TestContext.Current.CancellationToken));
            Assert.Equal(1, recorder.FailureCount);
            Assert.Equal(1, recorder.CatchCount);
            Assert.Equal(correlationId, recorder.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(sagaHarness.Consumed.Select<ContainerCatchStart>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<ContainerCatchStart>>(SnapshotOnlyToken()));
    }

    private static InMemoryTestHarness CreateHarness(string prefix, TimeSpan timeout) =>
        new($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record ConfigurationScopeStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class ConfigurationScopeState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ConfigurationScopeMachine : ViciOneServiceBusStateMachine<ConfigurationScopeState>
    {
        public ConfigurationScopeMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Running));
        }

        public State Running { get; } = null!;

        public Event<ConfigurationScopeStart> Start { get; } = null!;
    }

    public sealed class ConfigurationScopeRecorder
    {
        public int SagaCount { get; private set; }

        public int MessageCount { get; private set; }

        public int PairCount { get; private set; }

        public ConfigurationScopeState? Saga { get; private set; }

        public ConfigurationScopeState? PairSaga { get; private set; }

        public ConfigurationScopeStart? Message { get; private set; }

        public ConfigurationScopeStart? PairMessage { get; private set; }

        public void RecordSaga(ConfigurationScopeState saga)
        {
            SagaCount++;
            Saga = saga;
        }

        public void RecordMessage(ConfigurationScopeStart message)
        {
            MessageCount++;
            Message = message;
        }

        public void RecordPair(ConfigurationScopeState saga, ConfigurationScopeStart message)
        {
            PairCount++;
            PairSaga = saga;
            PairMessage = message;
        }
    }

    public sealed record RetryStatusStart(string ServiceName, Guid ServiceId);

    public sealed record RetryStatusRequest(string ServiceName);

    public sealed record RetryStatusResponse(Guid ServiceId, string ServiceName, string Status);

    public sealed class RetryStatusState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string ServiceName { get; set; } = string.Empty;
    }

    public sealed class RetryStatusMachine : ViciOneServiceBusStateMachine<RetryStatusState>
    {
        public RetryStatusMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Start, configuration => configuration
                .CorrelateBy(instance => instance.ServiceName, context => context.Message.ServiceName)
                .SelectId(context => context.Message.ServiceId));
            Event(() => Status, configuration => configuration
                .CorrelateBy(instance => instance.ServiceName, context => context.Message.ServiceName)
                .OnMissingInstance(missing => missing.Fault()));
            Initially(
                When(Start)
                    .Then(context => context.Saga.ServiceName = context.Message.ServiceName)
                    .TransitionTo(Running));
            During(
                Running,
                When(Status).Respond(context => new RetryStatusResponse(
                    context.Saga.CorrelationId,
                    context.Saga.ServiceName,
                    "Running")));
        }

        public State Running { get; } = null!;

        public Event<RetryStatusStart> Start { get; } = null!;

        public Event<RetryStatusRequest> Status { get; } = null!;
    }

    public sealed class GatedRetryObserver : IRetryObserver
    {
        private int _retryCount;

        public TaskCompletionSource FirstRetry { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RetryCount => Volatile.Read(ref _retryCount);

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context) where T : class, PipeContext => Task.CompletedTask;

        public Task PostFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;

        public async Task PreRetryAsync<T>(RetryContext<T> context) where T : class, PipeContext
        {
            int count = Interlocked.Increment(ref _retryCount);
            if (count == 1)
            {
                FirstRetry.TrySetResult();
                await Release.Task;
            }
        }

        public Task RetryFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;

        public Task RetryCompleteAsync<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;
    }

    public sealed record IgnoreStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class IgnoreState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int StartCount { get; set; }
    }

    public sealed class IgnoreMachine : ViciOneServiceBusStateMachine<IgnoreState>
    {
        public IgnoreMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Start)
                    .Then(context => context.Saga.StartCount++)
                    .TransitionTo(Running));
            During(Running, Ignore(Start));
        }

        public State Running { get; } = null!;

        public Event<IgnoreStart> Start { get; } = null!;
    }

    public sealed record RetryInitialize(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record RetryIgnoredStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class RetryIgnoreState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class RetryIgnoreMachine : ViciOneServiceBusStateMachine<RetryIgnoreState>
    {
        public RetryIgnoreMachine(AttemptRecorder attempts)
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Initialize).TransitionTo(Waiting));
            During(
                Waiting,
                When(Start).Then(context =>
                {
                    attempts.Record(context.Message.CorrelationId);
                    throw new ExpectedIgnoredFailure();
                }));
        }

        public State Waiting { get; } = null!;

        public Event<RetryInitialize> Initialize { get; } = null!;

        public Event<RetryIgnoredStart> Start { get; } = null!;
    }

    public sealed class AttemptRecorder
    {
        private readonly ConcurrentDictionary<Guid, int> _attempts = new();

        public void Record(Guid id) => _attempts.AddOrUpdate(id, 1, (_, count) => count + 1);

        public int Count(Guid id) => _attempts.GetValueOrDefault(id);
    }

    public sealed class CountingRetryObserver : IRetryObserver
    {
        private int _retryCount;

        public int RetryCount => Volatile.Read(ref _retryCount);

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context) where T : class, PipeContext => Task.CompletedTask;

        public Task PostFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;

        public Task PreRetryAsync<T>(RetryContext<T> context) where T : class, PipeContext
        {
            Interlocked.Increment(ref _retryCount);
            return Task.CompletedTask;
        }

        public Task RetryFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;

        public Task RetryCompleteAsync<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;
    }

    public sealed class ExpectedIgnoredFailure : Exception;

    public sealed record ContainerCatchStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class ContainerCatchState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ContainerCatchMachine : ViciOneServiceBusStateMachine<ContainerCatchState>
    {
        public ContainerCatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Start)
                    .Activity(activity => activity.OfType<ContainerFailureActivity>())
                    .TransitionTo(Active)
                    .Catch<ExpectedContainerFailure>(caught => caught
                        .Activity(activity => activity.OfType<ContainerCatchActivity>())
                        .Finalize()));
            SetCompletedWhenFinalized();
        }

        public State Active { get; } = null!;

        public Event<ContainerCatchStart> Start { get; } = null!;
    }

    public sealed class ContainerActivityRecorder
    {
        public int FailureCount { get; private set; }

        public int CatchCount { get; private set; }

        public Guid CorrelationId { get; private set; }

        public void RecordFailure(Guid correlationId)
        {
            FailureCount++;
            CorrelationId = correlationId;
        }

        public void RecordCatch(Guid correlationId)
        {
            CatchCount++;
            CorrelationId = correlationId;
        }
    }

    public sealed class ContainerFailureActivity(ContainerActivityRecorder recorder) :
        IStateMachineActivity<ContainerCatchState, ContainerCatchStart>
    {
        public void Probe(ProbeContext context) => context.CreateScope("containerFailure");

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public Task ExecuteAsync(
            BehaviorContext<ContainerCatchState, ContainerCatchStart> context,
            IBehavior<ContainerCatchState, ContainerCatchStart> next)
        {
            recorder.RecordFailure(context.Saga.CorrelationId);
            throw new ExpectedContainerFailure();
        }

        public Task FaultedAsync<TException>(
            BehaviorExceptionContext<ContainerCatchState, ContainerCatchStart, TException> context,
            IBehavior<ContainerCatchState, ContainerCatchStart> next)
            where TException : Exception => next.FaultedAsync(context);
    }

    public sealed class ContainerCatchActivity(ContainerActivityRecorder recorder) :
        IStateMachineActivity<ContainerCatchState, ContainerCatchStart>
    {
        public void Probe(ProbeContext context) => context.CreateScope("containerCatch");

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public Task ExecuteAsync(
            BehaviorContext<ContainerCatchState, ContainerCatchStart> context,
            IBehavior<ContainerCatchState, ContainerCatchStart> next) => next.ExecuteAsync(context);

        public Task FaultedAsync<TException>(
            BehaviorExceptionContext<ContainerCatchState, ContainerCatchStart, TException> context,
            IBehavior<ContainerCatchState, ContainerCatchStart> next)
            where TException : Exception
        {
            recorder.RecordCatch(context.Saga.CorrelationId);
            return next.FaultedAsync(context);
        }
    }

    public sealed class ExpectedContainerFailure : Exception;
}
