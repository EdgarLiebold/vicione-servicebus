using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ReceiveEndpointDependencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEPENDENCY", "dependent-endpoint-waits-then-drains-exactly-once")]
    public async Task DependentEndpoint_WaitsForReadinessThenConsumesTheQueuedMessageExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var dependency = new ControlledDependency();
        var observation = new DependencyObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IReceiveEndpointDependency>(dependency)
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DependentConsumer, DependentConsumerDefinition>();
            })
            .AddOptions<ViciOneServiceBusHostOptions>()
            .Configure(options => options.WaitUntilStarted = false)
            .Services
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = NewId.NextGuid();
            await harness.Bus.PublishAsync(new DependentMessage(correlationId), cancellationToken);
            IPublishedMessage<DependentMessage> published = await harness.Published
                .SelectAsync<DependentMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            HealthReport blockedHealth = await provider.GetRequiredService<HealthCheckService>()
                .CheckHealthAsync(cancellationToken);

            Assert.Equal(correlationId, published.Context.Message.CorrelationId);
            Assert.NotEqual(HealthStatus.Healthy, blockedHealth.Status);
            Assert.False(observation.Consumed.Task.IsCompleted);

            dependency.Release();
            Guid consumed = await observation.Consumed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            using var terminal = new CancellationTokenSource();
            terminal.Cancel();
            IReceivedMessage<DependentMessage>[] messages = harness.Consumed
                .Select<DependentMessage>(terminal.Token)
                .ToArray();

            Assert.Equal(correlationId, consumed);
            IReceivedMessage<DependentMessage> message = Assert.Single(messages);
            Assert.Equal(correlationId, message.Context.Message.CorrelationId);
            Assert.Null(message.Exception);
        }
        finally
        {
            dependency.Release();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record DependentMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class ControlledDependency : IReceiveEndpointDependency
    {
        private readonly TaskCompletionSource _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Ready => _ready.Task;

        public void Release() => _ready.TrySetResult();
    }

    public sealed class DependencyObservation
    {
        public TaskCompletionSource<Guid> Consumed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class DependentConsumer(DependencyObservation observation) : IConsumer<DependentMessage>
    {
        public Task ConsumeAsync(ConsumeContext<DependentMessage> context)
        {
            observation.Consumed.TrySetResult(context.Message.CorrelationId);
            return Task.CompletedTask;
        }
    }

    public sealed class DependentConsumerDefinition(IReceiveEndpointDependency dependency) :
        ConsumerDefinition<DependentConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<DependentConsumer> consumerConfigurator,
            IRegistrationContext context) => endpointConfigurator.AddDependency(dependency);
    }
}
