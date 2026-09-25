using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;
using PublishBatchRequestEntry = Amazon.SimpleNotificationService.Model.PublishBatchRequestEntry;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsTopologyCleanupTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-TOPOLOGY", "failed-declaration-retry-keeps-one-cleanup-owner")]
    public async Task Configure_FailedDeclarationThenRetryRegistersOneCleanupAgentAsync()
    {
        const string queueName = "temporary-queue";
        var topology = new AmazonSqsBrokerTopology([], [new QueueEntity(1, queueName, false, true)], []);
        var agents = new List<IAgent>();
        SqsReceiveEndpointContext endpoint = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, args) => method.Name switch
        {
            nameof(ReceiveEndpointContext.AddSendAgent) => CaptureAgent(args),
            _ => throw new NotSupportedException(method.Name)
        });
        var filter = new ConfigureAmazonSqsTopologyFilter<object>(new object(), topology, endpoint);

        var failedClient = new RecordingClientContext { FailFirstQueueDeclaration = true };
        using (var failedAttempt = new CancellationTokenSource())
        {
            using var failedScope = new ScopeClientContext(failedClient, failedAttempt.Token);
            InvalidOperationException firstFailure = await Assert.ThrowsAsync<InvalidOperationException>(
                () => filter.ConfigureAsync(failedScope, TestContext.Current.CancellationToken));
            Assert.Equal("transient queue declaration failure", firstFailure.Message);
            failedAttempt.Cancel();
        }
        await failedClient.DisposeAsync();
        Assert.Single(agents);

        await using var successfulClient = new RecordingClientContext();
        using (var successfulAttempt = new CancellationTokenSource())
        {
            var sharedContext = new SharedClientContext(successfulClient, successfulAttempt.Token);
            using var successfulScope = new ScopeClientContext(sharedContext, successfulAttempt.Token);
            await filter.ConfigureAsync(successfulScope, TestContext.Current.CancellationToken);
            successfulAttempt.Cancel();
        }

        Assert.Equal([queueName], failedClient.DeclaredQueues);
        Assert.Equal([queueName], successfulClient.DeclaredQueues);
        IAgent agent = Assert.Single(agents);
        await agent.StopAsync(TestContext.Current.CancellationToken);
        Assert.Empty(failedClient.DeletedQueues);
        Assert.Equal([queueName], successfulClient.DeletedQueues);

        await using var restartClient = new RecordingClientContext();
        using (var restartAttempt = new CancellationTokenSource())
        {
            using var restartScope = new ScopeClientContext(restartClient, restartAttempt.Token);
            await filter.ConfigureAsync(restartScope, TestContext.Current.CancellationToken);
            restartAttempt.Cancel();
        }

        Assert.Equal(2, agents.Count);
        await agents[1].StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal([queueName], restartClient.DeletedQueues);

        object? CaptureAgent(object?[]? args)
        {
            agents.Add(Assert.IsAssignableFrom<IAgent>(Assert.Single(args!)));
            return null;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-TOPOLOGY", "auto-delete-agent-registers-once-and-deletes-only-expiring-entities")]
    public async Task Configure_RegistersCleanupOnlyForAutoDeleteAndRemovesExactlyThoseEntitiesAsync(
        bool autoDeleteTopic, bool autoDeleteQueue)
    {
        TopicEntity permanentTopic = new(1, "permanent-topic", true, false);
        TopicEntity temporaryTopic = new(2, "temporary-topic", false, autoDeleteTopic);
        QueueEntity permanentQueue = new(3, "permanent-queue", true, false);
        QueueEntity temporaryQueue = new(4, "temporary-queue", false, autoDeleteQueue);
        var topology = new AmazonSqsBrokerTopology(
            [permanentTopic, temporaryTopic], [permanentQueue, temporaryQueue], []);
        var agents = new List<IAgent>();
        SqsReceiveEndpointContext endpoint = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, args) => method.Name switch
        {
            nameof(ReceiveEndpointContext.AddSendAgent) => CaptureAgent(args),
            _ => throw new NotSupportedException(method.Name)
        });
        await using var client = new RecordingClientContext();
        var filter = new ConfigureAmazonSqsTopologyFilter<object>(new object(), topology, endpoint);

        await filter.ConfigureAsync(client, TestContext.Current.CancellationToken);
        await filter.ConfigureAsync(client, TestContext.Current.CancellationToken);

        Assert.Equal(["permanent-topic", "temporary-topic"], client.DeclaredTopics.Order(StringComparer.Ordinal));
        Assert.Equal(["permanent-queue", "temporary-queue"], client.DeclaredQueues.Order(StringComparer.Ordinal));
        Assert.Equal(autoDeleteTopic || autoDeleteQueue ? 1 : 0, agents.Count);

        if (agents.Count == 1)
        {
            IAgent agent = agents[0];
            await agent.Ready.WaitAsync(TestContext.Current.CancellationToken);
            using var stopLifetime = new CancellationTokenSource();
            await agent.StopAsync(stopLifetime.Token);

            Assert.True(agent.Stopped.IsCancellationRequested);
            Assert.Equal(autoDeleteTopic ? new[] { "temporary-topic" } : Array.Empty<string>(), client.DeletedTopics);
            Assert.Equal(autoDeleteQueue ? new[] { "temporary-queue" } : Array.Empty<string>(), client.DeletedQueues);
            Assert.All(client.DeleteTokens, token => Assert.Equal(stopLifetime.Token, token));
            Assert.Equal((autoDeleteTopic ? 1 : 0) + (autoDeleteQueue ? 1 : 0), client.DeleteTokens.Count);
        }
        else
        {
            Assert.Empty(client.DeletedTopics);
            Assert.Empty(client.DeletedQueues);
        }

        object? CaptureAgent(object?[]? args)
        {
            agents.Add(Assert.IsAssignableFrom<IAgent>(Assert.Single(args!)));
            return null;
        }
    }

    private sealed class RecordingClientContext : BasePipeContext, ClientContext, IAsyncDisposable
    {
        private readonly IAmazonSQS _sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
        private readonly IAmazonSimpleNotificationService _sns =
            InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) => throw new NotSupportedException(method.Name));
        private readonly List<IAsyncDisposable> _resolved = [];
        private bool _disposed;

        public List<string> DeclaredTopics { get; } = [];
        public List<string> DeclaredQueues { get; } = [];
        public List<string> DeletedTopics { get; } = [];
        public List<string> DeletedQueues { get; } = [];
        public List<CancellationToken> DeleteTokens { get; } = [];
        public bool FailFirstQueueDeclaration { get; init; }

        public ConnectionContext ConnectionContext => throw new NotSupportedException();

        public Task<TopicInfo> CreateTopicAsync(Topic topic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeclaredTopics.Add(topic.EntityName);
            var info = new TopicInfo(topic.EntityName, $"arn:aws:sns:eu-central-1:123456789012:{topic.EntityName}",
                _sns, CancellationToken.None, true);
            _resolved.Add(info);
            return Task.FromResult(info);
        }

        public Task<QueueInfo> CreateQueueAsync(ViciOne.ServiceBus.AmazonSqs.Topology.Queue queue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeclaredQueues.Add(queue.EntityName);
            if (FailFirstQueueDeclaration && DeclaredQueues.Count == 1)
                throw new InvalidOperationException("transient queue declaration failure");
            var info = new QueueInfo(queue.EntityName,
                $"https://sqs.eu-central-1.amazonaws.com/123456789012/{queue.EntityName}",
                new Dictionary<string, string>
                {
                    [QueueAttributeName.QueueArn] = $"arn:aws:sqs:eu-central-1:123456789012:{queue.EntityName}"
                }, _sqs, CancellationToken.None, true);
            _resolved.Add(info);
            return Task.FromResult(info);
        }

        public Task DeleteTopicAsync(Topic topic, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            DeletedTopics.Add(topic.EntityName);
            DeleteTokens.Add(cancellationToken);
            return Task.CompletedTask;
        }

        public Task DeleteQueueAsync(ViciOne.ServiceBus.AmazonSqs.Topology.Queue queue, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            DeletedQueues.Add(queue.EntityName);
            DeleteTokens.Add(cancellationToken);
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            _disposed = true;
            foreach (IAsyncDisposable resource in _resolved)
                await resource.DisposeAsync();
        }

        public Task<bool> CreateQueueSubscriptionAsync(Topic topic, ViciOne.ServiceBus.AmazonSqs.Topology.Queue queue,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteMessageAsync(string queueName, string receiptHandle, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
