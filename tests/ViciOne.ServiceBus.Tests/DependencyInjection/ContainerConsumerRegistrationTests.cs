using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ContainerConsumerRegistrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUMER-LIFETIME", "dependency-used-before-exact-scope-disposal")]
    public async Task ScopedDependency_IsUsedBeforeAndDisposedAfterTheConsumerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new LifecycleObservation(expectedConsumers: 1);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<LifecycleDependency>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<LifecycleConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var message = new RegistrationMessage(NewId.NextGuid(), "one");
            await harness.Bus.PublishAsync(message, cancellationToken);
            LifecycleResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);
            await observation.Disposed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, Assert.Single(result.Messages));
            Assert.Single(result.Dependencies);
            Assert.True(result.Dependencies[0].EndpointMatchesConsumeContext);
            Assert.True(result.Dependencies[0].UsedBeforeDisposal);
            Assert.Equal(1, result.Dependencies[0].DisposeCount);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUMER-LIFETIME", "endpoint-service-scope-shared-by-two-consumers")]
    public async Task EndpointServiceScope_IsSharedByBothConsumersThenDisposedExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new LifecycleObservation(expectedConsumers: 2);
        var services = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<LifecycleDependency>();
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(timeout, timeout);
            configuration.AddConsumer<LifecycleConsumer>();
            configuration.AddConsumer<SecondLifecycleConsumer>();
            configuration.UsingInMemory((context, bus) =>
            {
                bus.ReceiveEndpoint("shared-service-scope", endpoint =>
                {
                    endpoint.UseServiceScope(context);
                    endpoint.ConfigureConsumer<LifecycleConsumer>(context);
                    endpoint.ConfigureConsumer<SecondLifecycleConsumer>(context);
                });
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var message = new RegistrationMessage(NewId.NextGuid(), "shared");
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(new Uri("queue:shared-service-scope"), TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            await endpoint.SendAsync(message, cancellationToken);
            LifecycleResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);
            await observation.Disposed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, result.Messages.Length);
            Assert.All(result.Messages, actual => Assert.Equal(message, actual));
            LifecycleDependency dependency = Assert.Single(result.Dependencies);
            Assert.True(dependency.EndpointMatchesConsumeContext);
            Assert.True(dependency.UsedBeforeDisposal);
            Assert.Equal(1, dependency.DisposeCount);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUMER-REGISTRATION", "direct-service-collection-registration")]
    public async Task DirectContainerConsumerRegistration_IsResolvedByConfigureConsumerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var delivered = new TaskCompletionSource<RegistrationMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var services = new ServiceCollection().AddSingleton(delivered);
        services.RegisterConsumer<DirectlyRegisteredConsumer>();
        services.AddViciOneServiceBusTestHarness(configuration => configuration.UsingInMemory((context, bus) =>
            bus.ReceiveEndpoint("direct-container-consumer", endpoint =>
                endpoint.ConfigureConsumer<DirectlyRegisteredConsumer>(context))));
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var expected = new RegistrationMessage(NewId.NextGuid(), "direct");
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(new Uri("queue:direct-container-consumer"), TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            await endpoint.SendAsync(expected, cancellationToken);

            Assert.Equal(expected, await delivered.Task.WaitAsync(timeout, cancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ENDPOINT-CONFIGURATION", "global-configurator-disables-fault-publication")]
    public async Task GlobalEndpointConfigurator_DisablesFaultPublicationOnEveryConfiguredEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var globalConfiguration = new DisableFaultPublicationConfiguration();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IConfigureReceiveEndpoint>(globalConfiguration)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<GloballyFaultingConsumer>();
                configuration.AddConsumer<GlobalControlConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            var faulting = new GlobalFaultMessage(NewId.NextGuid());
            var control = new GlobalControlMessage(NewId.NextGuid());
            await harness.Bus.PublishAsync(faulting, cancellationToken);
            await harness.Bus.PublishAsync(control, cancellationToken);
            IConsumedMessage<GlobalFaultMessage> failed = await harness.Consumed
                .SelectAsync<GlobalFaultMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            IConsumedMessage<GlobalControlMessage> consumed = await harness.Consumed
                .SelectAsync<GlobalControlMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.IsType<ExpectedGlobalFailure>(failed.Exception);
            Assert.Null(consumed.Exception);
            Assert.Equal(2, globalConfiguration.EndpointNames.Length);
            Assert.Contains(DefaultEndpointNameFormatter.Instance.Consumer<GloballyFaultingConsumer>(),
                globalConfiguration.EndpointNames);
            Assert.Contains(DefaultEndpointNameFormatter.Instance.Consumer<GlobalControlConsumer>(),
                globalConfiguration.EndpointNames);
            Assert.Empty(harness.Published.Snapshot<Fault<GlobalFaultMessage>>());
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-FILTER-SCOPE", "consume-send-and-next-consume-scope-boundaries")]
    public async Task ConsumeAndSendFilters_ShareOnlyTheOwningDeliveryScopeAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new FilterScopeObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<FilterScopeMarker>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ScopeAConsumer>();
                configuration.AddConsumer<ScopeBConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseConsumeFilter(typeof(ScopeConsumeFilter<>), context);
                    bus.UseSendFilter(typeof(ScopeSendFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var message = new ScopeAMessage(NewId.NextGuid());
            await harness.Bus.PublishAsync(message, cancellationToken);
            FilterScopeResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message.CorrelationId, result.CorrelationId);
            Assert.Same(result.AConsumeFilter, result.AConsumer);
            Assert.Same(result.AConsumer, result.BSendFilter);
            Assert.Same(result.BConsumeFilter, result.BConsumer);
            Assert.NotSame(result.AConsumer, result.BConsumer);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-FILTER-LAYERS", "raw-message-outside-and-consumer-layers-inside-one-scope")]
    public async Task ConsumerFilterLayers_ExposeTheExactContainerScopeBoundaryAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new LayerScopeObservation();
        var services = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<LayerScopeMarker>();
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(timeout, timeout);
            configuration.AddConsumer<LayerScopeConsumer>();
            configuration.UsingInMemory((context, bus) => bus.ReceiveEndpoint(
                "container-filter-layers",
                endpoint => endpoint.ConfigureConsumer<LayerScopeConsumer>(context, consumer =>
                {
                    consumer.Message<LayerScopeMessage>(message =>
                        message.UseFilter(new RawMessageLayerFilter(observation)));
                    consumer.UseFilter(new ConsumerLayerFilter(observation));
                    consumer.ConsumerMessage<LayerScopeMessage>(message =>
                        message.UseFilter(new ConsumerMessageLayerFilter(observation)));
                })));
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var expected = new LayerScopeMessage(NewId.NextGuid());
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(new Uri("queue:container-filter-layers"), TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            await endpoint.SendAsync(expected, cancellationToken);
            LayerScopeResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(expected.CorrelationId, result.CorrelationId);
            Assert.False(result.RawMessageHadServiceProvider);
            Assert.Same(result.ConsumerScope, result.ConsumerMessageScope);
            Assert.Same(result.ConsumerScope.ServiceProvider, result.ConsumerInstanceScope);
            Assert.Same(result.ConsumerMarker, result.ConsumerInstanceMarker);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record RegistrationMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    public sealed class LifecycleObservation(int expectedConsumers)
    {
        private readonly object _lock = new();
        private readonly List<LifecycleDependency> _dependencies = [];
        private readonly List<RegistrationMessage> _messages = [];
        private int _disposed;

        public TaskCompletionSource<LifecycleResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Disposed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(RegistrationMessage message, LifecycleDependency dependency)
        {
            lock (_lock)
            {
                if (!_dependencies.Contains(dependency))
                    _dependencies.Add(dependency);
                _messages.Add(message);
                if (_messages.Count == expectedConsumers)
                    Completed.TrySetResult(new LifecycleResult(_messages.ToArray(), _dependencies.ToArray()));
            }
        }

        public void RecordDisposed()
        {
            if (Interlocked.Increment(ref _disposed) == 1)
                Disposed.TrySetResult();
        }
    }

    public sealed record LifecycleResult(
        RegistrationMessage[] Messages,
        LifecycleDependency[] Dependencies);

    public sealed class LifecycleDependency : IDisposable
    {
        private readonly LifecycleObservation _observation;
        private int _disposeCount;

        public LifecycleDependency(
            ConsumeContext consumeContext,
            ISendEndpointProvider sendEndpointProvider,
            LifecycleObservation observation)
        {
            EndpointMatchesConsumeContext = ReferenceEquals(consumeContext, sendEndpointProvider);
            _observation = observation;
        }

        public bool EndpointMatchesConsumeContext { get; }

        public bool UsedBeforeDisposal { get; private set; }

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Use()
        {
            ObjectDisposedException.ThrowIf(DisposeCount != 0, this);
            UsedBeforeDisposal = true;
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
            _observation.RecordDisposed();
        }
    }

    public sealed class LifecycleConsumer(
        LifecycleDependency dependency,
        LifecycleObservation observation) : IConsumer<RegistrationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RegistrationMessage> context)
        {
            dependency.Use();
            observation.Record(context.Message, dependency);
            return Task.CompletedTask;
        }
    }

    public sealed class SecondLifecycleConsumer(
        LifecycleDependency dependency,
        LifecycleObservation observation) : IConsumer<RegistrationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RegistrationMessage> context)
        {
            dependency.Use();
            observation.Record(context.Message, dependency);
            return Task.CompletedTask;
        }
    }

    public sealed class DirectlyRegisteredConsumer(
        TaskCompletionSource<RegistrationMessage> delivered) : IConsumer<RegistrationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RegistrationMessage> context)
        {
            delivered.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed record GlobalFaultMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record GlobalControlMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class DisableFaultPublicationConfiguration : IConfigureReceiveEndpoint
    {
        private readonly ConcurrentQueue<string> _endpointNames = new();

        public string[] EndpointNames => _endpointNames.Order(StringComparer.Ordinal).ToArray();

        public void Configure(string? name, IReceiveEndpointConfigurator configurator)
        {
            _endpointNames.Enqueue(name ?? "<unnamed>");
            configurator.PublishFaults = false;
        }
    }

    public sealed class GloballyFaultingConsumer : IConsumer<GlobalFaultMessage>
    {
        public Task ConsumeAsync(ConsumeContext<GlobalFaultMessage> context) =>
            Task.FromException(new ExpectedGlobalFailure());
    }

    public sealed class GlobalControlConsumer : IConsumer<GlobalControlMessage>
    {
        public Task ConsumeAsync(ConsumeContext<GlobalControlMessage> context) => Task.CompletedTask;
    }

    public sealed class ExpectedGlobalFailure : Exception;

    public sealed record ScopeAMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record ScopeBMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class FilterScopeMarker;

    public sealed record FilterScopeResult(
        Guid CorrelationId,
        FilterScopeMarker AConsumeFilter,
        FilterScopeMarker AConsumer,
        FilterScopeMarker BSendFilter,
        FilterScopeMarker BConsumeFilter,
        FilterScopeMarker BConsumer);

    public sealed class FilterScopeObservation
    {
        private readonly object _lock = new();
        private FilterScopeMarker? _aConsumeFilter;
        private FilterScopeMarker? _aConsumer;
        private FilterScopeMarker? _bSendFilter;
        private FilterScopeMarker? _bConsumeFilter;
        private FilterScopeMarker? _bConsumer;
        private Guid _correlationId;

        public TaskCompletionSource<FilterScopeResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordAConsume(FilterScopeMarker marker)
        {
            lock (_lock)
            {
                _aConsumeFilter = marker;
                TryComplete();
            }
        }

        public void RecordAConsumer(Guid correlationId, FilterScopeMarker marker)
        {
            lock (_lock)
            {
                _correlationId = correlationId;
                _aConsumer = marker;
                TryComplete();
            }
        }

        public void RecordBSend(FilterScopeMarker marker)
        {
            lock (_lock)
            {
                _bSendFilter = marker;
                TryComplete();
            }
        }

        public void RecordBConsume(FilterScopeMarker marker)
        {
            lock (_lock)
            {
                _bConsumeFilter = marker;
                TryComplete();
            }
        }

        public void RecordBConsumer(FilterScopeMarker marker)
        {
            lock (_lock)
            {
                _bConsumer = marker;
                TryComplete();
            }
        }

        private void TryComplete()
        {
            if (_aConsumeFilter is not null && _aConsumer is not null && _bSendFilter is not null &&
                _bConsumeFilter is not null && _bConsumer is not null)
            {
                Completed.TrySetResult(new FilterScopeResult(
                    _correlationId,
                    _aConsumeFilter,
                    _aConsumer,
                    _bSendFilter,
                    _bConsumeFilter,
                    _bConsumer));
            }
        }
    }

    public sealed class ScopeConsumeFilter<T>(
        FilterScopeMarker marker,
        FilterScopeObservation observation) : IFilter<ConsumeContext<T>>
        where T : class
    {
        public Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            if (context.Message is ScopeAMessage)
                observation.RecordAConsume(marker);
            else if (context.Message is ScopeBMessage)
                observation.RecordBConsume(marker);
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("scopeConsume");
    }

    public sealed class ScopeSendFilter<T>(
        FilterScopeMarker marker,
        FilterScopeObservation observation) : IFilter<SendContext<T>>
        where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            if (context.Message is ScopeBMessage)
                observation.RecordBSend(marker);
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("scopeSend");
    }

    public sealed class ScopeAConsumer(
        FilterScopeMarker marker,
        FilterScopeObservation observation) : IConsumer<ScopeAMessage>
    {
        public async Task ConsumeAsync(ConsumeContext<ScopeAMessage> context)
        {
            observation.RecordAConsumer(context.Message.CorrelationId, marker);
            await context.Advanced().SendAsync(new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<ScopeBConsumer>()}"),
                new ScopeBMessage(context.Message.CorrelationId));
        }
    }

    public sealed class ScopeBConsumer(
        FilterScopeMarker marker,
        FilterScopeObservation observation) : IConsumer<ScopeBMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ScopeBMessage> context)
        {
            observation.RecordBConsumer(marker);
            return Task.CompletedTask;
        }
    }

    public sealed record LayerScopeMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class LayerScopeMarker;

    public sealed record LayerScopeResult(
        Guid CorrelationId,
        bool RawMessageHadServiceProvider,
        IServiceScope ConsumerScope,
        IServiceScope ConsumerMessageScope,
        IServiceProvider ConsumerInstanceScope,
        LayerScopeMarker ConsumerMarker,
        LayerScopeMarker ConsumerInstanceMarker);

    public sealed class LayerScopeObservation
    {
        private readonly object _lock = new();
        private bool? _rawHadProvider;
        private IServiceScope? _consumerScope;
        private IServiceScope? _consumerMessageScope;
        private IServiceProvider? _consumerInstanceScope;
        private LayerScopeMarker? _consumerMarker;
        private LayerScopeMarker? _consumerInstanceMarker;
        private Guid _correlationId;

        public TaskCompletionSource<LayerScopeResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordRaw(bool hadProvider)
        {
            lock (_lock)
            {
                _rawHadProvider = hadProvider;
                TryComplete();
            }
        }

        public void RecordConsumer(IServiceScope scope, LayerScopeMarker marker)
        {
            lock (_lock)
            {
                _consumerScope = scope;
                _consumerMarker = marker;
                TryComplete();
            }
        }

        public void RecordConsumerMessage(IServiceScope scope)
        {
            lock (_lock)
            {
                _consumerMessageScope = scope;
                TryComplete();
            }
        }

        public void RecordInstance(
            Guid correlationId,
            IServiceProvider scope,
            LayerScopeMarker marker)
        {
            lock (_lock)
            {
                _correlationId = correlationId;
                _consumerInstanceScope = scope;
                _consumerInstanceMarker = marker;
                TryComplete();
            }
        }

        private void TryComplete()
        {
            if (_rawHadProvider.HasValue && _consumerScope is not null && _consumerMessageScope is not null &&
                _consumerInstanceScope is not null && _consumerMarker is not null && _consumerInstanceMarker is not null)
            {
                Completed.TrySetResult(new LayerScopeResult(
                    _correlationId,
                    _rawHadProvider.Value,
                    _consumerScope,
                    _consumerMessageScope,
                    _consumerInstanceScope,
                    _consumerMarker,
                    _consumerInstanceMarker));
            }
        }
    }

    public sealed class RawMessageLayerFilter(LayerScopeObservation observation) :
        IFilter<ConsumeContext<LayerScopeMessage>>
    {
        public Task SendAsync(
            ConsumeContext<LayerScopeMessage> context,
            IPipe<ConsumeContext<LayerScopeMessage>> next)
        {
            observation.RecordRaw(context.TryGetPayload(out IServiceProvider? _));
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("rawMessageLayer");
    }

    public sealed class ConsumerLayerFilter(LayerScopeObservation observation) :
        IFilter<ConsumerConsumeContext<LayerScopeConsumer>>
    {
        public Task SendAsync(
            ConsumerConsumeContext<LayerScopeConsumer> context,
            IPipe<ConsumerConsumeContext<LayerScopeConsumer>> next)
        {
            IServiceScope scope = context.GetPayload<IServiceScope>();
            observation.RecordConsumer(
                scope,
                scope.ServiceProvider.GetRequiredService<LayerScopeMarker>());
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("consumerLayer");
    }

    public sealed class ConsumerMessageLayerFilter(LayerScopeObservation observation) :
        IFilter<ConsumerConsumeContext<LayerScopeConsumer, LayerScopeMessage>>
    {
        public Task SendAsync(
            ConsumerConsumeContext<LayerScopeConsumer, LayerScopeMessage> context,
            IPipe<ConsumerConsumeContext<LayerScopeConsumer, LayerScopeMessage>> next)
        {
            observation.RecordConsumerMessage(context.GetPayload<IServiceScope>());
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("consumerMessageLayer");
    }

    public sealed class LayerScopeConsumer(
        IServiceProvider scope,
        LayerScopeMarker marker,
        LayerScopeObservation observation) : IConsumer<LayerScopeMessage>
    {
        public Task ConsumeAsync(ConsumeContext<LayerScopeMessage> context)
        {
            observation.RecordInstance(context.Message.CorrelationId, scope, marker);
            return Task.CompletedTask;
        }
    }
}
