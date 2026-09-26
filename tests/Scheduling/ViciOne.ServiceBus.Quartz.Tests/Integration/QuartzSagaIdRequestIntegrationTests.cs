using System.Collections.Concurrent;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzSagaIdRequestIntegrationTests
{
    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("third")]
    [InlineData("fault")]
    [InlineData("timeout")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "saga-id-three-responses-fault-and-real-timeout")]
    public async Task SagaIdRequest_RoutesToTheHeaderOwnerAndCancelsOrDeliversItsRealTimeoutAsync(string kind)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid activeId = NewId.NextGuid();
        Guid controlId = NewId.NextGuid();
        string prefix = $"quartz-saga-id-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{prefix}-saga");
        var serviceAddress = new Uri($"loopback://localhost/{prefix}-service");
        var repository = new InMemorySagaRepository<RequestState>();
        var machine = new RequestMachine(serviceAddress);
        var requestSeen = new TaskCompletionSource<RequestEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseService = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcome = new TaskCompletionSource<Outcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcomes = new ConcurrentQueue<Outcome>();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout, configure: bus =>
        {
            bus.ReceiveEndpoint($"{prefix}-saga", endpoint =>
            {
                endpoint.UseVolatileOutbox();
                endpoint.StateMachineSaga(machine, repository);
            });
            bus.ReceiveEndpoint($"{prefix}-service", endpoint => endpoint.Handler<Validate>(async context =>
            {
                requestSeen.TrySetResult(new RequestEnvelope(context.RequestId, context.Message.CorrelationId,
                    context.ResponseAddress, context.Headers.Get<object>(MessageHeaders.Request.Accept)));
                await releaseService.Task.WaitAsync(timeout, context.CancellationToken);
                switch (kind)
                {
                    case "first": await context.RespondAsync(new First(controlId, "first")); break;
                    case "second": await context.RespondAsync(new Second(controlId, "second")); break;
                    case "third": await context.RespondAsync(new Third(controlId, "third")); break;
                    case "fault": throw new ExpectedServiceFailure("validation rejected");
                    case "timeout": break;
                    default: throw new ArgumentOutOfRangeException(nameof(kind));
                }
            }));
            bus.ReceiveEndpoint($"{prefix}-results", endpoint => endpoint.Handler<Outcome>(context =>
            {
                outcomes.Enqueue(context.Message);
                outcome.TrySetResult(context.Message);
                return Task.CompletedTask;
            }));
        });
        var scheduled = new QuartzSagaRequestTimeoutIntegrationTests.ScheduleMessageCapture();
        var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(_ => true);
        var primed = new ConsumeCompletionObserver<Prime>(_ => true);
        var started = new ConsumeCompletionObserver<Begin>(_ => true);
        var response = new ResponseObserver();
        using ConnectHandle scheduledHandle = fixture.Bus.ConnectConsumeObserver(scheduled);
        using ConnectHandle canceledHandle = fixture.Bus.ConnectConsumeObserver(canceled);
        using ConnectHandle primedHandle = fixture.Bus.ConnectConsumeObserver(primed);
        using ConnectHandle startedHandle = fixture.Bus.ConnectConsumeObserver(started);
        using ConnectHandle responseHandle = fixture.Bus.ConnectConsumeObserver(response);
        try
        {
            ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(inputAddress, cancellationToken: cancellationToken).WaitAsync(timeout, cancellationToken);
            await input.SendAsync(new Prime(controlId), cancellationToken);
            await primed.Completed.WaitAsync(timeout, cancellationToken);
            Assert.Equal(machine.Validation.Pending, State(repository, controlId).CurrentState);
            await input.SendAsync(new Begin(activeId, controlId), cancellationToken);
            await started.Completed.WaitAsync(timeout, cancellationToken);
            RequestEnvelope request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);
            ScheduleMessage schedule = await scheduled.Message.WaitAsync(timeout, cancellationToken);
            TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(schedule.TokenId, fixture.SchedulerNamespace);
            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(triggerKey, cancellationToken));

            Assert.Equal(activeId, request.RequestId);
            Assert.Equal(controlId, request.BodyId);
            Assert.Equal(inputAddress, request.ResponseAddress);
            Assert.Equal(new[] { MessageUrn.ForTypeString<First>(), MessageUrn.ForTypeString<Second>(), MessageUrn.ForTypeString<Third>() },
                Assert.IsAssignableFrom<IEnumerable<object>>(request.Accept).Select(item => Assert.IsType<string>(item)));
            Assert.Equal(activeId, schedule.TokenId);
            Assert.Equal(machine.Validation.Pending, State(repository, activeId).CurrentState);
            Assert.False(outcome.Task.IsCompleted);

            releaseService.SetResult();
            if (kind == "timeout")
                await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, cancellationToken);
            else
                await canceled.Completed.WaitAsync(timeout, cancellationToken);

            Outcome result = await outcome.Task.WaitAsync(timeout, cancellationToken);
            await response.Completed.WaitAsync(timeout, cancellationToken);
            string detail = kind switch { "fault" => "validation rejected", "timeout" => "expired", _ => kind };
            Assert.Equal(new Outcome(activeId, kind, controlId, detail), result);
            RequestState active = State(repository, activeId);
            Assert.Equal(machine.Finished, active.CurrentState);
            Assert.Equal(1, active.Count);
            Assert.Equal(kind == "fault" ? typeof(ExpectedServiceFailure).FullName : string.Empty, active.ErrorType);
            if (kind != "timeout")
            {
                Assert.Equal(1, canceled.ObservedCount);
                Assert.False(await fixture.Scheduler.Exists(triggerKey, cancellationToken));
            }
        }
        finally
        {
            releaseService.TrySetResult();
            await fixture.Bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        RequestState control = State(repository, controlId);
        Assert.Equal(machine.Validation.Pending, control.CurrentState);
        Assert.Equal(0, control.Count);
        Assert.Equal(string.Empty, control.Kind);
        Assert.Equal(Guid.Empty, control.BodyId);
        Assert.Single(outcomes);
        Assert.Equal(1, response.Count);
    }

    private static RequestState State(InMemorySagaRepository<RequestState> repository, Guid id) =>
        Assert.IsType<SagaInstance<RequestState>>(repository[id]).Instance;

    private sealed record RequestEnvelope(Guid? RequestId, Guid BodyId, Uri? ResponseAddress, object? Accept);
    public sealed record Prime(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record Begin(Guid CorrelationId, Guid BodyId) : ICorrelatedBy<Guid>;
    public sealed record Validate(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record First(Guid CorrelationId, string Reason) : ICorrelatedBy<Guid>;
    public sealed record Second(Guid CorrelationId, string Reason) : ICorrelatedBy<Guid>;
    public sealed record Third(Guid CorrelationId, string Reason) : ICorrelatedBy<Guid>;
    public sealed record Outcome(Guid CorrelationId, string Kind, Guid BodyId, string Detail);
    public sealed class ExpectedServiceFailure(string message) : Exception(message);

    public sealed class RequestState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
        public string Kind { get; set; } = string.Empty;
        public Guid BodyId { get; set; }
        public string Detail { get; set; } = string.Empty;
        public int Count { get; set; }
        public string ErrorType { get; set; } = string.Empty;
    }

    public sealed class RequestMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public RequestMachine(Uri serviceAddress)
        {
            InstanceState(state => state.CurrentState);
            Request(() => Validation, settings =>
            {
                settings.ServiceAddress = serviceAddress;
                settings.Timeout = TimeSpan.FromHours(1);
            });
            Initially(
                When(Primed).TransitionTo(Validation.Pending),
                When(Started).Request(Validation, context => new Validate(context.Message.BodyId)).TransitionTo(Validation.Pending));
            During(Validation.Pending,
                When(Validation.Completed).Then(context => Record(context.Saga, "first", context.Message.CorrelationId, context.Message.Reason))
                    .Publish(context => Result(context.Saga)).TransitionTo(Finished),
                When(Validation.Completed2).Then(context => Record(context.Saga, "second", context.Message.CorrelationId, context.Message.Reason))
                    .Publish(context => Result(context.Saga)).TransitionTo(Finished),
                When(Validation.Completed3).Then(context => Record(context.Saga, "third", context.Message.CorrelationId, context.Message.Reason))
                    .Publish(context => Result(context.Saga)).TransitionTo(Finished),
                When(Validation.Faulted).Then(context =>
                {
                    ExceptionInfo error = context.Message.Exceptions.Single();
                    Record(context.Saga, "fault", context.Message.Message.CorrelationId, error.Message);
                    context.Saga.ErrorType = error.ExceptionType;
                }).Publish(context => Result(context.Saga)).TransitionTo(Finished),
                When(Validation.TimeoutExpired).Then(context => Record(context.Saga, "timeout",
                    (context.Message.Message ?? throw new InvalidOperationException("Missing timed-out request")).CorrelationId, "expired"))
                    .Publish(context => Result(context.Saga)).TransitionTo(Finished));
        }

        private static void Record(RequestState state, string kind, Guid bodyId, string detail)
        {
            state.Kind = kind;
            state.BodyId = bodyId;
            state.Detail = detail;
            state.Count++;
        }

        private static Outcome Result(RequestState state) => new(state.CorrelationId, state.Kind, state.BodyId, state.Detail);
        public IEvent<Prime> Primed { get; } = null!;
        public IEvent<Begin> Started { get; } = null!;
        public IState Finished { get; } = null!;
        public IRequest<RequestState, Validate, First, Second, Third> Validation { get; } = null!;
    }

    private sealed class ResponseObserver : IConsumeObserver
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _count;
        public Task Completed => _completed.Task;
        public int Count => Volatile.Read(ref _count);
        private static bool IsResponse(object message) => message is First or Second or Third or Fault<Validate>
            or ViciOne.ServiceBus.Contracts.IRequestTimeoutExpired<Validate>;
        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;
        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class
        {
            if (IsResponse(context.Message))
            {
                Interlocked.Increment(ref _count);
                _completed.TrySetResult();
            }
            return Task.CompletedTask;
        }
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            if (IsResponse(context.Message))
                _completed.TrySetException(exception);
            return Task.CompletedTask;
        }
    }
}
