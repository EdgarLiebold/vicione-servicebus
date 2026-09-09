using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ContainerScopedEndpointTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ROOT-ENDPOINTS", "publish-and-send-proxies-work-from-root-provider")]
    public async Task RootProviderEndpoints_PublishAndSendThroughTheStartedBusAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new BusScopeObservation(expected: 2);
        await using ServiceProvider provider = BuildBusProvider(observation, timeout, validateScopes: false);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var published = new RootPublished(NewId.NextGuid());
            var sent = new RootSent(NewId.NextGuid());
            IPublishEndpoint publishEndpoint = provider.GetRequiredService<IPublishEndpoint>();
            ISendEndpointProvider sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
            ISendEndpoint endpoint = await sendEndpointProvider.GetSendEndpointAsync(new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<BusScopeConsumer>()}"), TestContext.Current.CancellationToken);

            await publishEndpoint.PublishAsync(published, cancellationToken);
            await endpoint.SendAsync(sent, cancellationToken);
            BusScopeCapture[] captures = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Contains(captures, capture => capture.CorrelationId == published.CorrelationId);
            Assert.Contains(captures, capture => capture.CorrelationId == sent.CorrelationId);
            Assert.All(captures, capture => Assert.Equal(BusScopeOperation.Root, capture.Operation));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "publish-send-request-and-filter-scope-matrix")]
    public async Task ScopedBusEndpoints_CarryTheExactCallerScopeThroughEveryOutboundShapeAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new BusScopeObservation(expected: 4);
        await using ServiceProvider provider = BuildBusProvider(observation, timeout, validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            ScopeMarker expected = scope.ServiceProvider.GetRequiredService<ScopeMarker>();
            IPublishEndpoint publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            ISendEndpointProvider sender = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
            IRequestClient<BusScopeRequest> client = scope.ServiceProvider.GetRequiredService<IRequestClient<BusScopeRequest>>();
            IRequestClient<ExplicitBusScopeRequest> explicitClient = scope.ServiceProvider
                .GetRequiredService<IRequestClient<ExplicitBusScopeRequest>>();
            var published = new ScopedPublished(NewId.NextGuid());
            var sent = new ScopedSent(NewId.NextGuid());
            var request = new BusScopeRequest(NewId.NextGuid());
            var explicitRequest = new ExplicitBusScopeRequest(NewId.NextGuid());
            ISendEndpoint endpoint = await sender.GetSendEndpointAsync(new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<BusScopeConsumer>()}"), TestContext.Current.CancellationToken);

            await publisher.PublishAsync(published, cancellationToken);
            await endpoint.SendAsync(sent, cancellationToken);
            Response<BusScopeResponse> response = await client.Advanced().GetResponseAsync<BusScopeResponse>(
                request,
                timeout: new RequestTimeout(timeout),
                cancellationToken: cancellationToken);
            Response<BusScopeResponse> explicitResponse = await explicitClient.Advanced().GetResponseAsync<BusScopeResponse>(
                explicitRequest,
                timeout: new RequestTimeout(timeout),
                cancellationToken: cancellationToken);
            BusScopeCapture[] captures = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(request.CorrelationId, response.Message.CorrelationId);
            Assert.Equal(explicitRequest.CorrelationId, explicitResponse.Message.CorrelationId);
            AssertScopedCapture(captures, published.CorrelationId, BusScopeOperation.Publish, expected, scope.ServiceProvider);
            AssertScopedCapture(captures, sent.CorrelationId, BusScopeOperation.Send, expected, scope.ServiceProvider);
            AssertScopedCapture(captures, request.CorrelationId, BusScopeOperation.Request, expected, scope.ServiceProvider);
            AssertScopedCapture(
                captures,
                explicitRequest.CorrelationId,
                BusScopeOperation.ExplicitRequest,
                expected,
                scope.ServiceProvider);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-MEDIATOR", "send-publish-request-and-filter-scope-matrix")]
    public async Task ScopedMediator_CarriesTheExactCallerScopeThroughEveryOutboundShapeAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new MediatorScopeObservation(expected: 3);
        await using ServiceProvider provider = BuildMediatorProvider(observation);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        ScopeMarker expected = scope.ServiceProvider.GetRequiredService<ScopeMarker>();
        IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
        var sent = new MediatorSent(NewId.NextGuid());
        var published = new MediatorPublished(NewId.NextGuid());
        var request = new MediatorScopeRequest(NewId.NextGuid());

        await mediator.SendAsync(sent, cancellationToken).WaitAsync(timeout, cancellationToken);
        await mediator.PublishAsync(published, cancellationToken).WaitAsync(timeout, cancellationToken);
        IRequestClient<MediatorScopeRequest> client = mediator.CreateRequestClient<MediatorScopeRequest>();
        Response<MediatorScopeResponse> response = await client.Advanced().GetResponseAsync<MediatorScopeResponse>(
            request,
            cancellationToken: cancellationToken).WaitAsync(timeout, cancellationToken);
        MediatorScopeCapture[] captures = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

        Assert.Equal(request.CorrelationId, response.Message.CorrelationId);
        AssertMediatorCapture(captures, sent.CorrelationId, MediatorScopeOperation.Send, expected);
        AssertMediatorCapture(captures, published.CorrelationId, MediatorScopeOperation.Publish, expected);
        AssertMediatorCapture(captures, request.CorrelationId, MediatorScopeOperation.Request, expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-MEDIATOR", "nested-send-and-publish-preserve-both-scoped-dependencies")]
    public async Task NestedScopedMediatorDispatch_PreservesBothDependenciesAsync(bool publish)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new CascadeObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopeMarker>()
            .AddScoped<SecondaryScopeMarker>()
            .AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<CascadeConsumer>();
                configuration.AddConsumer<CascadeLeafConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        ScopeMarker expected = scope.ServiceProvider.GetRequiredService<ScopeMarker>();
        SecondaryScopeMarker expectedSecondary = scope.ServiceProvider.GetRequiredService<SecondaryScopeMarker>();
        IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
        var message = new CascadeMessage(NewId.NextGuid(), publish);

        await mediator.SendAsync(message, cancellationToken).WaitAsync(timeout, cancellationToken);
        CascadeResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

        Assert.Equal(message.CorrelationId, result.CorrelationId);
        Assert.Same(expected, result.ParentMarker);
        Assert.Same(expectedSecondary, result.ParentSecondaryMarker);
        Assert.Same(expected, result.LeafMarker);
        Assert.Same(expectedSecondary, result.LeafSecondaryMarker);
    }

    private static ServiceProvider BuildBusProvider(
        BusScopeObservation observation,
        TimeSpan timeout,
        bool validateScopes) =>
        new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopeMarker>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<BusScopeConsumer>();
                configuration.AddRequestClient<BusScopeRequest>();
                configuration.AddRequestClient<ExplicitBusScopeRequest>(
                    new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<BusScopeConsumer>()}"));
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UsePublishFilter(typeof(BusPublishScopeFilter<>), context);
                    bus.UseSendFilter(typeof(BusSendScopeFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: validateScopes);

    private static ServiceProvider BuildMediatorProvider(MediatorScopeObservation observation) =>
        new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopeMarker>()
            .AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<MediatorScopeConsumer>();
                configuration.ConfigureMediator((context, mediator) =>
                {
                    mediator.UsePublishFilter(typeof(MediatorPublishScopeFilter<>), context);
                    mediator.UseSendFilter(typeof(MediatorSendScopeFilter<>), context);
                });
            })
            .BuildServiceProvider(validateScopes: true);

    private static void AssertScopedCapture(
        BusScopeCapture[] captures,
        Guid correlationId,
        BusScopeOperation operation,
        ScopeMarker marker,
        IServiceProvider provider)
    {
        BusScopeCapture capture = Assert.Single(captures, candidate => candidate.CorrelationId == correlationId);
        Assert.Equal(operation, capture.Operation);
        Assert.Same(marker, capture.Marker);
        Assert.Same(provider, capture.ServiceProvider);
        Assert.Same(provider, capture.ServiceScope!.ServiceProvider);
    }

    private static void AssertMediatorCapture(
        MediatorScopeCapture[] captures,
        Guid correlationId,
        MediatorScopeOperation operation,
        ScopeMarker marker)
    {
        MediatorScopeCapture capture = Assert.Single(captures, candidate => candidate.CorrelationId == correlationId);
        Assert.Equal(operation, capture.Operation);
        Assert.Same(marker, capture.FilterMarker);
        Assert.Same(marker, capture.ConsumerMarker);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    public sealed class ScopeMarker;
    public sealed class SecondaryScopeMarker;

    public enum BusScopeOperation { Root, Publish, Send, Request, ExplicitRequest }

    public sealed record RootPublished(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record RootSent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record ScopedPublished(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record ScopedSent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record BusScopeRequest(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record ExplicitBusScopeRequest(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record BusScopeResponse(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record BusScopeCapture(
        Guid CorrelationId,
        BusScopeOperation Operation,
        ScopeMarker? Marker,
        IServiceProvider? ServiceProvider,
        IServiceScope? ServiceScope);

    public sealed class BusScopeObservation(int expected)
    {
        private readonly ConcurrentDictionary<Guid, BusScopeCapture> _captures = new();
        public TaskCompletionSource<BusScopeCapture[]> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(BusScopeCapture capture)
        {
            _captures[capture.CorrelationId] = capture;
            if (_captures.Count == expected)
                Completed.TrySetResult(_captures.Values.OrderBy(value => value.CorrelationId).ToArray());
        }
    }

    public sealed class BusPublishScopeFilter<T>(ScopeMarker marker, BusScopeObservation observation) :
        IFilter<PublishContext<T>> where T : class
    {
        public Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            if (context.Message is ScopedPublished message)
                Record(message.CorrelationId, BusScopeOperation.Publish, context, marker, observation);
            else if (context.Message is BusScopeRequest request)
                Record(request.CorrelationId, BusScopeOperation.Request, context, marker, observation);
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("busPublishScope");
    }

    public sealed class BusSendScopeFilter<T>(ScopeMarker marker, BusScopeObservation observation) :
        IFilter<SendContext<T>> where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            if (context.Message is ScopedSent sent)
                Record(sent.CorrelationId, BusScopeOperation.Send, context, marker, observation);
            else if (context.Message is BusScopeRequest request)
                Record(request.CorrelationId, BusScopeOperation.Request, context, marker, observation);
            else if (context.Message is ExplicitBusScopeRequest explicitRequest)
                Record(explicitRequest.CorrelationId, BusScopeOperation.ExplicitRequest, context, marker, observation);
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("busSendScope");
    }

    private static void Record(
        Guid correlationId,
        BusScopeOperation operation,
        PipeContext context,
        ScopeMarker marker,
        BusScopeObservation observation)
    {
        context.TryGetPayload(out IServiceProvider? provider);
        context.TryGetPayload(out IServiceScope? scope);
        observation.Record(new BusScopeCapture(correlationId, operation, marker, provider, scope));
    }

    public sealed class BusScopeConsumer :
        IConsumer<RootPublished>,
        IConsumer<RootSent>,
        IConsumer<ScopedPublished>,
        IConsumer<ScopedSent>,
        IConsumer<BusScopeRequest>,
        IConsumer<ExplicitBusScopeRequest>
    {
        private readonly BusScopeObservation _observation;

        public BusScopeConsumer(BusScopeObservation observation) => _observation = observation;

        public Task ConsumeAsync(ConsumeContext<RootPublished> context)
        {
            _observation.Record(new BusScopeCapture(
                context.Message.CorrelationId, BusScopeOperation.Root, null, null, null));
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(ConsumeContext<RootSent> context)
        {
            _observation.Record(new BusScopeCapture(
                context.Message.CorrelationId, BusScopeOperation.Root, null, null, null));
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(ConsumeContext<ScopedPublished> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<ScopedSent> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<BusScopeRequest> context) =>
            context.RespondAsync(new BusScopeResponse(context.Message.CorrelationId));

        public Task ConsumeAsync(ConsumeContext<ExplicitBusScopeRequest> context) =>
            context.RespondAsync(new BusScopeResponse(context.Message.CorrelationId));
    }

    public enum MediatorScopeOperation { Send, Publish, Request }

    public sealed record MediatorSent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record MediatorPublished(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record MediatorScopeRequest(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record MediatorScopeResponse(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record MediatorScopeCapture(
        Guid CorrelationId,
        MediatorScopeOperation Operation,
        ScopeMarker FilterMarker,
        ScopeMarker ConsumerMarker);

    public sealed class MediatorScopeObservation(int expected)
    {
        private readonly ConcurrentDictionary<Guid, (MediatorScopeOperation Operation, ScopeMarker Filter)> _filters = new();
        private readonly ConcurrentDictionary<Guid, ScopeMarker> _consumers = new();
        public TaskCompletionSource<MediatorScopeCapture[]> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordFilter(Guid id, MediatorScopeOperation operation, ScopeMarker marker)
        {
            _filters[id] = (operation, marker);
            TryComplete();
        }

        public void RecordConsumer(Guid id, ScopeMarker marker)
        {
            _consumers[id] = marker;
            TryComplete();
        }

        private void TryComplete()
        {
            if (_filters.Count != expected || _consumers.Count != expected)
                return;
            Completed.TrySetResult(_filters.OrderBy(pair => pair.Key).Select(pair => new MediatorScopeCapture(
                pair.Key,
                pair.Value.Operation,
                pair.Value.Filter,
                _consumers[pair.Key])).ToArray());
        }
    }

    public sealed class MediatorPublishScopeFilter<T>(ScopeMarker marker, MediatorScopeObservation observation) :
        IFilter<PublishContext<T>> where T : class
    {
        public Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            if (context.Message is MediatorPublished message)
                observation.RecordFilter(message.CorrelationId, MediatorScopeOperation.Publish, marker);
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("mediatorPublishScope");
    }

    public sealed class MediatorSendScopeFilter<T>(ScopeMarker marker, MediatorScopeObservation observation) :
        IFilter<SendContext<T>> where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            if (context.Message is MediatorSent sent)
                observation.RecordFilter(sent.CorrelationId, MediatorScopeOperation.Send, marker);
            else if (context.Message is MediatorScopeRequest request)
                observation.RecordFilter(request.CorrelationId, MediatorScopeOperation.Request, marker);
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("mediatorSendScope");
    }

    public sealed class MediatorScopeConsumer(ScopeMarker marker, MediatorScopeObservation observation) :
        IConsumer<MediatorSent>, IConsumer<MediatorPublished>, IConsumer<MediatorScopeRequest>
    {
        public Task ConsumeAsync(ConsumeContext<MediatorSent> context)
        {
            observation.RecordConsumer(context.Message.CorrelationId, marker);
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(ConsumeContext<MediatorPublished> context)
        {
            observation.RecordConsumer(context.Message.CorrelationId, marker);
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(ConsumeContext<MediatorScopeRequest> context)
        {
            observation.RecordConsumer(context.Message.CorrelationId, marker);
            return context.RespondAsync(new MediatorScopeResponse(context.Message.CorrelationId));
        }
    }

    public sealed record CascadeMessage(Guid CorrelationId, bool Publish) : CorrelatedBy<Guid>;
    public sealed record CascadeLeaf(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record CascadeResult(
        Guid CorrelationId,
        ScopeMarker ParentMarker,
        SecondaryScopeMarker ParentSecondaryMarker,
        ScopeMarker LeafMarker,
        SecondaryScopeMarker LeafSecondaryMarker);

    public sealed class CascadeObservation
    {
        private readonly object _lock = new();
        private (Guid Id, ScopeMarker Marker, SecondaryScopeMarker Secondary)? _parent;
        private (Guid Id, ScopeMarker Marker, SecondaryScopeMarker Secondary)? _leaf;
        public TaskCompletionSource<CascadeResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Parent(Guid id, ScopeMarker marker, SecondaryScopeMarker secondary)
        {
            lock (_lock) { _parent = (id, marker, secondary); TryComplete(); }
        }

        public void Leaf(Guid id, ScopeMarker marker, SecondaryScopeMarker secondary)
        {
            lock (_lock) { _leaf = (id, marker, secondary); TryComplete(); }
        }

        private void TryComplete()
        {
            if (_parent is { } parent && _leaf is { } leaf)
                Completed.TrySetResult(new CascadeResult(
                    parent.Id, parent.Marker, parent.Secondary, leaf.Marker, leaf.Secondary));
        }
    }

    public sealed class CascadeConsumer(
        IScopedMediator mediator,
        ScopeMarker marker,
        SecondaryScopeMarker secondary,
        CascadeObservation observation) : IConsumer<CascadeMessage>
    {
        public async Task ConsumeAsync(ConsumeContext<CascadeMessage> context)
        {
            observation.Parent(context.Message.CorrelationId, marker, secondary);
            var leaf = new CascadeLeaf(context.Message.CorrelationId);
            if (context.Message.Publish)
                await mediator.PublishAsync(leaf, context.CancellationToken);
            else
                await mediator.SendAsync(leaf, context.CancellationToken);
        }
    }

    public sealed class CascadeLeafConsumer(
        ScopeMarker marker,
        SecondaryScopeMarker secondary,
        CascadeObservation observation) : IConsumer<CascadeLeaf>
    {
        public Task ConsumeAsync(ConsumeContext<CascadeLeaf> context)
        {
            observation.Leaf(context.Message.CorrelationId, marker, secondary);
            return Task.CompletedTask;
        }
    }
}
