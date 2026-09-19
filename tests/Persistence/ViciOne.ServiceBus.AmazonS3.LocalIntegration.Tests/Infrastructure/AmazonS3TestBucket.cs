using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.AmazonS3.LocalIntegration.Tests.Infrastructure;

internal sealed class AmazonS3TestBucket : IAsyncDisposable
{
    private readonly AmazonS3Client _client;
    private readonly TimeSpan _operationTimeout;

    private AmazonS3TestBucket(AmazonS3Client client, string bucketName, TimeSpan operationTimeout)
    {
        _client = client;
        BucketName = bucketName;
        _operationTimeout = operationTimeout;
    }

    public IAmazonS3 Client => _client;

    public string BucketName { get; }

    public TimeSpan OperationTimeout => _operationTimeout;

    public static AmazonS3TestBucket Create(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.LocalStack);
        LocalStackLocalOptions localStack = testOptions.LocalInfrastructure!.LocalStack!;
        var endpoint = new UriBuilder(Uri.UriSchemeHttp, localStack.Host, localStack.Port!.Value).Uri;
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint.AbsoluteUri.TrimEnd('/'),
            AuthenticationRegion = localStack.Region,
            ForcePathStyle = true,
            MaxErrorRetry = 0,
        };

        string prefix = string.Concat(purpose.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit));
        if (prefix.Length == 0)
            prefix = "test";
        if (prefix.Length > 20)
            prefix = prefix[..20];

        string bucketName = $"vicione-{prefix}-{Guid.NewGuid():N}";
        return new AmazonS3TestBucket(
            new AmazonS3Client(config),
            bucketName,
            testOptions.OperationTimeout!.Value);
    }

    public async ValueTask DisposeAsync()
    {
        using var cleanup = new CancellationTokenSource(_operationTimeout);
        try
        {
            string? continuationToken = null;
            do
            {
                ListObjectsV2Response listed = await _client.ListObjectsV2Async(
                    new ListObjectsV2Request
                    {
                        BucketName = BucketName,
                        ContinuationToken = continuationToken,
                    },
                    cleanup.Token);

                foreach (S3Object item in listed.S3Objects ?? [])
                    await _client.DeleteObjectAsync(BucketName, item.Key, cleanup.Token);

                continuationToken = listed.IsTruncated == true ? listed.NextContinuationToken : null;
            }
            while (continuationToken is not null);

            string? keyMarker = null;
            string? versionIdMarker = null;
            do
            {
                ListVersionsResponse versions = await _client.ListVersionsAsync(
                    new ListVersionsRequest
                    {
                        BucketName = BucketName,
                        KeyMarker = keyMarker,
                        VersionIdMarker = versionIdMarker,
                    },
                    cleanup.Token);

                foreach (S3ObjectVersion version in versions.Versions ?? [])
                {
                    await _client.DeleteObjectAsync(
                        new DeleteObjectRequest
                        {
                            BucketName = BucketName,
                            Key = version.Key,
                            VersionId = version.VersionId,
                        },
                        cleanup.Token);
                }

                keyMarker = versions.IsTruncated == true ? versions.NextKeyMarker : null;
                versionIdMarker = versions.IsTruncated == true ? versions.NextVersionIdMarker : null;
            }
            while (keyMarker is not null || versionIdMarker is not null);

            await _client.DeleteBucketAsync(BucketName, cleanup.Token);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // The test never created the bucket, or already removed it through the product path.
        }
        finally
        {
            _client.Dispose();
        }
    }
}
