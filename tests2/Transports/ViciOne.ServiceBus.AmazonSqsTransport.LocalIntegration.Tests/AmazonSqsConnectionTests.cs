namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime;
using System.Net;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsConnectionTests
{
    [Theory]
    [InlineData("a", "a")]
    [InlineData("Ready-Health_Probe", "readyhealthprobe")]
    [InlineData("---", "test")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LOCAL-PREFIX", "short-and-normalized-purpose-never-overruns")]
    public void RunPrefix_IsBoundedAndUsesOnlyAwsNameCharacters(string purpose, string normalizedPurpose)
    {
        var runId = new Guid("01234567-89ab-cdef-0123-456789abcdef");

        string actual = AmazonSqsLocalStack.CreatePrefix(purpose, runId);

        Assert.Equal($"vsb-{normalizedPurpose}-0123456789abcdef0123456789abcdef", actual);
        Assert.InRange(actual.Length, 38, 53);
        Assert.All(actual, character => Assert.True(char.IsAsciiLetterOrDigit(character) || character == '-'));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0177", "run-scoped-localstack-sqs-and-sns-ready")]
    public async Task RunScopedLocalStackHost_StartsAndReportsReady()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("providerready");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        ListQueuesResponse queues = await sqs.ListQueuesAsync(new ListQueuesRequest(), cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        ListTopicsResponse topics = await sns.ListTopicsAsync(new ListTopicsRequest(), cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.True(IPAddress.IsLoopback(IPAddress.Parse(fixture.Endpoint.Host)));
        Assert.Equal("eu-central-1", fixture.Region);
        Assert.Equal(HttpStatusCode.OK, queues.HttpStatusCode);
        Assert.Equal(HttpStatusCode.OK, topics.HttpStatusCode);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0178", "test-owned-resources-are-removed-by-fixture-teardown")]
    public async Task TestOwnedFixture_StartsStopsAndCleansItsResources()
    {
        AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("cleanup");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        string queueName = fixture.Name("queue");
        string topicName = fixture.Name("topic");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await sqs.CreateQueueAsync(new CreateQueueRequest(queueName), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await sns.CreateTopicAsync(new CreateTopicRequest(topicName), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal([queueName], await ListOwnedQueueNames(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
            Assert.Equal([topicName], await ListOwnedTopicNames(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            await fixture.DisposeAsync();
        }

        Assert.Empty(await ListOwnedQueueNames(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
        Assert.Empty(await ListOwnedTopicNames(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0179", "publish-without-consumer-creates-one-unsubscribed-topic")]
    public async Task PublishWithoutConsumer_CreatesOnlyTheExpectedTopic()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("unconsumed");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        Guid flowId = Guid.NewGuid();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(fixture.ConfigureHost);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.Publish(new UnconsumedEvent(flowId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            string[] topicNames = await ListOwnedTopicNames(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken);
            string topicName = Assert.Single(topicNames);
            Assert.Contains(nameof(UnconsumedEvent), topicName, StringComparison.Ordinal);
            Assert.Empty(await ListOwnedQueueNames(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken));

            ListTopicsResponse topics = await sns.ListTopicsAsync(new ListTopicsRequest(), cancellationToken);
            Topic topic = Assert.Single(topics.Topics, candidate => candidate.TopicArn.EndsWith(':' + topicName, StringComparison.Ordinal));
            ListSubscriptionsByTopicResponse subscriptions = await sns.ListSubscriptionsByTopicAsync(
                new ListSubscriptionsByTopicRequest { TopicArn = topic.TopicArn }, cancellationToken);
            Assert.Empty(subscriptions.Subscriptions ?? []);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0181", "explicit-credential-object-signs-local-provider-requests")]
    public async Task ExplicitCredentialsObject_ConnectsToTheConfiguredEndpoint()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("credentials");
        var credentials = new TrackingCredentials(AmazonSqsLocalStack.CreateRunCredentials());
        string queueName = fixture.Name("input");
        Guid expected = Guid.NewGuid();
        var received = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator, credentials);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<CredentialMessage>(context =>
                {
                    received.TrySetResult(context.Message.CorrelationId);
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
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new CredentialMessage(expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.True(credentials.ResolutionCount > 0, "The supplied AWSCredentials object was never resolved by an AWS SDK client.");
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0184", "valid-configuration-ready-healthy-and-delivering")]
    public async Task ValidConfiguration_ReachesReadyAndHealthy()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("ready");
        string queueName = fixture.Name("input");
        Guid expected = Guid.NewGuid();
        var received = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<ReadyMessage>(context =>
                {
                    received.TrySetResult(context.Message.CorrelationId);
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
            Assert.Equal(BusHealthStatus.Healthy, bus.CheckHealth().Status);

            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new ReadyMessage(expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private sealed record ReadyMessage(Guid CorrelationId);
    private sealed record CredentialMessage(Guid CorrelationId);
    private sealed record UnconsumedEvent(Guid FlowId);

    private sealed class TrackingCredentials(AWSCredentials inner) : AWSCredentials
    {
        private int _resolutionCount;

        public int ResolutionCount => Volatile.Read(ref _resolutionCount);

        public override ImmutableCredentials GetCredentials()
        {
            Interlocked.Increment(ref _resolutionCount);
            return inner.GetCredentials();
        }
    }

    private static async Task<string[]> ListOwnedQueueNames(
        IAmazonSQS sqs,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var names = new List<string>();
        string? token = null;
        do
        {
            ListQueuesResponse response = await sqs.ListQueuesAsync(
                    new ListQueuesRequest { QueueNamePrefix = prefix, NextToken = token }, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            names.AddRange((response.QueueUrls ?? []).Select(url => url[(url.LastIndexOf('/') + 1)..]));
            token = response.NextToken;
        }
        while (!string.IsNullOrEmpty(token));

        return names.Order(StringComparer.Ordinal).ToArray();
    }

    private static async Task<string[]> ListOwnedTopicNames(
        IAmazonSimpleNotificationService sns,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var names = new List<string>();
        string? token = null;
        do
        {
            ListTopicsResponse response = await sns.ListTopicsAsync(new ListTopicsRequest { NextToken = token }, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            names.AddRange((response.Topics ?? [])
                .Select(topic => topic.TopicArn[(topic.TopicArn.LastIndexOf(':') + 1)..])
                .Where(name => name.StartsWith(prefix, StringComparison.Ordinal)));
            token = response.NextToken;
        }
        while (!string.IsNullOrEmpty(token));

        return names.Order(StringComparer.Ordinal).ToArray();
    }
}
