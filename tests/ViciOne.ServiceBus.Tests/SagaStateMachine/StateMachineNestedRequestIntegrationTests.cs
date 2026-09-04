using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineNestedRequestIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-NESTED-REQUEST", "completed-request-resumes-original-request")]
    public async Task NestedRequestCompletion_ResumesTheOriginalRequestWithTheExactResponse()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri link = new("https://www.microsoft.com/");
        using var harness = CreateHarness("nested-complete", timeout, failRequest: false);
        var machine = new CreateLinkMachine(new Uri(harness.BaseAddress, "short-link-service"));
        ISagaStateMachineTestHarness<CreateLinkMachine, CreateLinkState> sagaHarness =
            harness.StateMachineSaga<CreateLinkState, CreateLinkMachine>(machine);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<CreateShortLink> client = harness.Bus.CreateRequestClient<CreateShortLink>(
                harness.InputQueueAddress,
                timeout);
            Response<ShortLinkCreated> response = await client.GetResponse<ShortLinkCreated>(
                    new CreateShortLink(link),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(link, response.Message.Link);
            Assert.Equal(link, response.Message.ShortLink);
            Assert.NotEqual(Guid.Empty, response.Message.CorrelationId);
            Assert.Equal(response.Message.CorrelationId, await sagaHarness.Exists(
                response.Message.CorrelationId,
                machine.Valid,
                timeout));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        ISentMessage<RequestShortLink> nested = Assert.Single(
            harness.Sent.Select<RequestShortLink>(SnapshotOnlyToken()));
        Assert.Equal(link, nested.Context.Message.Link);
        Assert.Single(harness.Consumed.Select<RequestShortLink>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<RequestShortLink>>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-NESTED-REQUEST", "faulted-request-faults-original-request")]
    public async Task NestedRequestFault_PropagatesOneTypedFaultToTheOriginalRequester()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri link = new("https://www.google.com/");
        using var harness = CreateHarness("nested-fault", timeout, failRequest: true);
        var machine = new CreateLinkMachine(new Uri(harness.BaseAddress, "short-link-service"));
        harness.StateMachineSaga<CreateLinkState, CreateLinkMachine>(machine);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<CreateShortLink> client = harness.Bus.CreateRequestClient<CreateShortLink>(
                harness.InputQueueAddress,
                timeout);
            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.GetResponse<ShortLinkCreated>(new CreateShortLink(link), cancellationToken));

            Assert.Equal(TypeCache<CreateShortLink>.ShortName, exception.RequestType);
            Assert.Contains(link.AbsoluteUri, exception.Message, StringComparison.Ordinal);
            Fault<CreateShortLink> originalFault = Assert.IsAssignableFrom<Fault<CreateShortLink>>(exception.Fault);
            ExceptionInfo cause = Assert.Single(originalFault.Exceptions);
            while (cause.ExceptionType != typeof(ExpectedNestedRequestException).FullName
                   && cause.InnerException is { } innerException)
                cause = innerException;
            Assert.Equal(typeof(ExpectedNestedRequestException).FullName, cause.ExceptionType);
            Assert.Contains(link.AbsoluteUri, cause.Message, StringComparison.Ordinal);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        ISentMessage<RequestShortLink> nested = Assert.Single(
            harness.Sent.Select<RequestShortLink>(SnapshotOnlyToken()));
        Assert.Equal(link, nested.Context.Message.Link);
        Assert.Single(harness.Consumed.Select<RequestShortLink>(SnapshotOnlyToken()));
    }

    private static InMemoryTestHarness CreateHarness(string suffix, TimeSpan timeout, bool failRequest)
    {
        var harness = new InMemoryTestHarness($"state-machine-{suffix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.UseInMemoryOutbox();
        harness.OnConfigureInMemoryBus += configurator =>
        {
            configurator.ReceiveEndpoint("request-state", endpoint => endpoint.StateMachineSaga(
                new RequestStateMachine(),
                new InMemorySagaRepository<RequestState>()));
            configurator.ReceiveEndpoint("short-link-service", endpoint => endpoint.Handler<RequestShortLink>(
                context => failRequest
                    ? Task.FromException(new ExpectedNestedRequestException(context.Message.Link.AbsoluteUri))
                    : context.RespondAsync(new ShortLinkCreated(
                        context.Message.CorrelationId,
                        context.Message.Link,
                        context.Message.Link))));
        };
        return harness;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public sealed record CreateShortLink(Uri Link);

    public sealed record RequestShortLink(Guid CorrelationId, Uri Link) : CorrelatedBy<Guid>;

    public sealed record ShortLinkCreated(Guid CorrelationId, Uri Link, Uri ShortLink) : CorrelatedBy<Guid>;

    public sealed class CreateLinkState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Uri Link { get; set; } = null!;

        public Uri ShortLink { get; set; } = null!;

        public Guid? LinkRequestId { get; set; }
    }

    public sealed class CreateLinkMachine : ViciOneServiceBusStateMachine<CreateLinkState>
    {
        public CreateLinkMachine(Uri serviceAddress)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => CreateRequested, configuration =>
            {
                configuration.CorrelateBy((instance, context) => instance.Link == context.Message.Link);
                configuration.SelectId(context => context.RequestId ?? NewId.NextGuid());
            });
            Request(() => LinkRequest, instance => instance.LinkRequestId, configuration =>
            {
                configuration.ServiceAddress = serviceAddress;
                configuration.Timeout = TimeSpan.Zero;
            });

            Initially(When(CreateRequested)
                .Then(context => context.Saga.Link = context.Message.Link)
                .Request(LinkRequest, context => new RequestShortLink(
                    context.Saga.CorrelationId,
                    context.Message.Link))
                .RequestStarted()
                .TransitionTo(LinkRequest.Pending));
            During(LinkRequest.Pending,
                When(LinkRequest.Completed)
                    .Then(context => context.Saga.ShortLink = context.Message.ShortLink)
                    .RequestCompleted()
                    .TransitionTo(Valid),
                When(LinkRequest.Faulted)
                    .RequestFaulted(CreateRequested)
                    .TransitionTo(Invalid));
            During(Valid, When(CreateRequested).Respond(context => new ShortLinkCreated(
                context.Saga.CorrelationId,
                context.Saga.Link,
                context.Saga.ShortLink)));
        }

        public State Valid { get; private set; } = null!;

        public State Invalid { get; private set; } = null!;

        public Event<CreateShortLink> CreateRequested { get; private set; } = null!;

        public Request<CreateLinkState, RequestShortLink, ShortLinkCreated> LinkRequest { get; private set; } = null!;
    }

    public sealed class ExpectedNestedRequestException(string message) : Exception(message);
}
