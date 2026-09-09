using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzNestedRequestIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-NESTED-REQUEST", "response-completes-original-request")]
    public async Task NestedSagaRequest_ResponseCompletesTheOriginalRequestAndClearsBothSagasAsync()
    {
        await using NestedRequestFixture fixture = await NestedRequestFixture.StartAsync(faultService: false);
        IRequestClient<CreateShortLink> client = fixture.Bus.CreateRequestClient<CreateShortLink>(
            fixture.SagaAddress,
            new RequestTimeout(fixture.Timeout));

        Response<ShortLinkCreated> response = await client.GetResponseAsync<ShortLinkCreated>(
                new CreateShortLink(new Uri("https://example.test/complete")),
                TestContext.Current.CancellationToken)
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        await fixture.Scheduled.Completed.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        await fixture.Canceled.Completed.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);

        Assert.Equal(new Uri("https://example.test/complete"), response.Message.Link);
        Assert.Equal(new Uri("https://short.example/complete"), response.Message.ShortLink);
        Assert.Equal(1, fixture.Scheduled.ObservedCount);
        Assert.Equal(1, fixture.Canceled.ObservedCount);
        Assert.Equal(0, fixture.LinkRepository.Count);
        Assert.Equal(0, fixture.RequestRepository.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-NESTED-REQUEST", "fault-propagates-to-original-request")]
    public async Task NestedSagaRequest_FaultPropagatesToTheOriginalRequesterAndClearsBothSagasAsync()
    {
        await using NestedRequestFixture fixture = await NestedRequestFixture.StartAsync(faultService: true);
        IRequestClient<CreateShortLink> client = fixture.Bus.CreateRequestClient<CreateShortLink>(
            fixture.SagaAddress,
            new RequestTimeout(fixture.Timeout));

        RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
            client.GetResponseAsync<ShortLinkCreated>(
                new CreateShortLink(new Uri("https://example.test/fault")),
                TestContext.Current.CancellationToken));
        await fixture.Scheduled.Completed.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        await fixture.Canceled.Completed.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);

        Assert.Contains(nameof(ExpectedLinkServiceException), exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, fixture.Scheduled.ObservedCount);
        Assert.Equal(1, fixture.Canceled.ObservedCount);
        Assert.Equal(0, fixture.LinkRepository.Count);
        Assert.Equal(0, fixture.RequestRepository.Count);
    }

    private sealed class NestedRequestFixture : IAsyncDisposable
    {
        private readonly QuartzTestBus _fixture;
        private readonly ConnectHandle _scheduledObserver;
        private readonly ConnectHandle _canceledObserver;

        private NestedRequestFixture(
            QuartzTestBus fixture,
            Uri sagaAddress,
            TimeSpan timeout,
            InMemorySagaRepository<CreateLinkState> linkRepository,
            InMemorySagaRepository<RequestState> requestRepository,
            ConsumeCompletionObserver<ScheduleMessage> scheduled,
            ConsumeCompletionObserver<CancelScheduledMessage> canceled,
            ConnectHandle scheduledObserver,
            ConnectHandle canceledObserver)
        {
            _fixture = fixture;
            SagaAddress = sagaAddress;
            Timeout = timeout;
            LinkRepository = linkRepository;
            RequestRepository = requestRepository;
            Scheduled = scheduled;
            Canceled = canceled;
            _scheduledObserver = scheduledObserver;
            _canceledObserver = canceledObserver;
        }

        public IBusControl Bus => _fixture.Bus;
        public Uri SagaAddress { get; }
        public TimeSpan Timeout { get; }
        public InMemorySagaRepository<CreateLinkState> LinkRepository { get; }
        public InMemorySagaRepository<RequestState> RequestRepository { get; }
        public ConsumeCompletionObserver<ScheduleMessage> Scheduled { get; }
        public ConsumeCompletionObserver<CancelScheduledMessage> Canceled { get; }

        public static async Task<NestedRequestFixture> StartAsync(bool faultService)
        {
            TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            string prefix = $"quartz-nested-request-{NewId.NextGuid():N}";
            var sagaAddress = new Uri($"loopback://localhost/{prefix}-saga");
            var serviceAddress = new Uri($"loopback://localhost/{prefix}-service");
            var linkRepository = new InMemorySagaRepository<CreateLinkState>();
            var requestRepository = new InMemorySagaRepository<RequestState>();
            var linkMachine = new CreateLinkStateMachine(serviceAddress);
            var requestMachine = new RequestStateMachine();
            QuartzTestBus fixture = await QuartzTestBus.StartAsync(
                timeout,
                configure: configurator =>
                {
                    configurator.ReceiveEndpoint($"{prefix}-saga", endpoint =>
                    {
                        endpoint.UseVolatileOutbox();
                        endpoint.StateMachineSaga(linkMachine, linkRepository);
                    });
                    configurator.ReceiveEndpoint($"{prefix}-request-state", endpoint =>
                    {
                        endpoint.UseVolatileOutbox();
                        endpoint.StateMachineSaga(requestMachine, requestRepository);
                    });
                    configurator.ReceiveEndpoint($"{prefix}-service", endpoint => endpoint.Handler<RequestShortLink>(context =>
                    {
                        if (faultService)
                            throw new ExpectedLinkServiceException();

                        return context.RespondAsync(new ShortLinkCreated(
                            context.Message.Link,
                            new Uri("https://short.example/complete")));
                    }));
                });
            var scheduled = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
            var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(_ => true);
            ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduled);
            ConnectHandle canceledObserver = fixture.Bus.ConnectConsumeObserver(canceled);

            return new NestedRequestFixture(
                fixture,
                sagaAddress,
                timeout,
                linkRepository,
                requestRepository,
                scheduled,
                canceled,
                scheduledObserver,
                canceledObserver);
        }

        public async ValueTask DisposeAsync()
        {
            _scheduledObserver.Dispose();
            _canceledObserver.Dispose();
            await _fixture.DisposeAsync();
        }
    }

    public sealed class CreateLinkState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public State CurrentState { get; set; } = null!;
        public Uri Link { get; set; } = null!;
        public Guid? LinkRequestId { get; set; }
    }

    public sealed class CreateLinkStateMachine : ViciOneServiceBusStateMachine<CreateLinkState>
    {
        public CreateLinkStateMachine(Uri serviceAddress)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => CreateRequested, configuration =>
            {
                configuration.CorrelateBy((instance, context) => instance.Link == context.Message.Link);
                configuration.SelectId(context => context.RequestId ?? NewId.NextGuid());
            });
            Request(() => LinkRequest, instance => instance.LinkRequestId, configuration =>
                configuration.Timeout = TimeSpan.FromMinutes(1));

            Initially(
                When(CreateRequested)
                    .Then(context => context.Saga.Link = context.Message.Link)
                    .Request(LinkRequest, _ => serviceAddress, context => new RequestShortLink(context.Saga.Link))
                    .RequestStarted()
                    .TransitionTo(LinkRequest.Pending));
            During(LinkRequest.Pending,
                When(LinkRequest.Completed)
                    .RequestCompleted(context => Task.FromResult(new ShortLinkCreated(
                        context.Saga.Link,
                        context.Message.ShortLink)))
                    .Finalize(),
                When(LinkRequest.Faulted)
                    .RequestFaulted(CreateRequested)
                    .Finalize());
            SetCompletedWhenFinalized();
        }

        public Event<CreateShortLink> CreateRequested { get; private set; } = null!;
        public Request<CreateLinkState, RequestShortLink, ShortLinkCreated> LinkRequest { get; private set; } = null!;
    }

    public sealed record CreateShortLink(Uri Link);
    public sealed record RequestShortLink(Uri Link);
    public sealed record ShortLinkCreated(Uri Link, Uri ShortLink);

    private sealed class ExpectedLinkServiceException : Exception
    {
    }
}
