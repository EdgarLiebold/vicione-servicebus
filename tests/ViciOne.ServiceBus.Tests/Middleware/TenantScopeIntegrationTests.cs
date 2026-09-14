using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class TenantScopeIntegrationTests
{
    private const string TenantHeader = "X-Tenant-Id";

    [Theory]
    [InlineData(TenantPipeline.PublishOpenConsume)]
    [InlineData(TenantPipeline.PublishTypedConsume)]
    [InlineData(TenantPipeline.TypedPublishTypedConsume)]
    [InlineData(TenantPipeline.SendOpenConsume)]
    [InlineData(TenantPipeline.RetrySendOpenConsume)]
    [InlineData(TenantPipeline.ExecuteActivity)]
    [InlineData(TenantPipeline.ExecuteActivityTyped)]
    [RequirementCoverage("REQ-VSB-TENANT-SCOPE", "header-filter-initializes-scope-before-consumer-or-activity-resolution")]
    public async Task TenantFilter_InitializesTheScopeBeforeDependentComponentsAreResolvedAsync(TenantPipeline pipeline)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new TenantObservation();
        var services = new ServiceCollection()
            .AddScoped<TenantContext>()
            .AddScoped<TenantDbContext>()
            .AddSingleton(observation);
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(timeout, timeout);
            if (pipeline is TenantPipeline.ExecuteActivity or TenantPipeline.ExecuteActivityTyped)
                configuration.AddExecuteActivity<TenantActivity, TenantArguments>();
            else
                configuration.AddConsumer<TenantConsumer>();

            if (pipeline is TenantPipeline.SendOpenConsume or TenantPipeline.RetrySendOpenConsume)
            {
                configuration.AddRequestClient<TenantRequest>(new Uri(
                    $"queue:{DefaultEndpointNameFormatter.Instance.Consumer<TenantConsumer>()}"));
            }

            configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                if (pipeline == TenantPipeline.ExecuteActivity)
                    endpoint.UseExecuteActivityFilter(typeof(TenantExecuteFilter<>), context);
                else if (pipeline == TenantPipeline.ExecuteActivityTyped)
                    endpoint.UseExecuteActivityFilter<TypedTenantExecuteFilter>(context);
                else if (pipeline is TenantPipeline.PublishTypedConsume or TenantPipeline.TypedPublishTypedConsume)
                    endpoint.UseConsumeFilter<TypedTenantConsumeFilter>(context);
                else
                    endpoint.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
            });
            configuration.UsingInMemory((context, bus) =>
            {
                if (pipeline == TenantPipeline.RetrySendOpenConsume)
                    bus.UseMessageRetry(retry => retry.Immediate(2));

                if (pipeline == TenantPipeline.TypedPublishTypedConsume)
                    bus.UsePublishFilter<TypedPublishTenantFilter>(context);
                else if (pipeline is TenantPipeline.PublishOpenConsume or TenantPipeline.PublishTypedConsume)
                    bus.UsePublishFilter(typeof(PublishTenantFilter<>), context);
                else
                    bus.UseSendFilter(typeof(SendTenantFilter<>), context);

                bus.ConfigureEndpoints(context);
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
            Guid correlationId = NewId.NextGuid();
            if (pipeline is TenantPipeline.ExecuteActivity or TenantPipeline.ExecuteActivityTyped)
            {
                var builder = new RoutingSlipBuilder(correlationId);
                builder.AddActivity(
                    "tenant",
                    new Uri($"queue:{DefaultEndpointNameFormatter.Instance.ExecuteActivity<TenantActivity, TenantArguments>()}"),
                    new TenantArguments(correlationId));
                await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
                Assert.True(await harness.Published.AnyAsync<IRoutingSlipCompleted>(cancellationToken));
            }
            else
            {
                IRequestClient<TenantRequest> client = harness.CreateRequestClient<TenantRequest>();
                Response<TenantResponse> response = await client.GetResponseAsync<TenantResponse>(
                    new TenantRequest(correlationId, pipeline == TenantPipeline.RetrySendOpenConsume ? 2 : 0),
                    cancellationToken);
                Assert.Equal(correlationId, response.Message.CorrelationId);
            }

            TenantResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, result.CorrelationId);
            Assert.Equal("tenant-native", result.TenantId);
            Assert.Equal(result.TenantId, result.DbTenantId);
            Assert.Equal(pipeline == TenantPipeline.RetrySendOpenConsume ? 3 : 1, result.AttemptCount);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum TenantPipeline
    {
        PublishOpenConsume,
        PublishTypedConsume,
        TypedPublishTypedConsume,
        SendOpenConsume,
        RetrySendOpenConsume,
        ExecuteActivity,
        ExecuteActivityTyped,
    }

    public sealed record TenantRequest(Guid CorrelationId, int FailureCount) : ICorrelatedBy<Guid>;

    public sealed record TenantResponse(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record TenantArguments(Guid CorrelationId);

    public sealed class TenantContext
    {
        public string TenantId { get; set; } = string.Empty;
    }

    public sealed class TenantDbContext(TenantContext tenantContext)
    {
        public string TenantId { get; } = tenantContext.TenantId;
    }

    public sealed record TenantResult(
        Guid CorrelationId,
        string TenantId,
        string DbTenantId,
        int AttemptCount);

    public sealed class TenantObservation
    {
        private int _attemptCount;

        public TaskCompletionSource<TenantResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int NextAttempt() => Interlocked.Increment(ref _attemptCount);
    }

    public sealed class TenantConsumer(
        TenantContext tenant,
        TenantDbContext database,
        TenantObservation observation) : IConsumer<TenantRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<TenantRequest> context)
        {
            int attempt = observation.NextAttempt();
            if (attempt <= context.Message.FailureCount)
                throw new TenantRetryException($"retry {attempt}");

            observation.Completed.TrySetResult(new TenantResult(
                context.Message.CorrelationId,
                tenant.TenantId,
                database.TenantId,
                attempt));
            await context.RespondAsync(new TenantResponse(context.Message.CorrelationId));
        }
    }

    public sealed class TenantActivity(
        TenantContext tenant,
        TenantDbContext database,
        TenantObservation observation) : IExecuteActivity<TenantArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TenantArguments> context)
        {
            int attempt = observation.NextAttempt();
            observation.Completed.TrySetResult(new TenantResult(
                context.Arguments.CorrelationId,
                tenant.TenantId,
                database.TenantId,
                attempt));
            return Task.FromResult(context.Completed());
        }
    }

    public sealed class PublishTenantFilter<T> : IFilter<PublishContext<T>>
        where T : class
    {
        public Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            context.Headers.Set(TenantHeader, "tenant-native");
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("publishTenant");
    }

    public sealed class TypedPublishTenantFilter : IFilter<PublishContext<TenantRequest>>
    {
        public Task SendAsync(PublishContext<TenantRequest> context, IPipe<PublishContext<TenantRequest>> next)
        {
            context.Headers.Set(TenantHeader, "tenant-native");
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("typedPublishTenant");
    }

    public sealed class SendTenantFilter<T> : IFilter<SendContext<T>>
        where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            context.Headers.Set(TenantHeader, "tenant-native");
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("sendTenant");
    }

    public sealed class TenantConsumeFilter<T>(TenantContext tenant) : IFilter<ConsumeContext<T>>
        where T : class
    {
        public Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            tenant.TenantId = context.Headers.Get<string>(TenantHeader) ?? string.Empty;
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("consumeTenant");
    }

    public sealed class TypedTenantConsumeFilter(TenantContext tenant) : IFilter<ConsumeContext<TenantRequest>>
    {
        public Task SendAsync(ConsumeContext<TenantRequest> context, IPipe<ConsumeContext<TenantRequest>> next)
        {
            tenant.TenantId = context.Headers.Get<string>(TenantHeader) ?? string.Empty;
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("typedConsumeTenant");
    }

    public sealed class TenantExecuteFilter<T>(TenantContext tenant) : IFilter<ExecuteContext<T>>
        where T : class
    {
        public Task SendAsync(ExecuteContext<T> context, IPipe<ExecuteContext<T>> next)
        {
            tenant.TenantId = context.Headers.Get<string>(TenantHeader) ?? string.Empty;
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("executeTenant");
    }

    public sealed class TypedTenantExecuteFilter(TenantContext tenant) : IFilter<ExecuteContext<TenantArguments>>
    {
        public Task SendAsync(ExecuteContext<TenantArguments> context, IPipe<ExecuteContext<TenantArguments>> next)
        {
            tenant.TenantId = context.Headers.Get<string>(TenantHeader) ?? string.Empty;
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("typedExecuteTenant");
    }

    public sealed class TenantRetryException(string message) : Exception(message);
}
