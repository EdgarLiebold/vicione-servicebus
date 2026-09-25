using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using SqsQueue = ViciOne.ServiceBus.AmazonSqs.Topology.Queue;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class QueueCacheOwnershipTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);
    private const string QueueName = "orders";
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders";
    private const string QueueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "pending-ephemeral-resolution-does-not-overlap-durable-ownership")]
    public async Task DurableTransition_WaitsForPendingEphemeralResolutionBeforeStartingItsOwnAsync()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRelease = new TaskCompletionSource<GetQueueUrlResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var urlCalls = 0;
        var urlResponse = new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK };
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => GetQueueUrlAsync(),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
            }),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> GetQueueUrlAsync()
        {
            if (Interlocked.Increment(ref urlCalls) == 1)
            {
                firstEntered.TrySetResult();
                return firstRelease.Task;
            }

            secondEntered.TrySetResult();
            return Task.FromResult(urlResponse);
        }

        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);
        Task<QueueInfo> ephemeral = cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken);
        await firstEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<QueueInfo> durable = cache.GetAsync(DurableQueue(), TestContext.Current.CancellationToken);

        try
        {
            Task observed = await Task.WhenAny(secondEntered.Task,
                Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
            Assert.NotSame(secondEntered.Task, observed);
        }
        finally
        {
            firstRelease.TrySetResult(urlResponse);
            await Task.WhenAll(ephemeral, durable).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        }

        QueueInfo durableInfo = await durable;
        Assert.Same(durableInfo, await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken));
        Assert.Equal(2, Volatile.Read(ref urlCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "pending-durable-resolution-is-shared-with-name-lookup")]
    public async Task NameLookup_JoinsPendingDurableResolutionWithoutCreatingAnEphemeralCopyAsync()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRelease = new TaskCompletionSource<GetQueueUrlResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var urlCalls = 0;
        var urlResponse = new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK };
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => GetQueueUrlAsync(),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
            }),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> GetQueueUrlAsync()
        {
            if (Interlocked.Increment(ref urlCalls) == 1)
            {
                firstEntered.TrySetResult();
                return firstRelease.Task;
            }

            secondEntered.TrySetResult();
            return Task.FromResult(urlResponse);
        }

        SqsQueue durableQueue = DurableQueue();
        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);
        Task<QueueInfo> durable = cache.GetAsync(durableQueue, TestContext.Current.CancellationToken);
        await firstEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<QueueInfo> byName = cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken);

        try
        {
            Task observed = await Task.WhenAny(secondEntered.Task,
                Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
            Assert.NotSame(secondEntered.Task, observed);
        }
        finally
        {
            firstRelease.TrySetResult(urlResponse);
            await Task.WhenAll(durable, byName).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        }

        Assert.Same(await durable, await byName);
        Assert.Equal(1, Volatile.Read(ref urlCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "caller-cancellation-preserves-owner-and-other-queues-progress")]
    public async Task CanceledCallers_DoNotReleasePendingOwnershipOrBlockAnotherQueueAsync()
    {
        var ordersEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ordersRelease = new TaskCompletionSource<GetQueueUrlResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondOrdersEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ordersCalls = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => GetQueueUrlAsync(Assert.IsType<string>(args![0])),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string>
                {
                    [QueueAttributeName.QueueArn] = $"arn:aws:sqs:eu-central-1:123456789012/{Path.GetFileName(Assert.IsType<string>(args![0]))}"
                }
            }),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> GetQueueUrlAsync(string name)
        {
            var response = new GetQueueUrlResponse
            {
                QueueUrl = $"https://sqs.eu-central-1.amazonaws.com/123456789012/{name}",
                HttpStatusCode = HttpStatusCode.OK
            };
            if (name == QueueName && Interlocked.Increment(ref ordersCalls) == 1)
            {
                ordersEntered.TrySetResult();
                return ordersRelease.Task;
            }

            if (name == QueueName)
                secondOrdersEntered.TrySetResult();
            return Task.FromResult(response);
        }

        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);
        using var firstCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var secondCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<QueueInfo> first = cache.GetByNameAsync(QueueName, firstCancellation.Token);
        await ordersEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<QueueInfo> canceledWaiter = cache.GetByNameAsync(QueueName, secondCancellation.Token);

        firstCancellation.Cancel();
        secondCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWaiter);

        Task<QueueInfo> durable = cache.GetAsync(DurableQueue(), TestContext.Current.CancellationToken);
        try
        {
            QueueInfo payments = await cache.GetByNameAsync("payments", TestContext.Current.CancellationToken)
                .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal("payments", payments.EntityName);
            Assert.Equal("https://sqs.eu-central-1.amazonaws.com/123456789012/payments", payments.Url);

            Task observed = await Task.WhenAny(secondOrdersEntered.Task,
                Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
            Assert.NotSame(secondOrdersEntered.Task, observed);
        }
        finally
        {
            ordersRelease.TrySetResult(new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
            await durable.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        }

        Assert.Equal(2, Volatile.Read(ref ordersCalls));
        Assert.Same(await durable, await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "failed-resolution-releases-name-gate-for-retry")]
    public async Task FailedResolution_ReleasesTheNameForASecondProviderAttemptAsync()
    {
        var expected = new InvalidOperationException("provider lookup failed");
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failFirst = new TaskCompletionSource<GetQueueUrlResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var urlCalls = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => GetQueueUrlAsync(),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
            }),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> GetQueueUrlAsync()
        {
            if (Interlocked.Increment(ref urlCalls) == 1)
            {
                firstEntered.TrySetResult();
                return failFirst.Task;
            }

            return Task.FromResult(new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
        }

        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);

        Task<QueueInfo> first = cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken);
        await firstEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<QueueInfo> retry = cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken);
        failFirst.SetException(expected);
        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            first.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        QueueInfo recovered = await retry.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Same(expected, observed);
        Assert.Equal(QueueName, recovered.EntityName);
        Assert.Equal(QueueUrl, recovered.Url);
        Assert.Equal(2, Volatile.Read(ref urlCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "remove-waits-for-pending-resolution-and-prevents-stale-reappearance")]
    public async Task Removal_WaitsForPendingResolutionAndAllowsFreshLookupAsync()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRelease = new TaskCompletionSource<GetQueueUrlResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var urlCalls = 0;
        var urlResponse = new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK };
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => GetQueueUrlAsync(),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
            }),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> GetQueueUrlAsync()
        {
            if (Interlocked.Increment(ref urlCalls) == 1)
            {
                firstEntered.TrySetResult();
                return firstRelease.Task;
            }

            return Task.FromResult(urlResponse);
        }

        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);
        Task<QueueInfo> pending = cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken);
        await firstEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<bool> removal = cache.RemoveByNameAsync(QueueName, TestContext.Current.CancellationToken);

        try
        {
            await Assert.ThrowsAsync<TimeoutException>(() =>
                removal.WaitAsync(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
        }
        finally
        {
            firstRelease.TrySetResult(urlResponse);
        }

        QueueInfo previous = await pending.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.True(await removal.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        QueueInfo fresh = await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.NotSame(previous, fresh);
        Assert.Equal(2, Volatile.Read(ref urlCalls));
        Assert.Same(fresh, await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "evicted-queue-rejects-send-and-delete-after-disposal")]
    public async Task EvictedQueue_RejectsSendAndDeleteWithoutRestartingBatchWorkersAsync()
    {
        var providerCalls = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => ResolveUrl(),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
            }),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> ResolveUrl()
        {
            Interlocked.Increment(ref providerCalls);
            return Task.FromResult(new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
        }

        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);
        QueueInfo ephemeral = await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken);
        QueueInfo durable = await cache.GetAsync(DurableQueue(), TestContext.Current.CancellationToken);

        Assert.NotSame(ephemeral, durable);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => ephemeral.SendAsync(
            new SendMessageBatchRequestEntry("send", "body"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => ephemeral.DeleteAsync("receipt", TestContext.Current.CancellationToken));
        await ephemeral.DisposeAsync();
        Assert.Equal(2, Volatile.Read(ref providerCalls));
        Assert.Same(durable, await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "disposal-waits-for-inflight-policy-update-and-rejects-new-work")]
    public async Task Dispose_WaitsForPolicyWriteAndPreservesItsSuccessfulResultAsync()
    {
        var writeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWrite = new TaskCompletionSource<SetQueueAttributesResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var writeCalls = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.SetQueueAttributesAsync) => WritePolicy(),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<SetQueueAttributesResponse> WritePolicy()
        {
            Interlocked.Increment(ref writeCalls);
            writeEntered.TrySetResult();
            return releaseWrite.Task;
        }

        var queue = new QueueInfo(QueueName, QueueUrl,
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn },
            client, TestContext.Current.CancellationToken, existing: true);
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:events";
        Task<bool> update = queue.UpdatePolicyAsync(QueueArn, topicArn, TestContext.Current.CancellationToken);
        await writeEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task disposal = queue.DisposeAsync().AsTask();

        try
        {
            Assert.False(disposal.IsCompleted);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                queue.UpdatePolicyAsync(QueueArn, topicArn, TestContext.Current.CancellationToken));
        }
        finally
        {
            releaseWrite.TrySetResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }

        Assert.True(await update.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        await disposal.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(1, Volatile.Read(ref writeCalls));
        Assert.Contains(topicArn, queue.Attributes[QueueAttributeName.Policy]);
        await queue.DisposeAsync();
    }

    private static SqsQueue DurableQueue() => InterfaceProxy<SqsQueue>.Create((method, _) => method.Name switch
    {
        "get_EntityName" => QueueName,
        "get_Durable" => true,
        "get_AutoDelete" => false,
        "get_QueueAttributes" => new Dictionary<string, object>(),
        "get_QueueTags" => new Dictionary<string, string>(),
        _ => throw new NotSupportedException(method.Name)
    });
}
