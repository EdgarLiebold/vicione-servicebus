using System.Net;
using System.Reflection;
using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.Tests.MessageData;

public sealed class AmazonS3MessageDataObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-OBSERVER", "lifecycle-no-op-and-prestart-fail-fast-boundary")]
    public async Task BusObserverLifecycle_IsNoOpExceptFailFastPreStartAsync()
    {
        var client = DispatchProxy.Create<IAmazonS3, FailingS3DispatchProxy>();
        var clientProxy = (FailingS3DispatchProxy)(object)client;
        var startupFailure = new InvalidOperationException("causal S3 startup failure");
        clientProxy.Failure = startupFailure;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("observer-message-data"));
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        var failure = new InvalidOperationException("causal observer failure");

        repository.PostCreate(bus);
        repository.CreateFaulted(failure);
        await repository.PostStartAsync(bus, Task.FromResult<BusReady>(null!));
        await repository.StartFaultedAsync(bus, failure);
        await repository.PreStopAsync(bus);
        await repository.PostStopAsync(bus);
        await repository.StopFaultedAsync(bus, failure);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.PreStartAsync(bus));
        Assert.Same(startupFailure, actual);
        Assert.Equal(CancellationToken.None, clientProxy.ObservedCancellationToken);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancellationFailure = new OperationCanceledException(cancellation.Token);
        clientProxy.Failure = cancellationFailure;
        OperationCanceledException actualCancellation =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => repository.EnsureReadyAsync(cancellation.Token));
        Assert.Same(cancellationFailure, actualCancellation);
        Assert.Equal(cancellation.Token, clientProxy.ObservedCancellationToken);

        Assert.Throws<ArgumentNullException>(() => repository.PostCreate(null!));
        Assert.Throws<ArgumentNullException>(() => repository.CreateFaulted(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PreStartAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.PostStartAsync(null!, Task.FromResult<BusReady>(null!)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PostStartAsync(bus, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StartFaultedAsync(null!, failure));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StartFaultedAsync(bus, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PreStopAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PostStopAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StopFaultedAsync(null!, failure));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StopFaultedAsync(bus, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "owned-rule-is-canonical-and-foreign-rule-is-preserved")]
    public async Task EnsureReady_CanonicalizesOnlyTheProductOwnedLifecycleRuleAsync()
    {
        var foreignTransition = new LifecycleTransition
        {
            Days = 90,
            StorageClass = S3StorageClass.Glacier,
        };
        var foreign = new LifecycleRule
        {
            Id = "caller-owned-archive",
            Status = LifecycleRuleStatus.Enabled,
            Filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = "caller/" },
            },
            Expiration = new LifecycleRuleExpiration { Days = 365 },
            Transitions = [foreignTransition],
        };
        var legacyForeign = new LifecycleRule
        {
            Id = "caller-owned-legacy-prefix",
            Status = LifecycleRuleStatus.Enabled,
            Expiration = new LifecycleRuleExpiration { Days = 180 },
        };
        typeof(LifecycleRule).GetProperty("Prefix")!.SetValue(legacyForeign, "legacy/");
        var ownedWithUnconfiguredAction = new LifecycleRule
        {
            Id = AmazonS3MessageDataRepository.LifecycleRuleId,
            Status = LifecycleRuleStatus.Enabled,
            Filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty },
            },
            Expiration = new LifecycleRuleExpiration { Days = 14 },
            Transitions =
            [
                new LifecycleTransition
                {
                    Days = 30,
                    StorageClass = S3StorageClass.Glacier,
                },
            ],
        };
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.Rules = [foreign, legacyForeign, ownedWithUnconfiguredAction];
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("canonical-lifecycle", lifecycleExpirationDays: 14));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await repository.EnsureReadyAsync(cancellationToken);

        PutLifecycleConfigurationRequest request = Assert.IsType<PutLifecycleConfigurationRequest>(proxy.PutRequest);
        Assert.Equal(cancellationToken, proxy.ObservedCancellationToken);
        LifecycleRule actualForeign = Assert.Single(request.Configuration.Rules, rule => rule.Id == foreign.Id);
        Assert.Same(foreign, actualForeign);
        Assert.Equal(365, actualForeign.Expiration.Days);
        Assert.Equal("caller/", Assert.IsType<LifecyclePrefixPredicate>(actualForeign.Filter.LifecycleFilterPredicate).Prefix);
        LifecycleTransition actualForeignTransition = Assert.Single(actualForeign.Transitions);
        Assert.Equal(90, actualForeignTransition.Days);
        Assert.Equal(S3StorageClass.Glacier, actualForeignTransition.StorageClass);
        LifecycleRule actualLegacyForeign = Assert.Single(
            request.Configuration.Rules,
            rule => rule.Id == legacyForeign.Id);
        Assert.Same(legacyForeign, actualLegacyForeign);
        Assert.Equal("legacy/", typeof(LifecycleRule).GetProperty("Prefix")!.GetValue(actualLegacyForeign));
        Assert.Equal(180, actualLegacyForeign.Expiration.Days);
        LifecycleRule actualOwned = Assert.Single(
            request.Configuration.Rules,
            rule => rule.Id == AmazonS3MessageDataRepository.LifecycleRuleId);
        Assert.Equal(LifecycleRuleStatus.Enabled, actualOwned.Status);
        Assert.Equal(14, actualOwned.Expiration.Days);
        Assert.Empty(actualOwned.Transitions ?? []);
        Assert.Null(actualOwned.AbortIncompleteMultipartUpload);
        Assert.Null(actualOwned.NoncurrentVersionExpiration);
        Assert.Empty(actualOwned.NoncurrentVersionTransitions ?? []);
        Assert.Equal(
            string.Empty,
            Assert.IsType<LifecyclePrefixPredicate>(actualOwned.Filter.LifecycleFilterPredicate).Prefix);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "missing-bucket-and-lifecycle-are-created")]
    public async Task EnsureReady_CreatesMissingBucketAndLifecycleRuleAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.BucketAclFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchBucket");
        proxy.LifecycleFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchLifecycleConfiguration");
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("missing-message-data", lifecycleExpirationDays: 7));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await repository.EnsureReadyAsync(cancellationToken);

        Assert.Equal("missing-message-data", proxy.PutBucketRequest?.BucketName);
        Assert.Equal("eu-central-1", proxy.PutBucketRequest?.BucketRegionName);
        PutLifecycleConfigurationRequest lifecycleRequest = Assert.IsType<PutLifecycleConfigurationRequest>(
            proxy.PutRequest);
        LifecycleRule owned = Assert.Single(lifecycleRequest.Configuration.Rules);
        Assert.Equal(AmazonS3MessageDataRepository.LifecycleRuleId, owned.Id);
        Assert.Equal(7, owned.Expiration.Days);
        Assert.Equal(LifecycleRuleStatus.Enabled, owned.Status);
        Assert.Equal(
            string.Empty,
            Assert.IsType<LifecyclePrefixPredicate>(owned.Filter.LifecycleFilterPredicate).Prefix);
        Assert.Equal(cancellationToken, proxy.ObservedCancellationToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "AccessDenied")]
    [InlineData(HttpStatusCode.MovedPermanently, "PermanentRedirect")]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "inaccessible-existing-bucket-and-current-rule-are-no-op")]
    public async Task EnsureReady_TreatsAnInaccessibleBucketWithCurrentRuleAsReadyAsync(
        HttpStatusCode statusCode,
        string errorCode)
    {
        LifecycleRule current = CurrentOwnedRule(14);
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.BucketAclFailure = S3Failure(statusCode, errorCode);
        proxy.Rules = [current];
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("existing-message-data", lifecycleExpirationDays: 14));

        await repository.EnsureReadyAsync(TestContext.Current.CancellationToken);

        Assert.Null(proxy.PutBucketRequest);
        Assert.Null(proxy.PutRequest);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "concurrent-idempotent-bucket-creation-is-accepted")]
    public async Task EnsureReady_AcceptsConcurrentCreationOfTheConfiguredBucketAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.BucketAclFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchBucket");
        proxy.PutBucketFailure = S3Failure(HttpStatusCode.Conflict, "BucketAlreadyOwnedByYou");
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("concurrent-message-data"));

        await repository.EnsureReadyAsync(TestContext.Current.CancellationToken);

        Assert.Equal("concurrent-message-data", proxy.PutBucketRequest?.BucketName);
        Assert.Null(proxy.PutRequest);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "bucket-creation-requires-client-region")]
    public async Task EnsureReady_RequiresAClientRegionBeforeCreatingABucketAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.BucketAclFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchBucket");
        proxy.ClientConfiguration = new AmazonS3Config();
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("regionless-message-data"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EnsureReadyAsync(TestContext.Current.CancellationToken));

        Assert.Contains("authentication region", exception.Message, StringComparison.Ordinal);
        Assert.Null(proxy.PutBucketRequest);
    }

    private static AmazonS3Exception S3Failure(HttpStatusCode statusCode, string errorCode) =>
        new("simulated Amazon S3 failure")
        {
            StatusCode = statusCode,
            ErrorCode = errorCode,
        };

    private static LifecycleRule CurrentOwnedRule(int expirationDays) =>
        new()
        {
            Id = AmazonS3MessageDataRepository.LifecycleRuleId,
            Status = LifecycleRuleStatus.Enabled,
            Filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty },
            },
            Expiration = new LifecycleRuleExpiration { Days = expirationDays },
        };

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The no-op observer boundary must not invoke the bus.");
    }

    private class FailingS3DispatchProxy : DispatchProxy
    {
        public Exception Failure { get; set; } = null!;

        public CancellationToken ObservedCancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(IAmazonS3.GetBucketAclAsync), targetMethod?.Name);
            ObservedCancellationToken = Assert.IsType<CancellationToken>(args![1]);
            return Task.FromException<GetBucketAclResponse>(Failure);
        }
    }

    private class LifecycleS3DispatchProxy : DispatchProxy
    {
        public Exception? BucketAclFailure { get; set; }

        public AmazonS3Config ClientConfiguration { get; set; } = new()
        {
            AuthenticationRegion = "eu-central-1",
        };

        public Exception? LifecycleFailure { get; set; }

        public PutBucketRequest? PutBucketRequest { get; private set; }

        public Exception? PutBucketFailure { get; set; }

        public IReadOnlyList<LifecycleRule> Rules { get; set; } = [];

        public PutLifecycleConfigurationRequest? PutRequest { get; private set; }

        public CancellationToken ObservedCancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_Config":
                    return ClientConfiguration;
                case nameof(IAmazonS3.GetBucketAclAsync):
                    ObservedCancellationToken = Assert.IsType<CancellationToken>(args![1]);
                    if (BucketAclFailure is not null)
                        return Task.FromException<GetBucketAclResponse>(BucketAclFailure);
                    return Task.FromResult(new GetBucketAclResponse());
                case nameof(IAmazonS3.PutBucketAsync):
                    PutBucketRequest = Assert.IsType<PutBucketRequest>(args![0]);
                    ObservedCancellationToken = Assert.IsType<CancellationToken>(args[1]);
                    if (PutBucketFailure is not null)
                        return Task.FromException<PutBucketResponse>(PutBucketFailure);
                    return Task.FromResult(new PutBucketResponse());
                case nameof(IAmazonS3.GetLifecycleConfigurationAsync):
                    ObservedCancellationToken = Assert.IsType<CancellationToken>(args![1]);
                    if (LifecycleFailure is not null)
                        return Task.FromException<GetLifecycleConfigurationResponse>(LifecycleFailure);
                    return Task.FromResult(
                        new GetLifecycleConfigurationResponse
                        {
                            Configuration = new LifecycleConfiguration { Rules = [.. Rules] },
                        });
                case nameof(IAmazonS3.PutLifecycleConfigurationAsync):
                    PutRequest = Assert.IsType<PutLifecycleConfigurationRequest>(args![0]);
                    ObservedCancellationToken = Assert.IsType<CancellationToken>(args[1]);
                    return Task.FromResult(new PutLifecycleConfigurationResponse());
                default:
                    throw new NotSupportedException(targetMethod?.Name);
            }
        }
    }
}
