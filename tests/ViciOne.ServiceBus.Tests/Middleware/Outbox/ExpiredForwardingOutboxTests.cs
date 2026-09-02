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
    public async Task ExpiredMessage_IsDiscardedBeforePersistentOutboxStorage()
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

        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<ForwardMessage>(async context =>
            {
                DateTime expiration = Assert.IsType<DateTime>(context.ExpirationTime).ToUniversalTime();
                context.SetTimeProvider(new FakeTimeProvider(new DateTimeOffset(expiration).AddMinutes(1)));
                ISendEndpoint endpoint = await context.GetSendEndpoint(forwardAddress).ConfigureAwait(false);
                var outboxEndpoint = new PersistentOutboxSendEndpoint(outbox, endpoint);

                await context.Forward(outboxEndpoint).ConfigureAwait(false);
                sourceCompleted.TrySetResult();
            });

        try
        {
            await harness.Start(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(
                    new ForwardMessage { Value = "expired-outbox" },
                    context => context.TimeToLive = TimeSpan.FromMinutes(5),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);
            await sourceCompleted.Task.WaitAsync(operationTimeout, cancellationToken);

            Assert.Equal(0, outbox.AddSendCount);
        }
        finally
        {
            await harness.Stop().WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private sealed class RecordingOutboxSendContext : OutboxSendContext
    {
        private int _addSendCount;

        public int AddSendCount => Volatile.Read(ref _addSendCount);

        public Task AddSend<T>(SendContext<T> context)
            where T : class
        {
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
