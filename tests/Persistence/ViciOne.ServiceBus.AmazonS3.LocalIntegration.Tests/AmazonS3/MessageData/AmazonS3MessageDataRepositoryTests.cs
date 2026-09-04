using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.AmazonS3.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.LocalIntegration.Tests.AmazonS3.MessageData;

public sealed class AmazonS3MessageDataRepositoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-PERSISTENCE", "exact-bytes-round-trip-through-run-scoped-bucket")]
    public async Task PutAndGet_RoundTripExactBytesThroughRunScopedBucketAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("roundtrip");
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] expected = [0, 1, 2, 127, 128, 254, 255, 42, 17];

        await repository.EnsureReadyAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        Uri address = await repository
            .PutAsync(new MemoryStream(expected, writable: false), cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await using Stream stored = await repository
            .GetAsync(address, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        using var actual = new MemoryStream();
        await stored.CopyToAsync(actual, cancellationToken);

        Assert.Equal("urn", address.Scheme);
        Assert.StartsWith("urn:file:", address.OriginalString, StringComparison.Ordinal);
        Assert.Equal(expected, actual.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "missing-bucket-created-before-ready")]
    public async Task PreStart_CreatesMissingBucketAndReportsReadyOnlyAfterSuccessAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("startup");
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.False(await global::Amazon.S3.Util.AmazonS3Util
            .DoesS3BucketExistV2Async(fixture.Client, fixture.BucketName)
            .WaitAsync(fixture.OperationTimeout, cancellationToken));

        await repository.EnsureReadyAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.True(await global::Amazon.S3.Util.AmazonS3Util
            .DoesS3BucketExistV2Async(fixture.Client, fixture.BucketName)
            .WaitAsync(fixture.OperationTimeout, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "owned-rule-reconciled-without-foreign-rule-loss")]
    public async Task PreStart_ReconcilesOnlyTheOwnedLifecycleRuleAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("lifecycle");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await fixture.Client.PutBucketAsync(
                new PutBucketRequest
                {
                    BucketName = fixture.BucketName,
                    BucketRegionName = fixture.Client.Config.AuthenticationRegion,
                },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await fixture.Client.PutLifecycleConfigurationAsync(
                new PutLifecycleConfigurationRequest
                {
                    BucketName = fixture.BucketName,
                    Configuration = new LifecycleConfiguration
                    {
                        Rules =
                        [
                            Rule("foreign-archive-rule", 30, "foreign/"),
                            Rule(AmazonS3MessageDataRepository.LifecycleRuleId, 3, "stale/"),
                        ],
                    },
                },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName, lifecycleExpirationDays: 14));

        await repository.EnsureReadyAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

        GetLifecycleConfigurationResponse response = await fixture.Client
            .GetLifecycleConfigurationAsync(fixture.BucketName, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        LifecycleRule foreign = Assert.Single(
            response.Configuration.Rules,
            rule => rule.Id == "foreign-archive-rule");
        LifecycleRule owned = Assert.Single(
            response.Configuration.Rules,
            rule => rule.Id == AmazonS3MessageDataRepository.LifecycleRuleId);
        Assert.Equal(30, foreign.Expiration.Days);
        Assert.Equal(LifecycleRuleStatus.Enabled, foreign.Status);
        Assert.Equal(
            "foreign/",
            Assert.IsType<LifecyclePrefixPredicate>(foreign.Filter.LifecycleFilterPredicate).Prefix);
        Assert.Equal(14, owned.Expiration.Days);
        Assert.Equal(LifecycleRuleStatus.Enabled, owned.Status);
        Assert.Equal(
            string.Empty,
            Assert.IsType<LifecyclePrefixPredicate>(owned.Filter.LifecycleFilterPredicate).Prefix);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CANCELLATION", "put-and-get-honor-caller-cancellation")]
    public async Task PutAndGet_HonorCallerCancellationAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("cancellation");
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName));
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        await repository.EnsureReadyAsync(testCancellation).WaitAsync(fixture.OperationTimeout, testCancellation);
        Uri address = await repository
            .PutAsync(new MemoryStream([1, 2, 3], writable: false), cancellationToken: testCancellation)
            .WaitAsync(fixture.OperationTimeout, testCancellation);
        using var callerCancellation = new CancellationTokenSource();
        callerCancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.PutAsync(
                new MemoryStream([4, 5, 6], writable: false),
                cancellationToken: callerCancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetAsync(address, callerCancellation.Token));
    }

    private static LifecycleRule Rule(string id, int expirationDays, string prefix) =>
        new()
        {
            Id = id,
            Status = LifecycleRuleStatus.Enabled,
            Filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = prefix },
            },
            Expiration = new LifecycleRuleExpiration { Days = expirationDays },
        };
}
