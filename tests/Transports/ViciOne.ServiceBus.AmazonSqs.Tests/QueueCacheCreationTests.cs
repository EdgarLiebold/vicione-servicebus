using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class QueueCacheCreationTests
{
    private const string QueueName = "orders";
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders";
    private const string QueueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "missing-queue-creation-preserves-declared-provider-metadata")]
    public async Task MissingQueue_CreatesWithExactAttributesAndTagsThenCachesReturnedIdentityAsync()
    {
        var calls = new List<string>();
        CreateQueueRequest? submitted = null;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => MissingAsync(),
            nameof(IAmazonSQS.CreateQueueAsync) => CreateAsync(Assert.IsType<CreateQueueRequest>(args![0])),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => AttributesAsync(
                Assert.IsType<string>(args![0]), Assert.IsAssignableFrom<IEnumerable<string>>(args[1])),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> MissingAsync()
        {
            calls.Add("lookup");
            return Task.FromException<GetQueueUrlResponse>(new QueueDoesNotExistException("missing"));
        }

        Task<CreateQueueResponse> CreateAsync(CreateQueueRequest request)
        {
            calls.Add("create");
            submitted = request;
            return Task.FromResult(new CreateQueueResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
        }

        Task<GetQueueAttributesResponse> AttributesAsync(string url, IEnumerable<string> requestedAttributes)
        {
            calls.Add("attributes");
            Assert.Equal(QueueUrl, url);
            Assert.Equal([QueueAttributeName.All], requestedAttributes);
            return Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string>
                {
                    [QueueAttributeName.QueueArn] = QueueArn,
                    [QueueAttributeName.VisibilityTimeout] = "30"
                }
            });
        }

        var declaration = new QueueEntity(1, QueueName, durable: true, autoDelete: false,
            queueAttributes: new Dictionary<string, object> { [QueueAttributeName.VisibilityTimeout] = 30 },
            queueSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false" },
            queueTags: new Dictionary<string, string> { ["zone"] = "blue", ["owner"] = "ops" });
        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);

        QueueInfo created = await cache.GetAsync(declaration, TestContext.Current.CancellationToken);
        QueueInfo repeated = await cache.GetAsync(declaration, TestContext.Current.CancellationToken);
        QueueInfo byName = await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken);

        Assert.Equal(["lookup", "create", "attributes"], calls);
        Assert.NotNull(submitted);
        Assert.Equal(QueueName, submitted.QueueName);
        Assert.Equal(new Dictionary<string, string> { [QueueAttributeName.VisibilityTimeout] = "30" }, submitted.Attributes);
        Assert.Equal(new Dictionary<string, string> { ["zone"] = "blue", ["owner"] = "ops" }, submitted.Tags);
        Assert.DoesNotContain("RawMessageDelivery", submitted.Attributes.Keys);
        Assert.Equal(30, declaration.QueueAttributes[QueueAttributeName.VisibilityTimeout]);
        Assert.False(created.Existing);
        Assert.Equal(QueueUrl, created.Url);
        Assert.Equal(QueueArn, created.Arn);
        Assert.Equal("30", created.Attributes[QueueAttributeName.VisibilityTimeout]);
        Assert.Same(created, repeated);
        Assert.Same(created, byName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "missing-fifo-queue-adds-required-flag-without-mutating-declaration")]
    public async Task MissingFifoQueue_AddsRequiredFlagWithoutMutatingDeclarationAsync(bool explicitFlag)
    {
        const string fifoName = "orders.fifo";
        const string fifoUrl = QueueUrl + ".fifo";
        const string fifoArn = QueueArn + ".fifo";
        CreateQueueRequest? submitted = null;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => Task.FromException<GetQueueUrlResponse>(
                new QueueDoesNotExistException("missing")),
            nameof(IAmazonSQS.CreateQueueAsync) => CreateAsync(Assert.IsType<CreateQueueRequest>(args![0])),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = fifoArn }
            }),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<CreateQueueResponse> CreateAsync(CreateQueueRequest request)
        {
            submitted = request;
            return Task.FromResult(new CreateQueueResponse { QueueUrl = fifoUrl, HttpStatusCode = HttpStatusCode.OK });
        }

        Dictionary<string, object> declaredAttributes = explicitFlag
            ? new Dictionary<string, object> { [QueueAttributeName.FifoQueue] = "true", ["ContentBasedDeduplication"] = "true" }
            : new Dictionary<string, object> { ["ContentBasedDeduplication"] = "true" };
        var declaration = new QueueEntity(1, fifoName, durable: true, autoDelete: false,
            queueAttributes: declaredAttributes);
        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);

        QueueInfo created = await cache.GetAsync(declaration, TestContext.Current.CancellationToken);

        Assert.NotNull(submitted);
        Assert.Equal(fifoName, submitted.QueueName);
        Assert.Equal("true", submitted.Attributes[QueueAttributeName.FifoQueue]);
        Assert.Equal("true", submitted.Attributes["ContentBasedDeduplication"]);
        Assert.Equal(explicitFlag, declaredAttributes.ContainsKey(QueueAttributeName.FifoQueue));
        Assert.Equal(fifoArn, created.Arn);
        Assert.False(created.Existing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "failed-create-response-does-not-cache-queue-and-allows-retry")]
    public async Task FailedCreateResponse_DoesNotFetchAttributesAndAllowsCleanRetryAsync()
    {
        var lookupCalls = 0;
        var createCalls = 0;
        var attributeCalls = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => MissingAsync(),
            nameof(IAmazonSQS.CreateQueueAsync) => CreateAsync(),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => AttributesAsync(),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> MissingAsync()
        {
            Interlocked.Increment(ref lookupCalls);
            return Task.FromException<GetQueueUrlResponse>(new QueueDoesNotExistException("missing"));
        }

        Task<CreateQueueResponse> CreateAsync()
        {
            int call = Interlocked.Increment(ref createCalls);
            return Task.FromResult(new CreateQueueResponse
            {
                QueueUrl = QueueUrl,
                HttpStatusCode = call == 1 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK
            });
        }

        Task<GetQueueAttributesResponse> AttributesAsync()
        {
            Interlocked.Increment(ref attributeCalls);
            return Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
            });
        }

        var declaration = new QueueEntity(1, QueueName, durable: true, autoDelete: false);
        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);

        AmazonSqsTransportException failure = await Assert.ThrowsAsync<AmazonSqsTransportException>(
            () => cache.GetAsync(declaration, TestContext.Current.CancellationToken));
        Assert.Contains("InternalServerError", failure.Message);
        Assert.Equal(0, Volatile.Read(ref attributeCalls));

        QueueInfo recovered = await cache.GetAsync(declaration, TestContext.Current.CancellationToken);
        Assert.False(recovered.Existing);
        Assert.Equal(QueueArn, recovered.Arn);
        Assert.Equal(2, Volatile.Read(ref lookupCalls));
        Assert.Equal(2, Volatile.Read(ref createCalls));
        Assert.Equal(1, Volatile.Read(ref attributeCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "post-create-attribute-failure-retries-existing-queue-without-duplicate-create")]
    public async Task AttributeReadFailureAfterCreation_RediscoversExistingQueueWithoutCreatingAgainAsync()
    {
        var calls = new List<string>();
        var lookups = 0;
        var attributeReads = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => LookupAsync(),
            nameof(IAmazonSQS.CreateQueueAsync) => CreateAsync(),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => AttributesAsync(
                Assert.IsType<string>(args![0]), Assert.IsAssignableFrom<IEnumerable<string>>(args[1])),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<GetQueueUrlResponse> LookupAsync()
        {
            calls.Add("lookup");
            return Interlocked.Increment(ref lookups) == 1
                ? Task.FromException<GetQueueUrlResponse>(new QueueDoesNotExistException("missing"))
                : Task.FromResult(new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
        }

        Task<CreateQueueResponse> CreateAsync()
        {
            calls.Add("create");
            return Task.FromResult(new CreateQueueResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
        }

        Task<GetQueueAttributesResponse> AttributesAsync(string url, IEnumerable<string> requestedAttributes)
        {
            calls.Add("attributes");
            Assert.Equal(QueueUrl, url);
            Assert.Equal([QueueAttributeName.All], requestedAttributes);
            return Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = Interlocked.Increment(ref attributeReads) == 1
                    ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
            });
        }

        var declaration = new QueueEntity(1, QueueName, durable: true, autoDelete: false);
        await using var cache = new QueueCache(client, new AmazonSqsClientContextCacheOptions(), TestContext.Current.CancellationToken);

        AmazonSqsTransportException failure = await Assert.ThrowsAsync<AmazonSqsTransportException>(
            () => cache.GetAsync(declaration, TestContext.Current.CancellationToken));
        Assert.Contains("ServiceUnavailable", failure.Message);
        Assert.Equal(["lookup", "create", "attributes"], calls);

        QueueInfo recovered = await cache.GetAsync(declaration, TestContext.Current.CancellationToken);

        Assert.True(recovered.Existing);
        Assert.Equal(QueueArn, recovered.Arn);
        Assert.Equal(QueueUrl, recovered.Url);
        Assert.Equal(["lookup", "create", "attributes", "lookup", "attributes"], calls);
        Assert.Equal(2, Volatile.Read(ref attributeReads));
        Assert.Same(recovered, await cache.GetByNameAsync(QueueName, TestContext.Current.CancellationToken));
    }
}
