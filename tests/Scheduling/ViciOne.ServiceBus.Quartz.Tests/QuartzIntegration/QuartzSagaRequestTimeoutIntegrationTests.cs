using Quartz;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzSagaRequestTimeoutIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "response-cancels-timeout-and-clears-request-id")]
    public async Task SagaRequest_ResponseCancelsItsQuartzTimeoutAndClearsTheRequestIdentityAsync()
    {
        await using RequestFixture fixture = await RequestFixture.StartAsync(respond: true);

        await fixture.SendStartAsync();
        RequestOutcome outcome = await fixture.Outcome.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        ScheduleMessage scheduled = await fixture.Scheduled.Message.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        await fixture.Canceled.Completed.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        RequestSagaState saga = Assert.IsType<SagaInstance<RequestSagaState>>(fixture.Repository[fixture.CorrelationId]).Instance;

        Assert.Equal("Completed", outcome.Result);
        Assert.Equal(fixture.CorrelationId, outcome.CorrelationId);
        Assert.Null(saga.ValidationRequestId);
        Assert.Equal(fixture.StateMachine.Completed.Name, saga.CurrentState.Name);
        Assert.False(await fixture.Scheduler.Exists(
            new TriggerKey(scheduled.TokenId.ToString("N")),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "quartz-timeout-is-delivered-to-the-pending-saga")]
    public async Task SagaRequest_QuartzTimeoutIsDeliveredToThePendingSagaAsync()
    {
        await using RequestFixture fixture = await RequestFixture.StartAsync(respond: false);

        await fixture.SendStartAsync();
        ScheduleMessage scheduled = await fixture.Scheduled.Message.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(scheduled.TokenId.ToString("N")),
            TestContext.Current.CancellationToken));

        await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, TestContext.Current.CancellationToken);
        RequestOutcome outcome = await fixture.Outcome.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        RequestSagaState saga = Assert.IsType<SagaInstance<RequestSagaState>>(fixture.Repository[fixture.CorrelationId]).Instance;

        Assert.Equal("TimedOut", outcome.Result);
        Assert.Equal(fixture.CorrelationId, outcome.CorrelationId);
        Assert.Equal(fixture.StateMachine.TimedOut.Name, saga.CurrentState.Name);
    }

    private sealed class RequestFixture : IAsyncDisposable
    {
        private readonly QuartzTestBus _fixture;
        private readonly ConnectHandle _scheduledHandle;
        private readonly ConnectHandle _canceledHandle;
        private readonly Uri _inputAddress;

        private RequestFixture(
            QuartzTestBus fixture,
            Uri inputAddress,
            TimeSpan timeout,
            Guid correlationId,
            RequestSagaStateMachine stateMachine,
            InMemorySagaRepository<RequestSagaState> repository,
            Task<RequestOutcome> outcome,
            ScheduleMessageCapture scheduled,
            ConsumeCompletionObserver<CancelScheduledMessage> canceled,
            ConnectHandle scheduledHandle,
            ConnectHandle canceledHandle)
        {
            _fixture = fixture;
            _inputAddress = inputAddress;
            Timeout = timeout;
            CorrelationId = correlationId;
            StateMachine = stateMachine;
            Repository = repository;
            Outcome = outcome;
            Scheduled = scheduled;
            Canceled = canceled;
            _scheduledHandle = scheduledHandle;
            _canceledHandle = canceledHandle;
        }

        public TimeSpan Timeout { get; }
        public Guid CorrelationId { get; }
        public RequestSagaStateMachine StateMachine { get; }
        public InMemorySagaRepository<RequestSagaState> Repository { get; }
        public Task<RequestOutcome> Outcome { get; }
        public ScheduleMessageCapture Scheduled { get; }
        public ConsumeCompletionObserver<CancelScheduledMessage> Canceled { get; }
        public IScheduler Scheduler => _fixture.Scheduler;

        public static async Task<RequestFixture> StartAsync(bool respond)
        {
            TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            Guid correlationId = NewId.NextGuid();
            string prefix = $"quartz-request-timeout-{NewId.NextGuid():N}";
            var inputAddress = new Uri($"loopback://localhost/{prefix}-saga");
            var serviceAddress = new Uri($"loopback://localhost/{prefix}-service");
            var repository = new InMemorySagaRepository<RequestSagaState>();
            var stateMachine = new RequestSagaStateMachine(serviceAddress);
            var outcome = new TaskCompletionSource<RequestOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
            QuartzTestBus fixture = await QuartzTestBus.StartAsync(
                timeout,
                configure: configurator =>
                {
                    configurator.ReceiveEndpoint($"{prefix}-saga", endpoint =>
                    {
                        endpoint.UseInMemoryOutbox();
                        endpoint.StateMachineSaga(stateMachine, repository);
                    });
                    configurator.ReceiveEndpoint($"{prefix}-service", endpoint => endpoint.Handler<ValidationRequest>(context =>
                    {
                        if (!respond)
                            return Task.CompletedTask;

                        return context.RespondAsync(new ValidationResponse(context.Message.CorrelationId));
                    }));
                    configurator.ReceiveEndpoint($"{prefix}-events", endpoint => endpoint.Handler<RequestOutcome>(context =>
                    {
                        outcome.TrySetResult(context.Message);
                        return Task.CompletedTask;
                    }));
                });
            var scheduled = new ScheduleMessageCapture();
            var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(_ => true);
            ConnectHandle scheduledHandle = fixture.Bus.ConnectConsumeObserver(scheduled);
            ConnectHandle canceledHandle = fixture.Bus.ConnectConsumeObserver(canceled);

            return new RequestFixture(
                fixture,
                inputAddress,
                timeout,
                correlationId,
                stateMachine,
                repository,
                outcome.Task,
                scheduled,
                canceled,
                scheduledHandle,
                canceledHandle);
        }

        public async Task SendStartAsync()
        {
            ISendEndpoint input = await _fixture.Bus.GetSendEndpointAsync(_inputAddress)
                .WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await input.SendAsync(new StartRequest(CorrelationId), TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            _scheduledHandle.Dispose();
            _canceledHandle.Dispose();
            await _fixture.DisposeAsync();
        }
    }

    public sealed class ScheduleMessageCapture : IConsumeObserver
    {
        private readonly TaskCompletionSource<ScheduleMessage> _message =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ScheduleMessage> Message => _message.Task;

        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class
        {
            if (context.Message is ScheduleMessage message)
                _message.TrySetResult(message);

            return Task.CompletedTask;
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            if (context.Message is ScheduleMessage)
                _message.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    public sealed class RequestSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public State CurrentState { get; set; } = null!;
        public Guid? ValidationRequestId { get; set; }
    }

    public sealed class RequestSagaStateMachine : ViciOneServiceBusStateMachine<RequestSagaState>
    {
        public RequestSagaStateMachine(Uri serviceAddress)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Started, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Request(() => Validation, instance => instance.ValidationRequestId, configuration =>
                configuration.Timeout = TimeSpan.FromMinutes(1));

            Initially(
                When(Started)
                    .Request(Validation, _ => serviceAddress, context => new ValidationRequest(context.Saga.CorrelationId))
                    .TransitionTo(Validation.Pending));
            During(Validation.Pending,
                When(Validation.Completed)
                    .Publish(context => new RequestOutcome(context.Saga.CorrelationId, "Completed"))
                    .TransitionTo(Completed),
                When(Validation.TimeoutExpired)
                    .Publish(context => new RequestOutcome(context.Saga.CorrelationId, "TimedOut"))
                    .TransitionTo(TimedOut));
        }

        public State Completed { get; private set; } = null!;
        public State TimedOut { get; private set; } = null!;
        public Event<StartRequest> Started { get; private set; } = null!;
        public Request<RequestSagaState, ValidationRequest, ValidationResponse> Validation { get; private set; } = null!;
    }

    public sealed record StartRequest(Guid CorrelationId);
    public sealed record ValidationRequest(Guid CorrelationId);
    public sealed record ValidationResponse(Guid CorrelationId);
    public sealed record RequestOutcome(Guid CorrelationId, string Result);
}
