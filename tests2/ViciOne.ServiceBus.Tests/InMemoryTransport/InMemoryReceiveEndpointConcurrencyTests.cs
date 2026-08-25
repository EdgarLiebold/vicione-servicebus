using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryReceiveEndpointConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "one-hundred-simultaneous-deliveries")]
    public async Task ConfiguredEndpointConcurrency_AdmitsAllOneHundredDeliveriesBeforeAnyCompletes()
    {
        const int concurrencyLimit = 100;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var allEntered = NewSignal();
        var allCompleted = NewSignal();
        var release = NewSignal();
        var active = 0;
        var completed = 0;
        var maximum = 0;
        using var harness = new InMemoryTestHarness($"endpoint-concurrency-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.PrefetchCount = concurrencyLimit;
            configurator.ConcurrentMessageLimit = concurrencyLimit;
            configurator.Handler<ConcurrentDelivery>(async _ =>
            {
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, current);
                if (current == concurrencyLimit)
                    allEntered.TrySetResult();

                await release.Task;

                Interlocked.Decrement(ref active);
                if (Interlocked.Increment(ref completed) == concurrencyLimit)
                    allCompleted.TrySetResult();
            });
        };

        try
        {
            await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
            Task[] sends = Enumerable.Range(0, concurrencyLimit)
                .Select(index => harness.InputQueueSendEndpoint.Send(
                    new ConcurrentDelivery(index),
                    cancellationToken))
                .ToArray();

            string? saturationFailure = null;
            try
            {
                await allEntered.Task.WaitAsync(timeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                saturationFailure =
                    $"Only {Volatile.Read(ref maximum)} of {concurrencyLimit} deliveries were simultaneously active.";
            }
            finally
            {
                release.TrySetResult();
            }

            await Task.WhenAll(sends).WaitAsync(timeout, cancellationToken);
            await allCompleted.Task.WaitAsync(timeout, cancellationToken);

            Assert.Null(saturationFailure);
            Assert.Equal(concurrencyLimit, Volatile.Read(ref maximum));
            Assert.Equal(0, Volatile.Read(ref active));
            Assert.Equal(concurrencyLimit, Volatile.Read(ref completed));
        }
        finally
        {
            release.TrySetResult();
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "delivery-beyond-limit-waits")]
    public async Task ConfiguredEndpointConcurrency_QueuesTheNextDeliveryUntilASlotIsReleased()
    {
        const int concurrencyLimit = 3;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var limitReached = NewSignal();
        var deliveryBeyondLimitEntered = NewSignal();
        var allCompleted = NewSignal();
        var release = NewSignal();
        var active = 0;
        var entered = 0;
        var completed = 0;
        var maximum = 0;
        using var harness = new InMemoryTestHarness($"endpoint-concurrency-cap-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.PrefetchCount = concurrencyLimit + 1;
            configurator.ConcurrentMessageLimit = concurrencyLimit;
            configurator.Handler<ConcurrentDelivery>(async _ =>
            {
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, current);
                int entry = Interlocked.Increment(ref entered);
                if (entry == concurrencyLimit)
                    limitReached.TrySetResult();
                else if (entry > concurrencyLimit)
                    deliveryBeyondLimitEntered.TrySetResult();

                await release.Task;

                Interlocked.Decrement(ref active);
                if (Interlocked.Increment(ref completed) == concurrencyLimit + 1)
                    allCompleted.TrySetResult();
            });
        };

        try
        {
            await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
            Task[] sends = Enumerable.Range(0, concurrencyLimit + 1)
                .Select(index => harness.InputQueueSendEndpoint.Send(
                    new ConcurrentDelivery(index),
                    cancellationToken))
                .ToArray();

            await limitReached.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(concurrencyLimit, Volatile.Read(ref active));
            Assert.Equal(concurrencyLimit, Volatile.Read(ref entered));
            Assert.False(deliveryBeyondLimitEntered.Task.IsCompleted);

            release.TrySetResult();
            await Task.WhenAll(sends).WaitAsync(timeout, cancellationToken);
            await deliveryBeyondLimitEntered.Task.WaitAsync(timeout, cancellationToken);
            await allCompleted.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(concurrencyLimit, Volatile.Read(ref maximum));
            Assert.Equal(0, Volatile.Read(ref active));
            Assert.Equal(concurrencyLimit + 1, Volatile.Read(ref entered));
            Assert.Equal(concurrencyLimit + 1, Volatile.Read(ref completed));
        }
        finally
        {
            release.TrySetResult();
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed;
        while (candidate > (observed = Volatile.Read(ref maximum)))
        {
            if (Interlocked.CompareExchange(ref maximum, candidate, observed) == observed)
                return;
        }
    }

    private sealed record ConcurrentDelivery(int Index);
}
