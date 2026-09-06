using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using PublishBatchRequestEntry = Amazon.SimpleNotificationService.Model.PublishBatchRequestEntry;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class SqsMoveTransportTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "move-links-caller-lifetime")]
    public async Task Move_LinksCallerCancellationToTopologyAndProviderOperationsAsync()
    {
        using var receiveLifetime = new CancellationTokenSource();
        using var callerLifetime = new CancellationTokenSource();
        var providerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observedToken = default;
        var clientContext = new TestClientContext(receiveLifetime.Token, async token =>
        {
            observedToken = token;
            providerStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        });
        ReceiveContext receiveContext = CreateReceiveContext(clientContext, receiveLifetime.Token);
        var topology = new AmazonSqsBrokerTopology([], [], []);
        var transport = new TestMoveTransport(new ConfigureAmazonSqsTopologyFilter<object>(new object(), topology));

        Task move = transport.MoveAsync(receiveContext, callerLifetime.Token);
        await providerStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            callerLifetime.Cancel();
            Task completed = await Task.WhenAny(move, Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));

            Assert.Same(move, completed);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => move);
            Assert.True(observedToken.IsCancellationRequested);
        }
        finally
        {
            receiveLifetime.Cancel();
            await IgnoreCancellationAsync(move);
        }
    }

    private static ReceiveContext CreateReceiveContext(ClientContext clientContext, CancellationToken cancellationToken)
    {
        return InterfaceProxy<ReceiveContext>.Create((method, args) => method.Name switch
        {
            "get_Body" => new StringMessageBody("payload"),
            "get_CancellationToken" => cancellationToken,
            nameof(PipeContext.TryGetPayload) => TryGetPayload(method.GetGenericArguments()[0], args, clientContext),
            _ => Default(method.ReturnType)
        });
    }

    private static bool TryGetPayload(Type payloadType, object?[]? arguments, ClientContext clientContext)
    {
        if (payloadType.IsInstanceOfType(clientContext))
        {
            arguments![0] = clientContext;
            return true;
        }

        arguments![0] = null;
        return false;
    }

    private static object? Default(Type returnType) => returnType.IsValueType ? Activator.CreateInstance(returnType) : null;

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private sealed class TestMoveTransport(ConfigureAmazonSqsTopologyFilter<object> topologyFilter)
        : SqsMoveTransport<object>("errors", topologyFilter)
    {
        public Task MoveAsync(ReceiveContext context, CancellationToken cancellationToken)
        {
            return MoveAsync(context, static (_, _) => { }, cancellationToken);
        }
    }

    private sealed class TestClientContext(CancellationToken cancellationToken, Func<CancellationToken, Task> send)
        : BasePipeContext(cancellationToken), ClientContext
    {
        public ConnectionContext ConnectionContext => throw new NotSupportedException();

        public Task<TopicInfo> CreateTopicAsync(Topic topic, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> CreateQueueSubscriptionAsync(Topic topic, Queue queue, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteTopicAsync(Topic topic, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken) =>
            send(cancellationToken);

        public Task DeleteMessageAsync(string queueName, string receiptHandle, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
