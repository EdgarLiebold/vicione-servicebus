using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.AmazonS3.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.LocalIntegration.Tests.MessageData;

public sealed class AmazonS3MessageDataRepositoryTests
{
    private static string PersistedRuleId => string.Join("-", "vicione", "servicebus", "message", "data", "expiration");

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

        Assert.Equal("s3", address.Scheme);
        Assert.Equal(fixture.BucketName, address.Host);
        Assert.False(string.IsNullOrWhiteSpace(address.AbsolutePath.TrimStart('/')));
        Assert.Equal(expected, actual.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-PERSISTENCE", "upload-remaining-bytes-preserves-caller-stream-ownership")]
    public async Task Put_UploadsOnlyRemainingBytesAndLeavesCallerStreamOpenAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("stream-boundary");
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await repository.EnsureReadyAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        using var source = new MemoryStream([91, 92, 1, 2, 3], writable: false);
        source.Position = 2;

        Uri address = await repository.PutAsync(source, cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        Assert.True(source.CanRead);
        await using Stream stored = await repository.GetAsync(address, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        using var actual = new MemoryStream();
        await stored.CopyToAsync(actual, cancellationToken);

        Assert.Equal([1, 2, 3], actual.ToArray());
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
                            TaggedRule(PersistedRuleId, 3),
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
            rule => rule.Id == PersistedRuleId);
        Assert.Equal(30, foreign.Expiration.Days);
        Assert.Equal(LifecycleRuleStatus.Enabled, foreign.Status);
        Assert.Equal(
            "foreign/",
            Assert.IsType<LifecyclePrefixPredicate>(foreign.Filter.LifecycleFilterPredicate).Prefix);
        Assert.Equal(14, owned.Expiration.Days);
        Assert.Equal(LifecycleRuleStatus.Enabled, owned.Status);
        Tag lifecycleTag = Assert.IsType<LifecycleTagPredicate>(owned.Filter.LifecycleFilterPredicate).Tag;
        Assert.Equal(PersistedRuleId, lifecycleTag.Key);
        Assert.Equal("enabled", lifecycleTag.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "legacy-untagged-rule-remains-unchanged-pending-migration")]
    public async Task PreStart_RejectsLegacyAllObjectRuleWithoutChangingItAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("legacy-retention");
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
                        Rules = [Rule(PersistedRuleId, 14, string.Empty)],
                    },
                },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName, lifecycleExpirationDays: 14));

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EnsureReadyAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken));
        GetLifecycleConfigurationResponse response = await fixture.Client
            .GetLifecycleConfigurationAsync(fixture.BucketName, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        LifecycleRule owned = Assert.Single(response.Configuration.Rules);

        Assert.Contains("Migrate existing objects", failure.Message, StringComparison.Ordinal);
        Assert.Equal(14, owned.Expiration.Days);
        Assert.Equal(string.Empty,
            Assert.IsType<LifecyclePrefixPredicate>(owned.Filter.LifecycleFilterPredicate).Prefix);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "versioned-bucket-rejected-before-retention-rule-and-upload")]
    public async Task VersionedBucket_RejectsStartupAndDirectTtlUploadAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("versioned-retention");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await fixture.Client.PutBucketAsync(
                new PutBucketRequest
                {
                    BucketName = fixture.BucketName,
                    BucketRegionName = fixture.Client.Config.AuthenticationRegion,
                },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await fixture.Client.PutBucketVersioningAsync(
                new PutBucketVersioningRequest
                {
                    BucketName = fixture.BucketName,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Enabled,
                    },
                },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName, lifecycleExpirationDays: 14));

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EnsureReadyAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken));

        Assert.Contains("versioning", failure.Message, StringComparison.OrdinalIgnoreCase);
        GetLifecycleConfigurationResponse lifecycle = await fixture.Client
            .GetLifecycleConfigurationAsync(fixture.BucketName, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        Assert.Empty(lifecycle.Configuration?.Rules ?? []);

        InvalidOperationException directUploadFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.PutAsync(
                    new MemoryStream([1], writable: false), TimeSpan.FromDays(14), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));
        Assert.Contains("versioning", directUploadFailure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "versioning-enabled-after-ready-rejects-next-ttl-upload")]
    public async Task Put_RejectsTtlUploadAfterBucketVersioningIsEnabledAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("versioning-change");
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName, lifecycleExpirationDays: 14));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri first = await repository.PutAsync(
                new MemoryStream([1], writable: false), TimeSpan.FromDays(14), cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await fixture.Client.PutBucketVersioningAsync(
                new PutBucketVersioningRequest
                {
                    BucketName = fixture.BucketName,
                    VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Enabled },
                },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.PutAsync(
                    new MemoryStream([2], writable: false), TimeSpan.FromDays(14), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));
        ListObjectsV2Response objects = await fixture.Client.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = fixture.BucketName },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Contains("versioning", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(first.AbsolutePath.TrimStart('/'), Assert.Single(objects.S3Objects).Key);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "only-explicit-retention-uploads-receive-lifecycle-tag")]
    public async Task Put_TagsOnlyUploadsWithExplicitRetentionAsync()
    {
        await using AmazonS3TestBucket fixture = AmazonS3TestBucket.Create("retention-tag");
        var repository = new AmazonS3MessageDataRepository(
            fixture.Client,
            new AmazonS3MessageDataRepositoryOptions(fixture.BucketName, lifecycleExpirationDays: 14));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Uri indefinite = await repository.PutAsync(
                new MemoryStream([1], writable: false), cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        Uri expiring = await repository.PutAsync(
                new MemoryStream([2], writable: false), TimeSpan.FromDays(14), cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        GetObjectTaggingResponse indefiniteTags = await fixture.Client.GetObjectTaggingAsync(
                new GetObjectTaggingRequest { BucketName = fixture.BucketName, Key = indefinite.AbsolutePath.TrimStart('/') },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        GetObjectTaggingResponse expiringTags = await fixture.Client.GetObjectTaggingAsync(
                new GetObjectTaggingRequest { BucketName = fixture.BucketName, Key = expiring.AbsolutePath.TrimStart('/') },
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Empty(indefiniteTags.Tagging ?? []);
        Tag tag = Assert.Single(expiringTags.Tagging ?? []);
        Assert.Equal(PersistedRuleId, tag.Key);
        Assert.Equal("enabled", tag.Value);
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

    private static LifecycleRule TaggedRule(string id, int expirationDays) =>
        new()
        {
            Id = id,
            Status = LifecycleRuleStatus.Enabled,
            Filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecycleTagPredicate
                {
                    Tag = new Tag { Key = id, Value = AmazonS3MessageDataRepository.LifecycleTagValue },
                },
            },
            Expiration = new LifecycleRuleExpiration { Days = expirationDays },
        };
}
