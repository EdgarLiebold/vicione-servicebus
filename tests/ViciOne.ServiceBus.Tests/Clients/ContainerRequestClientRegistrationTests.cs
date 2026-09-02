using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class ContainerRequestClientRegistrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-REQUEST-CLIENT", "generic-and-runtime-addressed-registration-preserve-causation")]
    public async Task AddressedGenericAndRuntimeRegistrations_CompleteTheNestedRequestWithExactCausation()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new NestedRequestObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<InitialConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "container-initial-request");
                configuration.AddConsumer<SubsequentConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "container-subsequent-request");
                configuration.AddRequestClient<InitialRequest>(new Uri("queue:container-initial-request"));
                configuration.AddRequestClient(
                    typeof(SubsequentRequest),
                    new Uri("queue:container-subsequent-request"));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IRequestClient<InitialRequest> client =
                scope.ServiceProvider.GetRequiredService<IRequestClient<InitialRequest>>();
            Guid correlationId = NewId.NextGuid();

            Response<InitialResponse> response = await client.GetResponse<InitialResponse>(
                new InitialRequest(correlationId, "World"),
                cancellationToken);
            NestedRequestSnapshot nested = await observation.Seen.Task.WaitAsync(timeout, cancellationToken);
            ISentMessage<InitialRequest> initialRequest =
                Assert.Single(harness.Sent.Select<InitialRequest>(SnapshotOnlyToken()));
            ISentMessage<SubsequentRequest> subsequentRequest =
                Assert.Single(harness.Sent.Select<SubsequentRequest>(SnapshotOnlyToken()));

            Assert.Equal(new InitialResponse(correlationId, "Hello, World"), response.Message);
            Assert.Equal("container-initial-request", initialRequest.Context.DestinationAddress!.AbsolutePath.Trim('/'));
            Assert.Equal("container-subsequent-request", subsequentRequest.Context.DestinationAddress!.AbsolutePath.Trim('/'));
            Assert.Empty(harness.Published.Select<InitialRequest>(SnapshotOnlyToken()));
            Assert.Empty(harness.Published.Select<SubsequentRequest>(SnapshotOnlyToken()));
            Assert.Equal("container-initial-request", response.SourceAddress!.AbsolutePath.Trim('/'));
            Assert.Equal("container-subsequent-request", nested.InputAddress.AbsolutePath.Trim('/'));
            Assert.Equal(correlationId, nested.CorrelationId);
            Assert.Equal(correlationId, nested.InitiatorId);
            Assert.Equal(nested.ConversationId, response.ConversationId);
            Assert.NotNull(nested.RequestId);
            Assert.NotNull(response.RequestId);
            Assert.NotEqual(nested.RequestId, response.RequestId);
            Assert.Equal(correlationId, response.InitiatorId);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-REQUEST-CLIENT", "open-generic-registration-resolves-without-explicit-client")]
    public async Task OpenGenericRegistration_ResolvesAndCompletesWithoutAnExplicitRequestClient()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<OpenGenericConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IRequestClient<OpenGenericRequest> client =
                scope.ServiceProvider.GetRequiredService<IRequestClient<OpenGenericRequest>>();
            var request = new OpenGenericRequest(NewId.NextGuid(), "open");

            Response<OpenGenericResponse> response = await client.GetResponse<OpenGenericResponse>(
                request,
                cancellationToken);

            Assert.Equal(new OpenGenericResponse(request.CorrelationId, "OPEN"), response.Message);
            Assert.Equal(
                DefaultEndpointNameFormatter.Instance.Consumer<OpenGenericConsumer>(),
                response.SourceAddress!.AbsolutePath.Trim('/'));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-REQUEST-CLIENT", "scoped-factory-created-client-stays-in-consumer-scope")]
    public async Task ScopedClientFactory_UsesTheOwningConsumerScopeForTheNestedRoundTrip()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ScopedFactoryObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopeMarker>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ScopedFactoryOuterConsumer>();
                configuration.AddConsumer<ScopedFactoryInnerConsumer>();
                configuration.AddRequestClient<ScopedFactoryOuterRequest>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UsePublishFilter(typeof(ScopedFactoryPublishFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            IRequestClient<ScopedFactoryOuterRequest> client =
                harness.GetRequestClient<ScopedFactoryOuterRequest>();
            var request = new ScopedFactoryOuterRequest(NewId.NextGuid(), "scoped");

            Response<ScopedFactoryOuterResponse> response = await client.GetResponse<ScopedFactoryOuterResponse>(
                request,
                cancellationToken);
            ScopedFactorySnapshot snapshot = await observation.Seen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new ScopedFactoryOuterResponse(request.CorrelationId, "SCOPED"), response.Message);
            Assert.Same(snapshot.FactoryScope, snapshot.OuterConsumerScope);
            Assert.NotSame(snapshot.OuterConsumerScope, snapshot.InnerConsumerScope);
            Assert.Equal(snapshot.OuterInputAddress, snapshot.InnerSourceAddress);
            Assert.Equal(request.CorrelationId, snapshot.InnerInitiatorId);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public sealed record InitialRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record InitialResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record SubsequentRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record SubsequentResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record NestedRequestSnapshot(
        Guid CorrelationId,
        Guid? InitiatorId,
        Guid? ConversationId,
        Guid? RequestId,
        Uri InputAddress);

    public sealed class NestedRequestObservation
    {
        public TaskCompletionSource<NestedRequestSnapshot> Seen { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class InitialConsumer(IRequestClient<SubsequentRequest> client) : IConsumer<InitialRequest>
    {
        public async Task Consume(ConsumeContext<InitialRequest> context)
        {
            Response<SubsequentResponse> response = await client.GetResponse<SubsequentResponse>(
                new SubsequentRequest(context.Message.CorrelationId, context.Message.Value),
                context.CancellationToken);
            await context.RespondAsync(
                new InitialResponse(response.Message.CorrelationId, response.Message.Value));
        }
    }

    public sealed class SubsequentConsumer(NestedRequestObservation observation) : IConsumer<SubsequentRequest>
    {
        public async Task Consume(ConsumeContext<SubsequentRequest> context)
        {
            observation.Seen.TrySetResult(new NestedRequestSnapshot(
                context.Message.CorrelationId,
                context.InitiatorId,
                context.ConversationId,
                context.RequestId,
                context.ReceiveContext.InputAddress));
            await context.RespondAsync(new SubsequentResponse(
                context.Message.CorrelationId,
                $"Hello, {context.Message.Value}"));
        }
    }

    public sealed record OpenGenericRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record OpenGenericResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed class OpenGenericConsumer : IConsumer<OpenGenericRequest>
    {
        public Task Consume(ConsumeContext<OpenGenericRequest> context) => context.RespondAsync(
            new OpenGenericResponse(context.Message.CorrelationId, context.Message.Value.ToUpperInvariant()));
    }

    public sealed record ScopedFactoryOuterRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record ScopedFactoryOuterResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record ScopedFactoryInnerRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record ScopedFactoryInnerResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed class ScopeMarker;

    public sealed record ScopedFactorySnapshot(
        ScopeMarker FactoryScope,
        ScopeMarker OuterConsumerScope,
        ScopeMarker InnerConsumerScope,
        Uri OuterInputAddress,
        Uri? InnerSourceAddress,
        Guid? InnerInitiatorId);

    public sealed class ScopedFactoryObservation
    {
        public TaskCompletionSource<ScopeMarker> FactoryScope { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<ScopedFactoryInnerSnapshot> Inner { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<ScopedFactorySnapshot> Seen { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ScopedFactoryPublishFilter<T>(
        ScopeMarker scope,
        ScopedFactoryObservation observation) : IFilter<PublishContext<T>>
        where T : class
    {
        public void Probe(ProbeContext context) => context.CreateFilterScope("scopedFactory");

        public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            if (context.Message is ScopedFactoryInnerRequest)
                observation.FactoryScope.TrySetResult(scope);

            await next.Send(context);
        }
    }

    public sealed class ScopedFactoryOuterConsumer : IConsumer<ScopedFactoryOuterRequest>
    {
        private readonly IRequestClient<ScopedFactoryInnerRequest> _client;
        private readonly ScopeMarker _scope;
        private readonly ScopedFactoryObservation _observation;

        public ScopedFactoryOuterConsumer(
            IScopedClientFactory clientFactory,
            ScopeMarker scope,
            ScopedFactoryObservation observation)
        {
            _client = clientFactory.CreateRequestClient<ScopedFactoryInnerRequest>();
            _scope = scope;
            _observation = observation;
        }

        public async Task Consume(ConsumeContext<ScopedFactoryOuterRequest> context)
        {
            Response<ScopedFactoryInnerResponse> response = await _client.GetResponse<ScopedFactoryInnerResponse>(
                new ScopedFactoryInnerRequest(context.Message.CorrelationId, context.Message.Value),
                context.CancellationToken);
            ScopeMarker factoryScope = await _observation.FactoryScope.Task.WaitAsync(context.CancellationToken);
            ScopedFactoryInnerSnapshot inner = await _observation.Inner.Task.WaitAsync(context.CancellationToken);
            _observation.Seen.TrySetResult(new ScopedFactorySnapshot(
                factoryScope,
                _scope,
                inner.Scope,
                context.ReceiveContext.InputAddress,
                inner.SourceAddress,
                inner.InitiatorId));
            await context.RespondAsync(new ScopedFactoryOuterResponse(
                response.Message.CorrelationId,
                response.Message.Value));
        }
    }

    public sealed record ScopedFactoryInnerSnapshot(
        ScopeMarker Scope,
        Uri? SourceAddress,
        Guid? InitiatorId);

    public sealed class ScopedFactoryInnerConsumer(
        ScopeMarker scope,
        ScopedFactoryObservation observation) : IConsumer<ScopedFactoryInnerRequest>
    {
        public async Task Consume(ConsumeContext<ScopedFactoryInnerRequest> context)
        {
            observation.Inner.TrySetResult(new ScopedFactoryInnerSnapshot(
                scope,
                context.SourceAddress,
                context.InitiatorId));
            await context.RespondAsync(new ScopedFactoryInnerResponse(
                context.Message.CorrelationId,
                context.Message.Value.ToUpperInvariant()));
        }
    }
}
