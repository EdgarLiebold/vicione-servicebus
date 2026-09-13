using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ScopedPipelineTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPE", "consumer-middleware-exposes-owning-service-scope")]
    public async Task ConsumerMiddleware_ExposesTheSameServiceScopeUsedToResolveTheConsumerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ScopeObservation();
        var configurationObserver = new ScopeFilterConfigurationObserver();
        await using ServiceProvider provider = new ServiceCollection()
            .AddScoped<ScopeMarker>()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ScopedRequestConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConnectConsumerConfigurationObserver(configurationObserver);
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
            Guid correlationId = NewId.NextGuid();
            IRequestClient<ScopedRequest> client = harness.CreateRequestClient<ScopedRequest>();

            Response<ScopedResponse> response = await client.GetResponseAsync<ScopedResponse>(
                new ScopedRequest(correlationId),
                cancellationToken);
            ScopeResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(correlationId, result.CorrelationId);
            Assert.Same(result.FilterMarker, result.ConsumerMarker);
            Assert.Same(result.FilterScopeProvider, result.ConsumerScopeProvider);
            Assert.NotSame(provider, result.FilterScopeProvider);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-FILTER", "excluded-implemented-interface-does-not-duplicate-publish-filter")]
    public async Task ExcludedImplementedInterface_DoesNotApplyThePublishFilterTwiceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new FilterObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<FilteredRequestConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UsePublishFilter(typeof(CountingPublishFilter<>), context);
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
            Guid correlationId = NewId.NextGuid();
            IRequestClient<FilteredRequest> client = harness.CreateRequestClient<FilteredRequest>();

            Response<FilteredResponse> response = await client.GetResponseAsync<FilteredResponse>(
                new FilteredRequest(correlationId),
                cancellationToken);
            FilterResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(correlationId, result.CorrelationId);
            Assert.Equal(1, result.FilterCount);
            Assert.Equal(1, observation.RequestInvocations);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record ScopedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ScopedResponse(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class ScopeMarker;

    public sealed record ScopeResult(
        Guid CorrelationId,
        ScopeMarker FilterMarker,
        ScopeMarker ConsumerMarker,
        IServiceProvider FilterScopeProvider,
        IServiceProvider ConsumerScopeProvider);

    public sealed class ScopeObservation
    {
        public TaskCompletionSource<ScopeResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ScopeFilterConfigurationObserver : IConsumerConfigurationObserver
    {
        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
            where TConsumer : class
        {
        }

        public void ConsumerMessageConfigured<TConsumer, TMessage>(
            IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
            where TConsumer : class
            where TMessage : class =>
            configurator.UseFilter(new ScopeCaptureFilter<TConsumer, TMessage>());
    }

    public sealed class ScopeCaptureFilter<TConsumer, TMessage> :
        IFilter<ConsumerConsumeContext<TConsumer, TMessage>>
        where TConsumer : class
        where TMessage : class
    {
        public async Task SendAsync(
            ConsumerConsumeContext<TConsumer, TMessage> context,
            IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        {
            IServiceScope scope = context.GetPayload<IServiceScope>();
            var marker = scope.ServiceProvider.GetRequiredService<ScopeMarker>();
            context.GetOrAddPayload(() => new ScopeFilterPayload(marker, scope.ServiceProvider));

            await next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("scopeCapture");
    }

    public sealed record ScopeFilterPayload(ScopeMarker Marker, IServiceProvider ScopeProvider);

    public sealed class ScopedRequestConsumer(
        ScopeMarker marker,
        IServiceProvider scopeProvider,
        ScopeObservation observation) : IConsumer<ScopedRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<ScopedRequest> context)
        {
            ScopeFilterPayload payload = context.GetPayload<ScopeFilterPayload>();
            observation.Completed.TrySetResult(new ScopeResult(
                context.Message.CorrelationId,
                payload.Marker,
                marker,
                payload.ScopeProvider,
                scopeProvider));
            await context.RespondAsync(new ScopedResponse(context.Message.CorrelationId));
        }
    }

    public sealed record FilteredRequest(Guid CorrelationId) : IFilteredRequest, CorrelatedBy<Guid>;

    [ExcludeFromImplementedTypes]
    public interface IFilteredRequest;

    public sealed record FilteredResponse(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record FilterResult(Guid CorrelationId, int FilterCount);

    public sealed class FilterObservation
    {
        private int _requestInvocations;

        public int RequestInvocations => Volatile.Read(ref _requestInvocations);

        public TaskCompletionSource<FilterResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordRequest() => Interlocked.Increment(ref _requestInvocations);
    }

    public sealed class CountingPublishFilter<T>(FilterObservation observation) : IFilter<PublishContext<T>>
        where T : class
    {
        public Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            int count = (context.TryGetHeader("X-FilterCount", out int? existing) ? existing : 0) ?? 0;
            count++;
            context.Headers.Set("X-FilterCount", count);
            if (context.Message is FilteredRequest)
                observation.RecordRequest();

            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("countingPublish");
    }

    public sealed class FilteredRequestConsumer(FilterObservation observation) : IConsumer<FilteredRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<FilteredRequest> context)
        {
            observation.Completed.TrySetResult(new FilterResult(
                context.Message.CorrelationId,
                context.Advanced().TryGetHeader("X-FilterCount", out int? count) ? count ?? 0 : 0));
            await context.RespondAsync(new FilteredResponse(context.Message.CorrelationId));
        }
    }
}
