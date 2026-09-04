using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;

internal sealed class AmazonSqsLocalStack : IAsyncDisposable
{
    private readonly AmazonSimpleNotificationServiceClient _sns;
    private readonly AmazonS3Client _s3;
    private readonly AmazonSQSClient _sqs;

    private AmazonSqsLocalStack(Uri endpoint, string region, TimeSpan operationTimeout, string prefix)
    {
        Endpoint = endpoint;
        Region = region;
        OperationTimeout = operationTimeout;
        Prefix = prefix;
        _sqs = CreateSqsClient();
        _sns = CreateSnsClient();
        _s3 = CreateS3Client();
    }

    public Uri Endpoint { get; }
    public string Region { get; }
    public TimeSpan OperationTimeout { get; }
    public string Prefix { get; }

    public IAmazonS3 S3Client => _s3;

    public static AmazonSqsLocalStack Create(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        ViciOneTestOptions options = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.LocalStack);
        LocalStackLocalOptions localStack = options.LocalInfrastructure!.LocalStack!;
        return new AmazonSqsLocalStack(
            new UriBuilder(Uri.UriSchemeHttp, localStack.Host, localStack.Port!.Value).Uri,
            localStack.Region!,
            options.OperationTimeout!.Value,
            CreatePrefix(purpose, Guid.NewGuid()));
    }

    internal static string CreatePrefix(string purpose, Guid runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        string normalizedPurpose = new(purpose.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        if (normalizedPurpose.Length == 0)
            normalizedPurpose = "test";
        if (normalizedPurpose.Length > 16)
            normalizedPurpose = normalizedPurpose[..16];

        return $"vsb-{normalizedPurpose}-{runId:N}";
    }

    public string Name(string purpose, bool fifo = false)
    {
        string suffix = new(purpose.ToLowerInvariant().Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_').ToArray());
        if (suffix.Length == 0)
            throw new ArgumentException("The resource purpose must contain at least one AWS name character.", nameof(purpose));

        string name = $"{Prefix}-{suffix}";
        int maximumBaseLength = fifo ? 75 : 80;
        if (name.Length > maximumBaseLength)
            name = name[..maximumBaseLength];
        return fifo ? name + ".fifo" : name;
    }

    public string BucketName(string purpose)
    {
        string suffix = new(purpose.ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '-')
            .ToArray());
        if (suffix.Length == 0)
            throw new ArgumentException("The resource purpose must contain at least one S3 bucket-name character.", nameof(purpose));

        string name = $"{Prefix}-{suffix}";
        return name.Length <= 63 ? name : name[..63].TrimEnd('-');
    }

    public void ConfigureHost(IAmazonSqsBusFactoryConfigurator configurator) =>
        ConfigureHost(configurator, (Action<IAmazonSqsHostConfigurator>?)null);

    public void ConfigureHost(IAmazonSqsBusFactoryConfigurator configurator, bool scopeTopics) =>
        ConfigureHost(configurator, scopeTopics, null);

    public void ConfigureHost(
        IAmazonSqsBusFactoryConfigurator configurator,
        Action<IAmazonSqsHostConfigurator>? configure) =>
        ConfigureHost(configurator, scopeTopics: true, configure);

    private void ConfigureHost(
        IAmazonSqsBusFactoryConfigurator configurator,
        bool scopeTopics,
        Action<IAmazonSqsHostConfigurator>? configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Host(new Uri($"amazonsqs://{Region}"), host =>
        {
            host.Scope(Prefix, scopeTopics);
            host.ClientFactories(CreateSqsClient, CreateSnsClient);
            configure?.Invoke(host);
        });
    }

    public void ConfigureHost(IAmazonSqsBusFactoryConfigurator configurator, AWSCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(credentials);

        configurator.Host(new Uri($"amazonsqs://{Region}"), host =>
        {
            host.Scope(Prefix, true);
            host.ClientFactories(() => CreateSqsClient(credentials), () => CreateSnsClient(credentials));
        });
    }

    public static AWSCredentials CreateRunCredentials()
    {
        string accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")
            ?? throw new InvalidOperationException("The canonical LocalStack runner did not project AWS_ACCESS_KEY_ID.");
        string secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")
            ?? throw new InvalidOperationException("The canonical LocalStack runner did not project AWS_SECRET_ACCESS_KEY.");
        string? sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

        return string.IsNullOrEmpty(sessionToken)
            ? new BasicAWSCredentials(accessKey, secretKey)
            : new SessionAWSCredentials(accessKey, secretKey, sessionToken);
    }

    public AmazonSQSClient CreateSqsClient() =>
        new(new AmazonSQSConfig
        {
            ServiceURL = Endpoint.AbsoluteUri.TrimEnd('/'),
            AuthenticationRegion = Region,
            MaxErrorRetry = 0,
        });

    public AmazonSQSClient CreateSqsClient(AWSCredentials credentials) =>
        new(credentials, new AmazonSQSConfig
        {
            ServiceURL = Endpoint.AbsoluteUri.TrimEnd('/'),
            AuthenticationRegion = Region,
            MaxErrorRetry = 0,
        });

    public AmazonSimpleNotificationServiceClient CreateSnsClient() =>
        new(new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = Endpoint.AbsoluteUri.TrimEnd('/'),
            AuthenticationRegion = Region,
            MaxErrorRetry = 0,
        });

    public AmazonSimpleNotificationServiceClient CreateSnsClient(AWSCredentials credentials) =>
        new(credentials, new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = Endpoint.AbsoluteUri.TrimEnd('/'),
            AuthenticationRegion = Region,
            MaxErrorRetry = 0,
        });

    public AmazonS3Client CreateS3Client() =>
        new(CreateRunCredentials(), new AmazonS3Config
        {
            ServiceURL = Endpoint.AbsoluteUri.TrimEnd('/'),
            AuthenticationRegion = Region,
            ForcePathStyle = true,
            MaxErrorRetry = 0,
        });

    public async Task<string[]> ListOwnedTopicNames(CancellationToken cancellationToken)
    {
        var names = new List<string>();
        string? token = null;
        do
        {
            ListTopicsResponse response = await _sns.ListTopicsAsync(
                    new ListTopicsRequest { NextToken = token },
                    cancellationToken)
                .WaitAsync(OperationTimeout, cancellationToken);
            names.AddRange((response.Topics ?? [])
                .Select(topic => topic.TopicArn[(topic.TopicArn.LastIndexOf(':') + 1)..])
                .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal)));
            token = response.NextToken;
        }
        while (!string.IsNullOrEmpty(token));

        return names.Order(StringComparer.Ordinal).ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        using var cleanup = new CancellationTokenSource(OperationTimeout);
        try
        {
            string? queueToken = null;
            do
            {
                ListQueuesResponse queues = await _sqs.ListQueuesAsync(new ListQueuesRequest { NextToken = queueToken }, cleanup.Token);
                foreach (string queueUrl in queues.QueueUrls ?? [])
                {
                    string queueName = queueUrl[(queueUrl.LastIndexOf('/') + 1)..];
                    if (queueName.StartsWith(Prefix, StringComparison.Ordinal))
                        await _sqs.DeleteQueueAsync(queueUrl, cleanup.Token);
                }
                queueToken = queues.NextToken;
            }
            while (!string.IsNullOrEmpty(queueToken));

            string? topicToken = null;
            do
            {
                ListTopicsResponse topics = await _sns.ListTopicsAsync(new ListTopicsRequest { NextToken = topicToken }, cleanup.Token);
                foreach (Topic topic in topics.Topics ?? [])
                {
                    string topicName = topic.TopicArn[(topic.TopicArn.LastIndexOf(':') + 1)..];
                    if (topicName.StartsWith(Prefix, StringComparison.Ordinal))
                        await _sns.DeleteTopicAsync(topic.TopicArn, cleanup.Token);
                }
                topicToken = topics.NextToken;
            }
            while (!string.IsNullOrEmpty(topicToken));

            ListBucketsResponse buckets = await _s3.ListBucketsAsync(cleanup.Token);
            foreach (S3Bucket bucket in buckets.Buckets ?? [])
            {
                if (!bucket.BucketName.StartsWith(Prefix, StringComparison.Ordinal))
                    continue;

                string? objectToken = null;
                do
                {
                    ListObjectsV2Response objects = await _s3.ListObjectsV2Async(
                        new ListObjectsV2Request
                        {
                            BucketName = bucket.BucketName,
                            ContinuationToken = objectToken,
                        },
                        cleanup.Token);
                    foreach (S3Object item in objects.S3Objects ?? [])
                        await _s3.DeleteObjectAsync(bucket.BucketName, item.Key, cleanup.Token);

                    objectToken = objects.IsTruncated == true ? objects.NextContinuationToken : null;
                }
                while (objectToken is not null);

                await _s3.DeleteBucketAsync(bucket.BucketName, cleanup.Token);
            }
        }
        finally
        {
            _s3.Dispose();
            _sns.Dispose();
            _sqs.Dispose();
        }
    }
}
