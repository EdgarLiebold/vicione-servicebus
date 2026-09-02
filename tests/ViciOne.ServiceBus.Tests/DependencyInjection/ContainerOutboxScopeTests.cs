using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ContainerOutboxScopeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-OUTBOX-PUBLISH-SCOPE", "consumer-and-outbox-publish-filter-share-scope")]
    public async Task OutboxPublication_UsesTheProducingConsumerScopeAndFlushesExactlyOnce()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new OutboxScopeObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopeMarker>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<OutboxProducingConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "container-outbox-producer");
                configuration.AddConsumer<OutboxPublishedConsumer>();
                configuration.AddConfigureEndpointsCallback((context, name, endpoint) =>
                {
                    if (name == "container-outbox-producer")
                    {
                        endpoint.UseMessageScope(context);
                        endpoint.UseInMemoryOutbox(context);
                    }
                });
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UsePublishFilter(typeof(OutboxScopePublishFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            var command = new OutboxProduce(NewId.NextGuid());
            ISendEndpoint endpoint = await harness.Bus
                .GetSendEndpoint(new Uri("queue:container-outbox-producer"))
                .WaitAsync(timeout, cancellationToken);
            await endpoint.Send(command, cancellationToken);
            OutboxScopeSnapshot snapshot = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(command.CorrelationId, snapshot.CorrelationId);
            Assert.Same(snapshot.ConsumerScope, snapshot.PublishFilterScope);
            Assert.Equal(1, snapshot.ConsumerCount);
            Assert.Equal(1, snapshot.FilterCount);
            Assert.Equal(1, snapshot.DeliveredCount);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Select<OutboxPublished>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<OutboxPublished>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-FAULT-PUBLISH-SCOPE", "fault-publication-bypasses-scoped-publish-filter")]
    public async Task FaultPublication_DoesNotConstructAScopedPublishFilterAndStillArrivesExactlyOnce()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var marker = new FaultFilterMarker();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(marker)
            .AddScoped<ScopeMarker>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<FaultingConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UsePublishFilter(typeof(FaultScopePublishFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            var command = new FaultingCommand(NewId.NextGuid());
            ISendEndpoint endpoint = await harness.Bus
                .GetSendEndpoint(new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<FaultingConsumer>()}"))
                .WaitAsync(timeout, cancellationToken);
            await endpoint.Send(command, cancellationToken);
            IReceivedMessage<Fault<FaultingCommand>> fault = await harness.Consumed
                .SelectAsync<Fault<FaultingCommand>>(cancellationToken)
                .First()
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(command, fault.Context.Message.Message);
            Assert.Contains(fault.Context.Message.Exceptions, exception =>
                exception.ExceptionType == typeof(ExpectedConsumerFailure).FullName);
            Assert.Equal(0, marker.ConstructionCount);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Select<Fault<FaultingCommand>>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<Fault<FaultingCommand>>(SnapshotOnlyToken()));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public sealed class ScopeMarker;

    public sealed record OutboxProduce(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record OutboxPublished(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record OutboxScopeSnapshot(
        Guid CorrelationId,
        ScopeMarker ConsumerScope,
        ScopeMarker PublishFilterScope,
        int ConsumerCount,
        int FilterCount,
        int DeliveredCount);

    public sealed class OutboxScopeObservation
    {
        private readonly object _lock = new();
        private Guid _correlationId;
        private ScopeMarker? _consumerScope;
        private ScopeMarker? _publishFilterScope;
        private int _consumerCount;
        private int _filterCount;
        private int _deliveredCount;

        public TaskCompletionSource<OutboxScopeSnapshot> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordConsumer(Guid correlationId, ScopeMarker scope)
        {
            lock (_lock)
            {
                _correlationId = correlationId;
                _consumerScope = scope;
                _consumerCount++;
                TryComplete();
            }
        }

        public void RecordFilter(ScopeMarker scope)
        {
            lock (_lock)
            {
                _publishFilterScope = scope;
                _filterCount++;
                TryComplete();
            }
        }

        public void RecordDelivery()
        {
            lock (_lock)
            {
                _deliveredCount++;
                TryComplete();
            }
        }

        private void TryComplete()
        {
            if (_consumerScope is null || _publishFilterScope is null || _deliveredCount == 0)
                return;

            Completed.TrySetResult(new OutboxScopeSnapshot(
                _correlationId,
                _consumerScope,
                _publishFilterScope,
                _consumerCount,
                _filterCount,
                _deliveredCount));
        }
    }

    public sealed class OutboxProducingConsumer(
        ScopeMarker scope,
        OutboxScopeObservation observation) : IConsumer<OutboxProduce>
    {
        public async Task Consume(ConsumeContext<OutboxProduce> context)
        {
            observation.RecordConsumer(context.Message.CorrelationId, scope);
            await context.Publish(new OutboxPublished(context.Message.CorrelationId));
        }
    }

    public sealed class OutboxPublishedConsumer(OutboxScopeObservation observation) : IConsumer<OutboxPublished>
    {
        public Task Consume(ConsumeContext<OutboxPublished> context)
        {
            observation.RecordDelivery();
            return Task.CompletedTask;
        }
    }

    public sealed class OutboxScopePublishFilter<T>(
        ScopeMarker scope,
        OutboxScopeObservation observation) : IFilter<PublishContext<T>>
        where T : class
    {
        public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            if (context.Message is OutboxPublished)
                observation.RecordFilter(scope);

            await next.Send(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("outboxScopePublish");
    }

    public sealed record FaultingCommand(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class FaultingConsumer :
        IConsumer<FaultingCommand>,
        IConsumer<Fault<FaultingCommand>>
    {
        public Task Consume(ConsumeContext<FaultingCommand> context) =>
            throw new ExpectedConsumerFailure();

        public Task Consume(ConsumeContext<Fault<FaultingCommand>> context) => Task.CompletedTask;
    }

    public sealed class FaultFilterMarker
    {
        private int _constructionCount;

        public int ConstructionCount => Volatile.Read(ref _constructionCount);

        public void Constructed() => Interlocked.Increment(ref _constructionCount);
    }

    public sealed class FaultScopePublishFilter<T> : IFilter<PublishContext<T>>
        where T : class
    {
        public FaultScopePublishFilter(ScopeMarker _, FaultFilterMarker marker) => marker.Constructed();

        public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next) => next.Send(context);

        public void Probe(ProbeContext context) => context.CreateFilterScope("faultScopePublish");
    }

    public sealed class ExpectedConsumerFailure : Exception;
}
