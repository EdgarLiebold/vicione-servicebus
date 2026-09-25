using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsClientContextQueueEvictionTests
{
    private const string QueueName = "orders";
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders";
    private const string QueueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-retries-evicted-queue-before-batch-admission")]
    public async Task Client_ReresolvesAnEvictedQueueBeforeSendOrDeleteAdmissionAsync(bool delete)
    {
        var lookups = 0;
        var providerCalls = 0;
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.SendMessageBatchAsync) => SendAsync(Assert.IsType<SendMessageBatchRequest>(args![0])),
            nameof(IAmazonSQS.DeleteMessageBatchAsync) => DeleteAsync(Assert.IsType<DeleteMessageBatchRequest>(args![0])),
            _ => throw new NotSupportedException(method.Name)
        });
        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create(
            (method, _) => throw new NotSupportedException(method.Name));
        QueueInfo stale = CreateQueue(sqs);
        await stale.DisposeAsync();
        await using QueueInfo fresh = CreateQueue(sqs);
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, args) => method.Name switch
        {
            nameof(ConnectionContext.GetQueueByNameAsync) => ResolveAsync(Assert.IsType<string>(args![0])),
            _ => throw new NotSupportedException(method.Name)
        });
        var client = new AmazonSqsClientContext(connection, sqs, sns, TestContext.Current.CancellationToken);

        Task<QueueInfo> ResolveAsync(string name)
        {
            Assert.Equal(QueueName, name);
            return Task.FromResult(Interlocked.Increment(ref lookups) == 1 ? stale : fresh);
        }

        Task<SendMessageBatchResponse> SendAsync(SendMessageBatchRequest request)
        {
            Interlocked.Increment(ref providerCalls);
            Assert.Equal(QueueUrl, request.QueueUrl);
            SendMessageBatchRequestEntry entry = Assert.Single(request.Entries);
            Assert.Equal("payload", entry.MessageBody);
            return Task.FromResult(new SendMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = [new SendMessageBatchResultEntry { Id = entry.Id }],
                Failed = []
            });
        }

        Task<DeleteMessageBatchResponse> DeleteAsync(DeleteMessageBatchRequest request)
        {
            Interlocked.Increment(ref providerCalls);
            Assert.Equal(QueueUrl, request.QueueUrl);
            DeleteMessageBatchRequestEntry entry = Assert.Single(request.Entries);
            Assert.Equal("receipt", entry.ReceiptHandle);
            return Task.FromResult(new DeleteMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = [new DeleteMessageBatchResultEntry { Id = entry.Id }],
                Failed = []
            });
        }

        if (delete)
            await client.DeleteMessageAsync(QueueName, "receipt", TestContext.Current.CancellationToken);
        else
            await client.SendMessageAsync(QueueName, new SendMessageBatchRequestEntry("send", "payload"),
                TestContext.Current.CancellationToken);

        Assert.Equal(2, Volatile.Read(ref lookups));
        Assert.Equal(1, Volatile.Read(ref providerCalls));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-does-not-retry-after-provider-batch-failure")]
    public async Task Client_DoesNotReplayAnAdmittedBatchAfterProviderFailureAsync(bool delete)
    {
        var providerFailure = new InvalidOperationException("provider failed");
        var lookups = 0;
        var providerCalls = 0;
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.SendMessageBatchAsync) => FailSendAsync(),
            nameof(IAmazonSQS.DeleteMessageBatchAsync) => FailDeleteAsync(),
            _ => throw new NotSupportedException(method.Name)
        });
        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create(
            (method, _) => throw new NotSupportedException(method.Name));
        await using QueueInfo queue = CreateQueue(sqs);
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            nameof(ConnectionContext.GetQueueByNameAsync) => ResolveAsync(),
            _ => throw new NotSupportedException(method.Name)
        });
        var client = new AmazonSqsClientContext(connection, sqs, sns, TestContext.Current.CancellationToken);

        Task<QueueInfo> ResolveAsync()
        {
            Interlocked.Increment(ref lookups);
            return Task.FromResult(queue);
        }

        Task<SendMessageBatchResponse> FailSendAsync()
        {
            Interlocked.Increment(ref providerCalls);
            return Task.FromException<SendMessageBatchResponse>(providerFailure);
        }

        Task<DeleteMessageBatchResponse> FailDeleteAsync()
        {
            Interlocked.Increment(ref providerCalls);
            return Task.FromException<DeleteMessageBatchResponse>(providerFailure);
        }

        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() => delete
            ? client.DeleteMessageAsync(QueueName, "receipt", TestContext.Current.CancellationToken)
            : client.SendMessageAsync(QueueName, new SendMessageBatchRequestEntry("send", "payload"),
                TestContext.Current.CancellationToken));

        Assert.Same(providerFailure, observed);
        Assert.Equal(1, Volatile.Read(ref lookups));
        Assert.Equal(1, Volatile.Read(ref providerCalls));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-bounds-retries-of-repeatedly-evicted-queue")]
    public async Task Client_StopsAfterTwoEvictedQueueResolutionsWithoutCallingProviderAsync(bool delete)
    {
        var lookups = 0;
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create(
            (method, _) => throw new InvalidOperationException($"Unexpected provider call: {method.Name}"));
        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create(
            (method, _) => throw new NotSupportedException(method.Name));
        QueueInfo stale = CreateQueue(sqs);
        await stale.DisposeAsync();
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            nameof(ConnectionContext.GetQueueByNameAsync) => ResolveAsync(),
            _ => throw new NotSupportedException(method.Name)
        });
        var client = new AmazonSqsClientContext(connection, sqs, sns, TestContext.Current.CancellationToken);

        Task<QueueInfo> ResolveAsync()
        {
            Interlocked.Increment(ref lookups);
            return Task.FromResult(stale);
        }

        ObjectDisposedException exception = await Assert.ThrowsAsync<ObjectDisposedException>(() => delete
            ? client.DeleteMessageAsync(QueueName, "receipt", TestContext.Current.CancellationToken)
            : client.SendMessageAsync(QueueName, new SendMessageBatchRequestEntry("send", "payload"),
                TestContext.Current.CancellationToken));

        Assert.Contains(QueueName, exception.Message);
        Assert.Equal(2, Volatile.Read(ref lookups));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-retries-only-unadmitted-entry-after-full-batch-channel-closes")]
    public async Task Client_RetriesAWaitingBatchEntryAfterEvictionClosesTheFullChannelAsync(bool delete)
    {
        const string freshUrl = QueueUrl + "-fresh";
        var oldStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOld = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldTargetCalls = 0;
        var freshTargetCalls = 0;
        var lookups = 0;
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.SendMessageBatchAsync) => SendAsync(Assert.IsType<SendMessageBatchRequest>(args![0])),
            nameof(IAmazonSQS.DeleteMessageBatchAsync) => DeleteAsync(Assert.IsType<DeleteMessageBatchRequest>(args![0])),
            _ => throw new NotSupportedException(method.Name)
        });
        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create(
            (method, _) => throw new NotSupportedException(method.Name));
        QueueInfo old = CreateQueue(sqs);
        await using QueueInfo fresh = new(QueueName, freshUrl,
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn },
            sqs, TestContext.Current.CancellationToken, existing: true);
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            nameof(ConnectionContext.GetQueueByNameAsync) => Task.FromResult(Interlocked.Increment(ref lookups) == 1 ? old : fresh),
            _ => throw new NotSupportedException(method.Name)
        });
        var client = new AmazonSqsClientContext(connection, sqs, sns, TestContext.Current.CancellationToken);

        async Task<SendMessageBatchResponse> SendAsync(SendMessageBatchRequest request)
        {
            await WaitIfOldAsync(request.QueueUrl);
            CountTarget(request.QueueUrl, request.Entries.Any(x => x.MessageBody == "target"));
            return new SendMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.Entries.Select(x => new SendMessageBatchResultEntry { Id = x.Id }).ToList(),
                Failed = []
            };
        }

        async Task<DeleteMessageBatchResponse> DeleteAsync(DeleteMessageBatchRequest request)
        {
            await WaitIfOldAsync(request.QueueUrl);
            CountTarget(request.QueueUrl, request.Entries.Any(x => x.ReceiptHandle == "target"));
            return new DeleteMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.Entries.Select(x => new DeleteMessageBatchResultEntry { Id = x.Id }).ToList(),
                Failed = []
            };
        }

        async Task WaitIfOldAsync(string url)
        {
            if (url != QueueUrl)
                return;

            oldStarted.TrySetResult();
            await releaseOld.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }

        void CountTarget(string url, bool containsTarget)
        {
            if (!containsTarget)
                return;

            if (url == QueueUrl)
                Interlocked.Increment(ref oldTargetCalls);
            else if (url == freshUrl)
                Interlocked.Increment(ref freshTargetCalls);
            else
                throw new InvalidOperationException($"Unexpected queue URL: {url}");
        }

        Task FillAsync(int number) => delete
            ? old.DeleteAsync($"filler-{number}", TestContext.Current.CancellationToken)
            : old.SendAsync(new SendMessageBatchRequestEntry($"filler-{number}", $"filler-{number}"),
                TestContext.Current.CancellationToken);

        Task first = FillAsync(0);
        await oldStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Task[] filler = [first, .. Enumerable.Range(1, 299).Select(FillAsync)];
        Task target = delete
            ? client.DeleteMessageAsync(QueueName, "target", TestContext.Current.CancellationToken)
            : client.SendMessageAsync(QueueName, new SendMessageBatchRequestEntry("target", "target"),
                TestContext.Current.CancellationToken);
        Task disposal = old.DisposeAsync().AsTask();

        releaseOld.TrySetResult();

        await disposal.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await target.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        try
        {
            await Task.WhenAll(filler).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
        catch (BatchAdmissionClosedException)
        {
        }

        Assert.All(filler, task => Assert.True(task.IsCompletedSuccessfully ||
            task.Exception?.InnerException is BatchAdmissionClosedException));
        Assert.Equal(0, Volatile.Read(ref oldTargetCalls));
        Assert.Equal(1, Volatile.Read(ref freshTargetCalls));
        Assert.Equal(2, Volatile.Read(ref lookups));
    }

    private static QueueInfo CreateQueue(IAmazonSQS sqs) => new(QueueName, QueueUrl,
        new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn },
        sqs, TestContext.Current.CancellationToken, existing: true);
}
