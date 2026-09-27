using System.Collections.Concurrent;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts;
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
public sealed class QuartzSagaRequestGenerationIntegrationTests
{
    [Theory]
    [InlineData("response")]
    [InlineData("fault")]
    [InlineData("timeout")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "stale-generation-preserves-next-request-and-quartz-trigger")]
    public async Task PreviousRequestMessages_CannotCompleteOrCancelTheNextRequestOfTheSameSagaAsync(string staleKind)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        Guid sagaId = NewId.NextGuid();
        string prefix = $"quartz-generation-{NewId.NextGuid():N}";
        Uri inputAddress = new($"loopback://localhost/{prefix}-saga");
        Uri serviceAddress = new($"loopback://localhost/{prefix}-service");
        var repository = new InMemorySagaRepository<GenerationState>();
        var machine = new GenerationMachine(serviceAddress);
        var firstRequest = new TaskCompletionSource<ConsumeContext<Validate>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRequest = new TaskCompletionSource<ConsumeContext<Validate>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = new ConcurrentQueue<Validate>();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout, configure: bus =>
        {
            bus.ReceiveEndpoint($"{prefix}-saga", endpoint =>
            {
                endpoint.UseVolatileOutbox();
                endpoint.StateMachineSaga(machine, repository);
            });
            bus.ReceiveEndpoint($"{prefix}-service", endpoint => endpoint.Handler<Validate>(context =>
            {
                requests.Enqueue(context.Message);
                (context.Message.Generation == 1 ? firstRequest : secondRequest).TrySetResult(context);
                return Task.CompletedTask;
            }));
        });
        var firstScheduled = new QuartzSagaRequestTimeoutIntegrationTests.ScheduleMessageCapture();
        var firstCompleted = new ConsumeCompletionObserver<Accepted>(message => message.Generation == 1);
        var secondCompleted = new ConsumeCompletionObserver<Accepted>(message => message.Generation == 2);
        var begun = new ConsumeCompletionObserver<Begin>(message => message.Generation == 2);
        var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(_ => true, 2);
        var sends = new CancellationSends();
        using ConnectHandle scheduledHandle = fixture.Bus.ConnectConsumeObserver(firstScheduled);
        using ConnectHandle firstCompletedHandle = fixture.Bus.ConnectConsumeObserver(firstCompleted);
        using ConnectHandle secondCompletedHandle = fixture.Bus.ConnectConsumeObserver(secondCompleted);
        using ConnectHandle begunHandle = fixture.Bus.ConnectConsumeObserver(begun);
        using ConnectHandle canceledHandle = fixture.Bus.ConnectConsumeObserver(canceled);
        using ConnectHandle sendsHandle = fixture.Bus.ConnectSendObserver(sends);

        ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(inputAddress, cancellationToken: token).WaitAsync(timeout, token);
        await input.SendAsync(new Begin(sagaId, 1), token);
        ConsumeContext<Validate> first = await firstRequest.Task.WaitAsync(timeout, token);
        ScheduleMessage firstSchedule = await firstScheduled.Message.WaitAsync(timeout, token);
        Guid firstId = Assert.IsType<Guid>(first.RequestId);
        Assert.NotEqual(sagaId, firstId);
        Assert.Equal(firstId, firstSchedule.TokenId);
        Assert.Equal(new Validate(sagaId, 1), first.Message);
        Assert.Equal(inputAddress, first.ResponseAddress);
        Assert.True(await fixture.Scheduler.Exists(QuartzTriggerKey.ForOneTime(firstId, fixture.SchedulerNamespace), token));
        var firstCanceled = new ConsumeCompletionObserver<CancelScheduledMessage>(message => message.TokenId == firstId);
        using ConnectHandle firstCanceledHandle = fixture.Bus.ConnectConsumeObserver(firstCanceled);
        await SendWithRequestIdAsync(input, new Accepted(sagaId, 1, "first accepted"), firstId, token);
        await firstCompleted.Completed.WaitAsync(timeout, token);
        await firstCanceled.Completed.WaitAsync(timeout, token);
        Assert.Equal(machine.Completed, State(repository, sagaId).CurrentState);
        Assert.Null(State(repository, sagaId).RequestId);
        Assert.False(await fixture.Scheduler.Exists(QuartzTriggerKey.ForOneTime(firstId, fixture.SchedulerNamespace), token));

        var secondScheduled = new QuartzSagaRequestTimeoutIntegrationTests.ScheduleMessageCapture();
        using ConnectHandle secondScheduledHandle = fixture.Bus.ConnectConsumeObserver(secondScheduled);
        await input.SendAsync(new Begin(sagaId, 2), token);
        ConsumeContext<Validate> second = await secondRequest.Task.WaitAsync(timeout, token);
        ScheduleMessage secondSchedule = await secondScheduled.Message.WaitAsync(timeout, token);
        await begun.Completed.WaitAsync(timeout, token);
        Guid secondId = Assert.IsType<Guid>(second.RequestId);
        Assert.NotEqual(firstId, secondId);
        Assert.NotEqual(sagaId, secondId);
        Assert.Equal(secondId, secondSchedule.TokenId);
        Assert.Equal(new Validate(sagaId, 2), second.Message);
        Assert.Equal(inputAddress, second.ResponseAddress);
        Assert.Equal(new[] { firstId }, sends.Tokens);
        Assert.True(await fixture.Scheduler.Exists(QuartzTriggerKey.ForOneTime(secondId, fixture.SchedulerNamespace), token));

        Guid probeId = NewId.NextGuid();
        var received = new ExactReceiveCompletion(inputAddress, probeId);
        using ConnectHandle receiveHandle = fixture.Bus.ConnectReceiveObserver(received);
        switch (staleKind)
        {
            case "response":
                await SendWithRequestIdAsync(input, new Accepted(sagaId, 1, "late duplicate"), firstId, token, probeId);
                break;
            case "fault":
                Fault<Validate> fault = new ValidationFault(NewId.NextGuid(), first.MessageId,
                    new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
                    [new ValidationFailureInfo()], ViciOne.ServiceBus.Metadata.HostMetadataCache.Host,
                    [MessageUrn.ForTypeString<Validate>()], new Validate(sagaId, 1));
                await SendWithRequestIdAsync(input, fault, firstId, token, probeId);
                break;
            case "timeout":
                IRequestTimeoutExpired<Validate> expired = new Expired(sagaId, firstId, new Validate(sagaId, 1),
                    new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
                    new DateTimeOffset(2030, 1, 2, 4, 4, 5, TimeSpan.Zero));
                await SendWithRequestIdAsync(input, expired, firstId, token, probeId);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(staleKind));
        }
        await received.Completed.WaitAsync(timeout, token);
        GenerationState pending = State(repository, sagaId);
        Assert.Equal(machine.Validation.Pending, pending.CurrentState);
        Assert.Equal(secondId, pending.RequestId);
        Assert.Equal(1, pending.CompletedCount);
        Assert.Equal("first accepted", pending.Result);
        Assert.Equal(0, pending.FaultCount);
        Assert.Equal(0, pending.TimeoutCount);
        Assert.Equal(new[] { firstId }, sends.Tokens);
        Assert.True(await fixture.Scheduler.Exists(QuartzTriggerKey.ForOneTime(secondId, fixture.SchedulerNamespace), token));

        await SendWithRequestIdAsync(input, new Accepted(sagaId, 2, "second accepted"), secondId, token);
        await secondCompleted.Completed.WaitAsync(timeout, token);
        await canceled.Completed.WaitAsync(timeout, token);
        await fixture.Bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        GenerationState completed = State(repository, sagaId);
        Assert.Equal(machine.Completed, completed.CurrentState);
        Assert.Null(completed.RequestId);
        Assert.Equal(2, completed.CompletedCount);
        Assert.Equal("second accepted", completed.Result);
        Assert.Equal(0, completed.FaultCount);
        Assert.Equal(0, completed.TimeoutCount);
        Assert.Equal(new[] { firstId, secondId }, sends.Tokens);
        Assert.Equal(2, canceled.ObservedCount);
        Assert.Equal(new[] { new Validate(sagaId, 1), new Validate(sagaId, 2) }, requests);
        Assert.False(await fixture.Scheduler.Exists(QuartzTriggerKey.ForOneTime(firstId, fixture.SchedulerNamespace), token));
        Assert.False(await fixture.Scheduler.Exists(QuartzTriggerKey.ForOneTime(secondId, fixture.SchedulerNamespace), token));
    }

    private static Task SendWithRequestIdAsync<T>(ISendEndpoint endpoint, T message, Guid requestId,
        CancellationToken token, Guid? messageId = null) where T : class => endpoint.SendAsync(message,
        Pipe.Execute<SendContext<T>>(context =>
        {
            context.RequestId = requestId;
            if (messageId.HasValue)
                context.MessageId = messageId.Value;
        }), token);

    private static GenerationState State(InMemorySagaRepository<GenerationState> repository, Guid id) =>
        Assert.IsType<SagaInstance<GenerationState>>(repository[id]).Instance;

    private sealed class ExactReceiveCompletion(Uri address, Guid messageId) : IReceiveObserver
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Completed => _completed.Task;
        private bool Matches(ReceiveContext context) => context.InputAddress == address && context.GetMessageId() == messageId;
        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public Task PostReceiveAsync(ReceiveContext context)
        {
            if (Matches(context))
                _completed.TrySetResult();
            return Task.CompletedTask;
        }
        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            if (Matches(context))
                _completed.TrySetException(exception);
            return Task.CompletedTask;
        }
        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
        {
            if (Matches(context.Advanced().ReceiveContext))
                _completed.TrySetException(exception);
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationSends : ISendObserver
    {
        private readonly ConcurrentQueue<Guid> _tokens = new();
        public Guid[] Tokens => _tokens.ToArray();
        public Task PreSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;
        public Task PostSendAsync<T>(SendContext<T> context) where T : class
        {
            if (context.Message is CancelScheduledMessage cancel)
                _tokens.Enqueue(cancel.TokenId);
            return Task.CompletedTask;
        }
        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    public sealed record Begin(Guid CorrelationId, int Generation) : ICorrelatedBy<Guid>;
    public sealed record Validate(Guid CorrelationId, int Generation) : ICorrelatedBy<Guid>;
    public sealed record Accepted(Guid CorrelationId, int Generation, string Result) : ICorrelatedBy<Guid>;
    public sealed record Expired(Guid CorrelationId, Guid RequestId, Validate Message,
        DateTimeOffset Timestamp, DateTimeOffset ExpirationTime) : IRequestTimeoutExpired<Validate>;
    public sealed record ValidationFault(Guid FaultId, Guid? FaultedMessageId, DateTimeOffset Timestamp,
        ExceptionInfo[] Exceptions, HostInfo Host, string[] FaultMessageTypes, Validate Message) : Fault<Validate>;
    public sealed class ValidationFailureInfo : ExceptionInfo
    {
        public string ExceptionType => typeof(InvalidOperationException).FullName!;
        public ExceptionInfo? InnerException => null;
        public string StackTrace => string.Empty;
        public string Message => "late validation failure";
        public string Source => nameof(QuartzSagaRequestGenerationIntegrationTests);
        public IDictionary<string, object>? Data => null;
    }

    public sealed class GenerationState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
        public Guid? RequestId { get; set; }
        public int CompletedCount { get; set; }
        public int FaultCount { get; set; }
        public int TimeoutCount { get; set; }
        public string Result { get; set; } = string.Empty;
    }

    public sealed class GenerationMachine : ViciOneServiceBusStateMachine<GenerationState>
    {
        public GenerationMachine(Uri serviceAddress)
        {
            InstanceState(state => state.CurrentState);
            Request(() => Validation, state => state.RequestId, settings =>
            {
                settings.ServiceAddress = serviceAddress;
                settings.Timeout = TimeSpan.FromHours(1);
                settings.Completed = correlation => correlation.OnMissingInstance(missing => missing.Discard());
                settings.Faulted = correlation => correlation.OnMissingInstance(missing => missing.Discard());
                settings.TimeoutExpired = correlation => correlation.OnMissingInstance(missing => missing.Discard());
            });
            Initially(When(Started)
                .Request(Validation, context => new Validate(context.Saga.CorrelationId, context.Message.Generation))
                .TransitionTo(Validation.Pending));
            During(Completed, When(Started)
                .Request(Validation, context => new Validate(context.Saga.CorrelationId, context.Message.Generation))
                .TransitionTo(Validation.Pending));
            During(Validation.Pending,
                When(Validation.Completed).Then(context =>
                {
                    context.Saga.CompletedCount++;
                    context.Saga.Result = context.Message.Result;
                }).TransitionTo(Completed),
                When(Validation.Faulted).Then(context => context.Saga.FaultCount++).TransitionTo(Failed),
                When(Validation.TimeoutExpired).Then(context => context.Saga.TimeoutCount++).TransitionTo(ExpiredState));
        }
        public IEvent<Begin> Started { get; } = null!;
        public IState Completed { get; } = null!;
        public IState Failed { get; } = null!;
        public IState ExpiredState { get; } = null!;
        public IRequest<GenerationState, Validate, Accepted> Validation { get; } = null!;
    }
}
