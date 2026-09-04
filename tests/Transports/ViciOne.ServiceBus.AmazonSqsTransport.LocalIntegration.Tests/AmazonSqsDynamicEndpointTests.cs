using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsDynamicEndpointTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0185", "runtime-endpoint-receives-and-removes-provider-resources")]
    public async Task ConnectedSubscriptionEndpoint_ReceivesAndStopsCleanlyAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("dynamic");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        string queueName = fixture.Name("endpoint");
        Guid expected = Guid.NewGuid();
        var received = new TaskCompletionSource<ObservedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(fixture.ConfigureHost);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HostReceiveEndpointHandle? endpointHandle = null;
        bool busStarted = false;
        bool endpointStopped = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            busStarted = true;
            endpointHandle = bus.ConnectReceiveEndpoint(queueName, endpoint =>
            {
                var amazonEndpoint = Assert.IsAssignableFrom<IAmazonSqsReceiveEndpointConfigurator>(endpoint);
                amazonEndpoint.Durable = false;
                amazonEndpoint.AutoDelete = true;
                endpoint.Handler<DynamicEvent>(context =>
                {
                    received.TrySetResult(new ObservedMessage(context.MessageId, context.Message.CorrelationId));
                    return Task.CompletedTask;
                });
            });
            ReceiveEndpointReady ready = await endpointHandle.Ready.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(queueName, ready.InputAddress.Segments[^1].TrimEnd('/'));

            Guid messageId = Guid.NewGuid();
            await bus.PublishAsync(new DynamicEvent(expected), context => context.MessageId = messageId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(
                new ObservedMessage(messageId, expected),
                await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal([queueName], await ListOwnedQueueNamesAsync(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
            Topic topic = Assert.Single(await ListOwnedTopicsAsync(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
            ListSubscriptionsByTopicResponse subscriptions = await sns.ListSubscriptionsByTopicAsync(
                    new ListSubscriptionsByTopicRequest { TopicArn = topic.TopicArn }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Single(subscriptions.Subscriptions ?? []);

            await endpointHandle.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            endpointStopped = true;

            Assert.Empty(await ListOwnedQueueNamesAsync(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
            Assert.Empty(await ListOwnedTopicsAsync(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (endpointHandle != null && !endpointStopped)
                await endpointHandle.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            if (busStarted)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static async Task<string[]> ListOwnedQueueNamesAsync(
        IAmazonSQS sqs,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ListQueuesResponse response = await sqs.ListQueuesAsync(
                new ListQueuesRequest { QueueNamePrefix = prefix }, cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        return (response.QueueUrls ?? [])
            .Select(url => url[(url.LastIndexOf('/') + 1)..])
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<Topic[]> ListOwnedTopicsAsync(
        IAmazonSimpleNotificationService sns,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ListTopicsResponse response = await sns.ListTopicsAsync(new ListTopicsRequest(), cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        return (response.Topics ?? [])
            .Where(topic => topic.TopicArn[(topic.TopicArn.LastIndexOf(':') + 1)..].StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(topic => topic.TopicArn, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record DynamicEvent(Guid CorrelationId);
    private sealed record ObservedMessage(Guid? MessageId, Guid CorrelationId);
}
