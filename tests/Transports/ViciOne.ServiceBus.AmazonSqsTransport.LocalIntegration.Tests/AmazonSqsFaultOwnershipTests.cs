using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsFaultOwnershipTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0267", "product-error-and-skipped-queues-exclude-native-redrive")]
    public async Task ProductErrorAndSkippedQueues_RejectNativeRedriveAndDoNotDuplicate()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("faultownership");
        await AssertNativeRedriveRejected(fixture);

        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        string inputQueue = fixture.Name("input");
        string errorQueue = $"{inputQueue}_error";
        string skippedQueue = $"{inputQueue}_skipped";
        Guid faultId = Guid.NewGuid();
        Guid skippedId = Guid.NewGuid();
        var faultMoved = NewObservation<Guid>();
        var skippedMoved = NewObservation<Guid>();
        var faultCount = 0;
        var skippedCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<FaultOwnedMessage>(_ => throw new InvalidOperationException("intentional ownership probe"));
            });
            configurator.ReceiveEndpoint(errorQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<FaultOwnedMessage>(context =>
                {
                    Interlocked.Increment(ref faultCount);
                    faultMoved.TrySetResult(context.Message.Id);
                    return Task.CompletedTask;
                });
            });
            configurator.ReceiveEndpoint(skippedQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<SkippedOwnedMessage>(context =>
                {
                    Interlocked.Increment(ref skippedCount);
                    skippedMoved.TrySetResult(context.Message.Id);
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
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{inputQueue}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new FaultOwnedMessage(faultId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new SkippedOwnedMessage(skippedId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(faultId, await faultMoved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(skippedId, await skippedMoved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(1, Volatile.Read(ref faultCount));
            Assert.Equal(1, Volatile.Read(ref skippedCount));

            string inputUrl = (await sqs.GetQueueUrlAsync(inputQueue, cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken))
                .QueueUrl;
            GetQueueAttributesResponse attributes = await sqs.GetQueueAttributesAsync(
                    inputUrl,
                    [QueueAttributeName.RedrivePolicy, QueueAttributeName.ApproximateNumberOfMessages],
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.False(attributes.Attributes.ContainsKey(QueueAttributeName.RedrivePolicy));
            Assert.Equal("0", attributes.Attributes[QueueAttributeName.ApproximateNumberOfMessages]);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task AssertNativeRedriveRejected(AmazonSqsLocalStack fixture)
    {
        IBusControl? invalidBus = null;
        bool started = false;
        try
        {
            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            {
                invalidBus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
                {
                    fixture.ConfigureHost(configurator);
                    configurator.ReceiveEndpoint(fixture.Name("invalid"), endpoint =>
                        endpoint.QueueAttributes[QueueAttributeName.RedrivePolicy] = "{}");
                });
                await invalidBus.StartAsync(TestContext.Current.CancellationToken)
                    .WaitAsync(fixture.OperationTimeout, TestContext.Current.CancellationToken);
                started = true;
            });

            Assert.Contains("RedrivePolicy", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (started && invalidBus is not null)
                await invalidBus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record FaultOwnedMessage(Guid Id);
    private sealed record SkippedOwnedMessage(Guid Id);
}
