using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class ExplicitRedeliveryIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EXPLICIT-REDELIVERY", "fallback-scheduler-redelivers-with-count-and-callback-metadata")]
    public async Task MissingPayload_UsesTheConfiguredSchedulerAndPreservesRedeliveryMetadataAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new RedeliveryObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddDelayedMessageScheduler();
                configuration.AddConsumer<ExplicitRedeliveryConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureDelayedMessageScheduler();
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);

        try
        {
            var message = new ExplicitRedeliveryMessage(NewId.NextGuid());
            await harness.Bus.PublishAsync(message, cancellationToken);

            RedeliveryResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message.CorrelationId, result.CorrelationId);
            Assert.Equal("callback-applied", result.CallbackValue);
            Assert.Equal([0, 1], observation.RedeliveryCounts);
            Assert.Equal(1, observation.CallbackCount);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record ExplicitRedeliveryMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record RedeliveryResult(Guid CorrelationId, string? CallbackValue);

    public sealed class RedeliveryObservation
    {
        private int _callbackCount;

        public ConcurrentQueue<int> RedeliveryCounts { get; } = new();

        public int CallbackCount => Volatile.Read(ref _callbackCount);

        public TaskCompletionSource<RedeliveryResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordCallback() => Interlocked.Increment(ref _callbackCount);
    }

    public sealed class ExplicitRedeliveryConsumer(RedeliveryObservation observation) : IConsumer<ExplicitRedeliveryMessage>
    {
        private const string CallbackHeader = "ViciOne-Explicit-Redelivery-Callback";

        public async Task ConsumeAsync(ConsumeContext<ExplicitRedeliveryMessage> context)
        {
            int redeliveryCount = context.Advanced().GetRedeliveryCount();
            observation.RedeliveryCounts.Enqueue(redeliveryCount);

            if (redeliveryCount == 0)
            {
                await context.RedeliverAsync(
                    TimeSpan.Zero,
                    (_, sendContext) =>
                    {
                        observation.RecordCallback();
                        sendContext.Headers.Set(CallbackHeader, "callback-applied");
                    },
                    context.CancellationToken);
                return;
            }

            observation.Completed.TrySetResult(new RedeliveryResult(
                context.Message.CorrelationId,
                context.Headers.Get<string>(CallbackHeader)));
        }
    }
}
