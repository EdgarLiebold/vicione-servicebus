using System.Collections.Concurrent;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
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
    public Task SagaIdRequest_RoutesToTheHeaderOwnerAndCancelsOrDeliversItsRealTimeoutAsync(string kind) => RunAsync(kind, false);

    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("third")]
    [InlineData("fault")]
    [InlineData("timeout")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "correlation-callbacks-override-default-owner-without-canceling-its-timeout")]
    public Task CustomCorrelation_SelectsTheBodyOwnerAndPreservesTheOtherSagaTimeoutAsync(string kind) => RunAsync(kind, true);

    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("third")]
    [InlineData("fault")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "missing-request-header-preserves-sagas-and-real-timeout")]
    public Task MissingRequestId_RejectsTheResponseWithoutChangingEitherSagaOrItsTimeoutAsync(string kind) => RunAsync(kind, false, true);

    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("third")]
    [InlineData("fault")]
    [InlineData("timeout")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "property-request-id-response-fault-and-real-timeout")]
    public Task PropertyRequestId_RoutesTheExactOwnerAndSettlesItsQuartzTriggerAsync(string kind) =>
        RunAsync(kind, false, propertyStored: true);

    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("third")]
    [InlineData("fault")]
    [InlineData("timeout")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "property-request-id-callback-override-preserves-request-owner-trigger")]
    public Task PropertyRequestId_CustomCorrelationSelectsBodyOwnerWithoutCancelingRequestOwnersTriggerAsync(string kind) =>
        RunAsync(kind, true, propertyStored: true);

    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("third")]
    [InlineData("fault")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "missing-property-request-id-cannot-select-unowned-saga")]
    public Task PropertyRequestId_MissingHeaderCannotSelectUnownedSagaOrCancelItsTriggerAsync(string kind) =>
        RunAsync(kind, false, missingHeader: true, propertyStored: true);

    [Theory]
    [InlineData("first", false)]
    [InlineData("second", false)]
    [InlineData("third", false)]
    [InlineData("fault", false)]
    [InlineData("first", true)]
    [InlineData("second", true)]
    [InlineData("fault", true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-REQUEST", "two-live-property-request-owners-and-quartz-triggers")]
    public async Task PropertyRequestId_ReplySettlesOnlyItsOwnerWhileNeighborTimesOutAsync(string kind, bool twoResponses)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        Guid activeId = NewId.NextGuid();
        Guid neighborId = NewId.NextGuid();
        string prefix = $"quartz-property-neighbors-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{prefix}-saga");
        var serviceAddress = new Uri($"loopback://localhost/{prefix}-service");
        var repository = new InMemorySagaRepository<RequestState>();
        ViciOneServiceBusStateMachine<RequestState> machine = twoResponses
            ? new TwoRequestMachine(serviceAddress)
            : new RequestMachine(serviceAddress, propertyStored: true);
        IState pendingState = twoResponses
            ? ((TwoRequestMachine)machine).Validation.Pending
            : ((RequestMachine)machine).Validation.Pending;
        IState finishedState = twoResponses
            ? ((TwoRequestMachine)machine).Finished
            : ((RequestMachine)machine).Finished;
        var activeRequest = new TaskCompletionSource<RequestEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        var neighborRequest = new TaskCompletionSource<RequestEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeOutcome = new TaskCompletionSource<Outcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var neighborOutcome = new TaskCompletionSource<Outcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcomes = new ConcurrentQueue<Outcome>();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout, configure: bus =>
        {
            bus.ReceiveEndpoint($"{prefix}-saga", endpoint =>
            {
                endpoint.UseVolatileOutbox();
                endpoint.StateMachineSaga(machine, repository);
            });
            bus.ReceiveEndpoint($"{prefix}-service", endpoint => endpoint.Handler<Validate>(context =>
            {
                var envelope = new RequestEnvelope(context.RequestId, context.Message.CorrelationId,
                    context.ResponseAddress, context.Headers.Get<object>(MessageHeaders.Request.Accept));
                (context.Message.CorrelationId == neighborId ? activeRequest : neighborRequest).TrySetResult(envelope);
                return Task.CompletedTask;
            }));
            bus.ReceiveEndpoint($"{prefix}-results", endpoint => endpoint.Handler<Outcome>(context =>
            {
                outcomes.Enqueue(context.Message);
                (context.Message.CorrelationId == activeId ? activeOutcome : neighborOutcome).TrySetResult(context.Message);
                return Task.CompletedTask;
            }));
        });
        Guid activeToken = Guid.Empty;
        var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(message => message.TokenId == activeToken);
        var scheduled = new ConsumeCompletionObserver<ScheduleMessage>(_ => true, expectedCount: 2);
        using ConnectHandle canceledHandle = fixture.Bus.ConnectConsumeObserver(canceled);
        using ConnectHandle scheduledHandle = fixture.Bus.ConnectConsumeObserver(scheduled);
        try
        {
            ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(inputAddress, cancellationToken: token)
                .WaitAsync(timeout, token);
            await input.SendAsync(new Begin(activeId, neighborId), token);
            await input.SendAsync(new Begin(neighborId, activeId), token);
            RequestEnvelope first = await activeRequest.Task.WaitAsync(timeout, token);
            RequestEnvelope second = await neighborRequest.Task.WaitAsync(timeout, token);
            await scheduled.Completed.WaitAsync(timeout, token);
            Assert.Equal(2, scheduled.ObservedCount);
            activeToken = Assert.IsType<Guid>(first.RequestId);
            Guid neighborToken = Assert.IsType<Guid>(second.RequestId);
            Assert.NotEqual(activeId, activeToken);
            Assert.NotEqual(neighborId, activeToken);
            Assert.NotEqual(neighborToken, activeToken);
            Assert.NotEqual(activeId, neighborToken);
            Assert.NotEqual(neighborId, neighborToken);
            Assert.Equal(inputAddress, first.ResponseAddress);
            Assert.Equal(inputAddress, second.ResponseAddress);
            string[] acceptedTypes = twoResponses
                ? [MessageUrn.ForTypeString<First>(), MessageUrn.ForTypeString<Second>()]
                : [MessageUrn.ForTypeString<First>(), MessageUrn.ForTypeString<Second>(), MessageUrn.ForTypeString<Third>()];
            Assert.Equal(acceptedTypes,
                Assert.IsAssignableFrom<IEnumerable<object>>(first.Accept).Select(item => Assert.IsType<string>(item)));
            Assert.Equal(acceptedTypes,
                Assert.IsAssignableFrom<IEnumerable<object>>(second.Accept).Select(item => Assert.IsType<string>(item)));
            TriggerKey activeKey = QuartzTriggerKey.ForOneTime(activeToken, fixture.SchedulerNamespace);
            TriggerKey neighborKey = QuartzTriggerKey.ForOneTime(neighborToken, fixture.SchedulerNamespace);
            ITrigger neighborTrigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(neighborKey, token));
            Assert.True(await fixture.Scheduler.Exists(activeKey, token));
            Assert.Equal(activeToken, State(repository, activeId).RequestId);
            Assert.Equal(neighborToken, State(repository, neighborId).RequestId);
            Assert.Equal(pendingState, State(repository, activeId).CurrentState);
            Assert.Equal(pendingState, State(repository, neighborId).CurrentState);
            Assert.Empty(outcomes);

            Guid wrongToken = NewId.NextGuid();
            Assert.NotEqual(activeToken, wrongToken);
            Assert.NotEqual(neighborToken, wrongToken);
            Guid probeId = NewId.NextGuid();
            var probe = new RejectedResponseObserver(inputAddress, probeId);
            using ConnectHandle probeHandle = fixture.Bus.ConnectReceiveObserver(probe);
            await SendSelectedAsync(wrongToken, probeId);
            await probe.Completed.WaitAsync(timeout, token);
            Assert.Equal(pendingState, State(repository, activeId).CurrentState);
            Assert.Equal(activeToken, State(repository, activeId).RequestId);
            Assert.Equal(pendingState, State(repository, neighborId).CurrentState);
            Assert.Equal(neighborToken, State(repository, neighborId).RequestId);
            Assert.True(await fixture.Scheduler.Exists(activeKey, token));
            Assert.True(await fixture.Scheduler.Exists(neighborKey, token));
            Assert.Empty(outcomes);
            Assert.Equal(0, canceled.ObservedCount);

            await SendSelectedAsync(activeToken);

            async Task SendSelectedAsync(Guid requestToken, Guid? messageId = null)
            {
                switch (kind)
                {
                    case "first": await SendWithRequestIdAsync(input, new First(neighborId, kind), requestToken, token, messageId); break;
                    case "second": await SendWithRequestIdAsync(input, new Second(neighborId, kind), requestToken, token, messageId); break;
                    case "third": await SendWithRequestIdAsync(input, new Third(neighborId, kind), requestToken, token, messageId); break;
                    case "fault":
                        Fault<Validate> fault = new ValidationFault(NewId.NextGuid(), null, DateTimeOffset.UtcNow,
                            [new ValidationExceptionInfo()], ViciOne.ServiceBus.Metadata.HostMetadataCache.Host,
                            [MessageUrn.ForTypeString<Validate>()], new Validate(neighborId));
                        await SendWithRequestIdAsync(input, fault, requestToken, token, messageId);
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(kind));
                }
            }

            Outcome firstResult = await activeOutcome.Task.WaitAsync(timeout, token);
            await canceled.Completed.WaitAsync(timeout, token);
            Assert.Equal(new Outcome(activeId, kind, neighborId,
                kind == "fault" ? "validation rejected" : kind), firstResult);
            Assert.Equal(finishedState, State(repository, activeId).CurrentState);
            Assert.Equal(1, State(repository, activeId).Count);
            Assert.Equal(kind == "fault" ? typeof(ExpectedServiceFailure).FullName : string.Empty,
                State(repository, activeId).ErrorType);
            Assert.False(await fixture.Scheduler.Exists(activeKey, token));
            Assert.True(await fixture.Scheduler.Exists(neighborKey, token));
            Assert.Equal(pendingState, State(repository, neighborId).CurrentState);
            Assert.Equal(neighborToken, State(repository, neighborId).RequestId);
            Assert.Equal(0, State(repository, neighborId).Count);
            Assert.False(neighborOutcome.Task.IsCompleted);

            await fixture.Scheduler.TriggerJob(neighborTrigger.JobKey, neighborTrigger.JobDataMap, token);
            Outcome secondResult = await neighborOutcome.Task.WaitAsync(timeout, token);
            Assert.Equal(new Outcome(neighborId, "timeout", activeId, "expired"), secondResult);
            Assert.Equal(finishedState, State(repository, neighborId).CurrentState);
            Assert.Equal(1, State(repository, neighborId).Count);
            Assert.Equal(new[] { firstResult, secondResult }, outcomes);
            Assert.Equal(1, canceled.ObservedCount);
        }
        finally
        {
            await fixture.Bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static async Task RunAsync(string kind, bool useBodyCorrelation, bool missingHeader = false,
        bool propertyStored = false)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid activeId = NewId.NextGuid();
        Guid controlId = NewId.NextGuid();
        Guid expectedOwner = useBodyCorrelation ? controlId : activeId;
        string prefix = $"quartz-saga-id-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{prefix}-saga");
        var serviceAddress = new Uri($"loopback://localhost/{prefix}-service");
        var repository = new InMemorySagaRepository<RequestState>();
        var machine = new RequestMachine(serviceAddress, useBodyCorrelation, propertyStored);
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
                if (missingHeader)
                    return;
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
        Guid expectedCancelToken = expectedOwner;
        var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(message => message.TokenId == expectedCancelToken);
        var primed = new ConsumeCompletionObserver<Prime>(_ => true);
        var started = new ConsumeCompletionObserver<Begin>(_ => true);
        var response = new ResponseObserver();
        Guid probeMessageId = NewId.NextGuid();
        var rejected = new RejectedResponseObserver(inputAddress, probeMessageId);
        Guid? scheduledToken = null;
        using ConnectHandle rejectedHandle = fixture.Bus.ConnectReceiveObserver(rejected);
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
            scheduledToken = schedule.TokenId;
            TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(schedule.TokenId, fixture.SchedulerNamespace);
            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(triggerKey, cancellationToken));

            Guid requestId = Assert.IsType<Guid>(request.RequestId);
            if (propertyStored)
                Assert.NotEqual(activeId, requestId);
            else
                Assert.Equal(activeId, requestId);
            if (propertyStored && !useBodyCorrelation)
                expectedCancelToken = requestId;
            Assert.Equal(controlId, request.BodyId);
            Assert.Equal(inputAddress, request.ResponseAddress);
            Assert.Equal(new[] { MessageUrn.ForTypeString<First>(), MessageUrn.ForTypeString<Second>(), MessageUrn.ForTypeString<Third>() },
                Assert.IsAssignableFrom<IEnumerable<object>>(request.Accept).Select(item => Assert.IsType<string>(item)));
            Assert.Equal(requestId, schedule.TokenId);
            Assert.Equal(machine.Validation.Pending, State(repository, activeId).CurrentState);
            Assert.Equal(propertyStored ? requestId : null, State(repository, activeId).RequestId);
            Assert.Null(State(repository, controlId).RequestId);
            Assert.False(outcome.Task.IsCompleted);

            releaseService.SetResult();
            if (missingHeader)
            {
                switch (kind)
                {
                    case "first": await SendWithoutRequestIdAsync(input, new First(activeId, kind), probeMessageId, cancellationToken); break;
                    case "second": await SendWithoutRequestIdAsync(input, new Second(activeId, kind), probeMessageId, cancellationToken); break;
                    case "third": await SendWithoutRequestIdAsync(input, new Third(activeId, kind), probeMessageId, cancellationToken); break;
                    case "fault":
                        Fault<Validate> fault = new ValidationFault(NewId.NextGuid(), NewId.NextGuid(), DateTimeOffset.UtcNow,
                            [new ValidationExceptionInfo()], ViciOne.ServiceBus.Metadata.HostMetadataCache.Host,
                            [MessageUrn.ForTypeString<Validate>()], new Validate(activeId));
                        await SendWithoutRequestIdAsync(input, fault, probeMessageId, cancellationToken);
                        break;
                }
                if (propertyStored)
                {
                    await rejected.Completed.WaitAsync(timeout, cancellationToken);
                    Assert.False(outcome.Task.IsCompleted);
                }
                else
                {
                    Task completed = await Task.WhenAny(rejected.Fault, outcome.Task).WaitAsync(timeout, cancellationToken);
                    Assert.Same(rejected.Fault, completed);
                    Assert.Equal("Missing RequestId", Assert.IsType<RequestException>(await rejected.Fault).Message);
                }
                Assert.True(await fixture.Scheduler.Exists(triggerKey, cancellationToken));
            }
            else
            {
                if (kind == "timeout")
                    await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, cancellationToken);

                Outcome result = await outcome.Task.WaitAsync(timeout, cancellationToken);
                await response.Completed.WaitAsync(timeout, cancellationToken);
                string detail = kind switch { "fault" => "validation rejected", "timeout" => "expired", _ => kind };
                Assert.Equal(new Outcome(expectedOwner, kind, controlId, detail), result);
                RequestState active = State(repository, expectedOwner);
                Assert.Equal(machine.Finished, active.CurrentState);
                Assert.Equal(1, active.Count);
                Assert.Equal(kind == "fault" ? typeof(ExpectedServiceFailure).FullName : string.Empty, active.ErrorType);
                if (kind != "timeout")
                {
                    if (!(propertyStored && useBodyCorrelation))
                    {
                        await canceled.Completed.WaitAsync(timeout, cancellationToken);
                        Assert.Equal(1, canceled.ObservedCount);
                    }
                    else
                        Assert.Equal(0, canceled.ObservedCount);
                    Assert.Equal(useBodyCorrelation, await fixture.Scheduler.Exists(triggerKey, cancellationToken));
                }
                else if (useBodyCorrelation)
                    Assert.True(await fixture.Scheduler.Exists(triggerKey, cancellationToken));
            }
        }
        finally
        {
            releaseService.TrySetResult();
            await fixture.Bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        RequestState control = State(repository, useBodyCorrelation ? activeId : controlId);
        Assert.Equal(machine.Validation.Pending, control.CurrentState);
        Assert.Equal(0, control.Count);
        Assert.Equal(string.Empty, control.Kind);
        Assert.Equal(string.Empty, control.Detail);
        Assert.Equal(string.Empty, control.ErrorType);
        Assert.Equal(Guid.Empty, control.BodyId);
        if (propertyStored)
        {
            RequestState originalOwner = State(repository, activeId);
            bool requestIdRetained = useBodyCorrelation || missingHeader || kind is "fault" or "timeout";
            Assert.Equal(requestIdRetained ? scheduledToken : null, originalOwner.RequestId);
            Assert.Equal(useBodyCorrelation || missingHeader ? machine.Validation.Pending : machine.Finished,
                originalOwner.CurrentState);
        }
        if (propertyStored && useBodyCorrelation)
            Assert.Equal(0, canceled.ObservedCount);
        if (missingHeader)
        {
            RequestState active = State(repository, activeId);
            Assert.Equal(machine.Validation.Pending, active.CurrentState);
            Assert.Equal(0, active.Count);
            Assert.Equal(string.Empty, active.Kind);
            Assert.Equal(string.Empty, active.Detail);
            Assert.Equal(string.Empty, active.ErrorType);
            Assert.Equal(Guid.Empty, active.BodyId);
            Assert.Empty(outcomes);
            Assert.Equal(propertyStored ? 1 : 0, response.Count);
            Assert.Equal(0, canceled.ObservedCount);
            Assert.True(await fixture.Scheduler.Exists(QuartzTriggerKey.ForOneTime(
                Assert.IsType<Guid>(scheduledToken), fixture.SchedulerNamespace), cancellationToken));
        }
        else
        {
            Assert.Single(outcomes);
            Assert.Equal(1, response.Count);
        }
    }

    private static Task SendWithoutRequestIdAsync<T>(ISendEndpoint endpoint, T message, Guid messageId, CancellationToken cancellationToken)
        where T : class => endpoint.SendAsync(message, Pipe.Execute<SendContext<T>>(context =>
        {
            context.MessageId = messageId;
            context.RequestId = null;
        }), cancellationToken);

    private static Task SendWithRequestIdAsync<T>(ISendEndpoint endpoint, T message, Guid requestId, CancellationToken token,
        Guid? messageId = null)
        where T : class => endpoint.SendAsync(message,
            Pipe.Execute<SendContext<T>>(context =>
            {
                context.RequestId = requestId;
                if (messageId.HasValue)
                    context.MessageId = messageId.Value;
            }), token);

    public sealed record ValidationFault(Guid FaultId, Guid? FaultedMessageId, DateTimeOffset Timestamp,
        ExceptionInfo[] Exceptions, HostInfo Host, string[] FaultMessageTypes, Validate Message) : Fault<Validate>;

    public sealed class ValidationExceptionInfo : ExceptionInfo
    {
        public string ExceptionType => typeof(ExpectedServiceFailure).FullName!;
        public ExceptionInfo? InnerException => null;
        public string StackTrace => string.Empty;
        public string Message => "validation rejected";
        public string Source => nameof(QuartzSagaIdRequestIntegrationTests);
        public IDictionary<string, object>? Data => null;
    }

    private sealed class RejectedResponseObserver(Uri inputAddress, Guid messageId) : IReceiveObserver
    {
        private readonly TaskCompletionSource<Exception> _fault = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<Exception> Fault => _fault.Task;
        public Task Completed => _completed.Task;
        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public Task PostReceiveAsync(ReceiveContext context)
        {
            if (Matches(context))
                _completed.TrySetResult();
            return Task.CompletedTask;
        }
        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class => Task.CompletedTask;
        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            if (Matches(context))
                _fault.TrySetResult(exception);
            return Task.CompletedTask;
        }

        private bool Matches(ReceiveContext context)
        {
            Guid? receivedId = context.TryGetPayload(out ConsumeContext? consume) ? consume.MessageId : context.GetMessageId();
            return context.InputAddress == inputAddress && receivedId == messageId;
        }
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
        public Guid? RequestId { get; set; }
        public string Kind { get; set; } = string.Empty;
        public Guid BodyId { get; set; }
        public string Detail { get; set; } = string.Empty;
        public int Count { get; set; }
        public string ErrorType { get; set; } = string.Empty;
    }

    public sealed class TwoRequestMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public TwoRequestMachine(Uri serviceAddress)
        {
            InstanceState(state => state.CurrentState);
            Request(() => Validation, state => state.RequestId, settings =>
            {
                settings.ServiceAddress = serviceAddress;
                settings.Timeout = TimeSpan.FromHours(1);
            });
            Initially(When(Started)
                .Request(Validation, context => new Validate(context.Message.BodyId))
                .TransitionTo(Validation.Pending));
            During(Validation.Pending,
                When(Validation.Completed)
                    .Then(context => Record(context.Saga, "first", context.Message.CorrelationId, context.Message.Reason))
                    .Publish(context => Result(context.Saga)).TransitionTo(Finished),
                When(Validation.Completed2)
                    .Then(context => Record(context.Saga, "second", context.Message.CorrelationId, context.Message.Reason))
                    .Publish(context => Result(context.Saga)).TransitionTo(Finished),
                When(Validation.Faulted).Then(context =>
                {
                    ExceptionInfo error = context.Message.Exceptions.Single();
                    Record(context.Saga, "fault", context.Message.Message.CorrelationId, error.Message);
                    context.Saga.ErrorType = error.ExceptionType;
                }).Publish(context => Result(context.Saga)).TransitionTo(Finished),
                When(Validation.TimeoutExpired)
                    .Then(context => Record(context.Saga, "timeout",
                        (context.Message.Message ?? throw new InvalidOperationException("Missing timed-out request")).CorrelationId,
                        "expired"))
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
        public IEvent<Begin> Started { get; } = null!;
        public IState Finished { get; } = null!;
        public IRequest<RequestState, Validate, First, Second> Validation { get; } = null!;
    }

    public sealed class RequestMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public RequestMachine(Uri serviceAddress, bool useBodyCorrelation = false, bool propertyStored = false)
        {
            InstanceState(state => state.CurrentState);
            if (propertyStored)
                Request(() => Validation, state => state.RequestId, settings => Configure(settings));
            else
                Request(() => Validation, settings => Configure(settings));

            void Configure(IRequestConfigurator<RequestState, Validate, First, Second, Third> settings)
            {
                settings.ServiceAddress = serviceAddress;
                settings.Timeout = TimeSpan.FromHours(1);
                if (useBodyCorrelation)
                {
                    settings.Completed = correlation => correlation.CorrelateById(context => context.Message.CorrelationId);
                    settings.Completed2 = correlation => correlation.CorrelateById(context => context.Message.CorrelationId);
                    settings.Completed3 = correlation => correlation.CorrelateById(context => context.Message.CorrelationId);
                    settings.Faulted = correlation => correlation.CorrelateById(context => context.Message.Message.CorrelationId);
                    settings.TimeoutExpired = correlation => correlation.CorrelateById(context =>
                        (context.Message.Message ?? throw new InvalidOperationException("Missing timed-out request")).CorrelationId);
                }
            }
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
