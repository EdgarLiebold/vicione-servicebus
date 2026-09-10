using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using PersistentOutboxSendEndpoint = ViciOne.ServiceBus.Middleware.Outbox.OutboxSendEndpoint;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class ExpiredForwardingOutboxTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "expired-forward-discarded-before-persistent-outbox")]
    public async Task ExpiredMessage_IsDiscardedBeforePersistentOutboxStorageAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"message-forwarding-outbox-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var sourceCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outbox = new RecordingOutboxSendContext();
        Uri forwardAddress = new(harness.BaseAddress, "persistent-outbox-forward");

        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<ForwardMessage>(async context =>
            {
                DateTimeOffset expiration = Assert.IsType<DateTimeOffset>(context.ExpirationTime).ToUniversalTime();
                context.SetTimeProvider(new FakeTimeProvider(expiration.AddMinutes(1)));
                ISendEndpoint endpoint = await context.Advanced().GetSendEndpointAsync(forwardAddress).ConfigureAwait(false);
                var outboxEndpoint = new PersistentOutboxSendEndpoint(outbox, endpoint);

                await context.ForwardAsync(outboxEndpoint).ConfigureAwait(false);
                sourceCompleted.TrySetResult();
            });

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(
                    new ForwardMessage { Value = "expired-outbox" },
                    context => context.TimeToLive = TimeSpan.FromMinutes(5),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);
            await sourceCompleted.Task.WaitAsync(operationTimeout, cancellationToken);

            Assert.Equal(0, outbox.AddSendCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private sealed class RecordingOutboxSendContext : OutboxSendContext
    {
        private int _addSendCount;

        public int AddSendCount => Volatile.Read(ref _addSendCount);

        public Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _addSendCount);
            return Task.CompletedTask;
        }

        public object? GetService(Type serviceType) => null;
    }

    private sealed class ForwardMessage
    {
        public string Value { get; init; } = string.Empty;
    }
}
