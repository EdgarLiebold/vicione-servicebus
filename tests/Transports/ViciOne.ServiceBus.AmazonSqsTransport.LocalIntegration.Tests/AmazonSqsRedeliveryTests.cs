using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsRedeliveryTests
{
    private const int DelayedRedeliveryLimit = 3;
    private const int ImmediateRetryLimit = 1;
    private int _attempts;

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0186", "three-delayed-redeliveries-with-one-immediate-retry-stop-exactly")]
    public async Task DelayedRedelivery_StopsAtTheConfiguredLimit()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("redelivery");
        string inputQueue = fixture.Name("input");
        string faultQueue = fixture.Name("faults");
        Guid correlationId = Guid.NewGuid();
        var faulted = new TaskCompletionSource<FaultObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseDelayedRedelivery(redelivery =>
                redelivery.Interval(DelayedRedeliveryLimit, TimeSpan.FromSeconds(1)));
            configurator.UseMessageRetry(retry => retry.Immediate(ImmediateRetryLimit));
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<AlwaysFails>(context =>
                {
                    Interlocked.Increment(ref _attempts);
                    throw new IntentionalRedeliveryException(context.Message.CorrelationId);
                });
            });
            configurator.ReceiveEndpoint(faultQueue, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<Fault<AlwaysFails>>(context =>
                {
                    faulted.TrySetResult(new FaultObservation(
                        context.Message.Message.CorrelationId,
                        context.Headers.Get<int>(MessageHeaders.FaultRedeliveryCount)
                            ?? throw new InvalidDataException("The terminal fault is missing FaultRedeliveryCount."),
                        context.Headers.Get<int>(MessageHeaders.FaultRetryCount)
                            ?? throw new InvalidDataException("The terminal fault is missing FaultRetryCount."),
                        context.Message.Exceptions.Select(exception => exception.ExceptionType).ToArray()));
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.Publish(new AlwaysFails(correlationId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            FaultObservation actual = await faulted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(DelayedRedeliveryLimit, actual.RedeliveryCount);
            Assert.Equal(ImmediateRetryLimit, actual.RetryCount);
            Assert.Contains(typeof(IntentionalRedeliveryException).FullName, actual.ExceptionTypes);
            Assert.Equal((DelayedRedeliveryLimit + 1) * (ImmediateRetryLimit + 1), Volatile.Read(ref _attempts));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private sealed record AlwaysFails(Guid CorrelationId);

    private sealed record FaultObservation(
        Guid CorrelationId,
        int RedeliveryCount,
        int RetryCount,
        string?[] ExceptionTypes);

    private sealed class IntentionalRedeliveryException(Guid correlationId)
        : Exception($"Intentional redelivery failure for {correlationId:D}.");
}
