using System.Net;
using System.Reflection;
using global::Amazon;
using global::Amazon.Runtime;
using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.Tests.MessageData;

public sealed class AmazonS3MessageDataObserverTests
{
    private static string PersistedRuleId => string.Join("-", "vicione", "servicebus", "message", "data", "expiration");

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
        Assert.Equal(1, clientProxy.InvocationCount);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException actualCancellation =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => repository.EnsureReadyAsync(cancellation.Token));
        Assert.Equal(cancellation.Token, actualCancellation.CancellationToken);
        Assert.Equal(1, clientProxy.InvocationCount);
        Assert.Equal(CancellationToken.None, clientProxy.ObservedCancellationToken);

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
            Id = PersistedRuleId,
            Status = LifecycleRuleStatus.Enabled,
            Filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecycleTagPredicate
                {
                    Tag = new Tag
                    {
                        Key = PersistedRuleId,
                        Value = AmazonS3MessageDataRepository.LifecycleTagValue,
                    },
                },
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
            rule => rule.Id == PersistedRuleId);
        Assert.Equal(LifecycleRuleStatus.Enabled, actualOwned.Status);
        Assert.Equal(14, actualOwned.Expiration.Days);
        Assert.Empty(actualOwned.Transitions ?? []);
        Assert.Null(actualOwned.AbortIncompleteMultipartUpload);
        Assert.Null(actualOwned.NoncurrentVersionExpiration);
        Assert.Empty(actualOwned.NoncurrentVersionTransitions ?? []);
        Tag lifecycleTag = Assert.IsType<LifecycleTagPredicate>(actualOwned.Filter.LifecycleFilterPredicate).Tag;
        Assert.Equal(PersistedRuleId, lifecycleTag.Key);
        Assert.Equal("enabled", lifecycleTag.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "missing-bucket-and-lifecycle-are-created")]
    public async Task EnsureReady_CreatesMissingBucketAndLifecycleRuleAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.HeadBucketFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchBucket");
        proxy.LifecycleFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchLifecycleConfiguration");
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("missing-message-data", lifecycleExpirationDays: 7));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await repository.EnsureReadyAsync(cancellationToken);

        Assert.Equal("missing-message-data", proxy.PutBucketRequest?.BucketName);
        Assert.Null(proxy.PutBucketRequest?.BucketRegionName);
        Assert.True(proxy.PutBucketRequest?.UseClientRegion);
        PutLifecycleConfigurationRequest lifecycleRequest = Assert.IsType<PutLifecycleConfigurationRequest>(
            proxy.PutRequest);
        LifecycleRule owned = Assert.Single(lifecycleRequest.Configuration.Rules);
        string persistedRuleId = PersistedRuleId;
        Assert.Equal(persistedRuleId, owned.Id);
        Assert.Equal(7, owned.Expiration.Days);
        Assert.Equal(LifecycleRuleStatus.Enabled, owned.Status);
        Tag lifecycleTag = Assert.IsType<LifecycleTagPredicate>(owned.Filter.LifecycleFilterPredicate).Tag;
        Assert.Equal(persistedRuleId, lifecycleTag.Key);
        Assert.Equal("enabled", lifecycleTag.Value);
        Assert.Equal(cancellationToken, proxy.ObservedCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "unmanaged-retention-requires-lifecycle-inspection")]
    public async Task EnsureReady_RequiresLifecycleInspectionWhenRetentionIsUnmanagedAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.LifecycleFailure = S3Failure(HttpStatusCode.Forbidden, "AccessDenied");
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("unmanaged-retention"));

        AmazonS3Exception failure = await Assert.ThrowsAsync<AmazonS3Exception>(
            () => repository.EnsureReadyAsync(TestContext.Current.CancellationToken));

        Assert.Equal("AccessDenied", failure.ErrorCode);
        Assert.Null(proxy.PutRequest);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(null)]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "legacy-untagged-owned-rule-requires-explicit-migration")]
    public async Task EnsureReady_RejectsLegacyOwnedRuleWithoutOverwritingItAsync(int? expirationDays)
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        LifecycleRule legacy = CurrentOwnedRule(14);
        legacy.Filter = new LifecycleFilter
        {
            LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty },
        };
        proxy.Rules = [legacy];
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("legacy-retention", expirationDays));

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EnsureReadyAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Migrate existing objects", failure.Message, StringComparison.Ordinal);
        Assert.Null(proxy.PutRequest);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "unmanaged-retention-preserves-existing-tagged-rule")]
    public async Task EnsureReady_PreservesTaggedRuleWhenRetentionIsUnmanagedAsync()
    {
        LifecycleRule existing = CurrentOwnedRule(14);
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.Rules = [existing];
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("unmanaged-retention"));

        await repository.EnsureReadyAsync(TestContext.Current.CancellationToken);

        Assert.Null(proxy.PutRequest);
        Assert.Same(existing, Assert.Single(proxy.Rules));
    }

    [Theory]
    [InlineData("Enabled")]
    [InlineData("Suspended")]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "versioned-bucket-rejected-for-expiration")]
    public async Task EnsureReady_RejectsVersionedBucketWhenExpirationIsConfiguredAsync(string versioningStatus)
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.VersioningStatus = versioningStatus == "Enabled"
            ? VersionStatus.Enabled
            : VersionStatus.Suspended;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("versioned-message-data", lifecycleExpirationDays: 14));

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EnsureReadyAsync(TestContext.Current.CancellationToken));

        Assert.Contains("versioning", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(proxy.PutRequest);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "concurrent-readiness-is-reconciled-once")]
    public async Task EnsureReady_CoalescesConcurrentFirstCallsAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, CountingReadyS3DispatchProxy>();
        var proxy = (CountingReadyS3DispatchProxy)(object)client;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("concurrent-readiness"));
        Task[] callers = Enumerable.Range(0, 16)
            .Select(_ => repository.EnsureReadyAsync(TestContext.Current.CancellationToken))
            .ToArray();

        proxy.ReleaseHeadBucket();
        await Task.WhenAll(callers);

        Assert.Equal(1, proxy.HeadBucketCalls);
        Assert.Equal(1, proxy.LifecycleCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "bus-restart-revalidates-owned-lifecycle-rule")]
    public async Task PreStart_RevalidatesOwnedRuleAfterAnEarlierSuccessfulStartAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("restart-retention", lifecycleExpirationDays: 14));
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await repository.PreStartAsync(bus);
        LifecycleRule legacy = CurrentOwnedRule(14);
        legacy.Filter = new LifecycleFilter
        {
            LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty },
        };
        proxy.Rules = [legacy];

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.PreStartAsync(bus));

        Assert.Contains("Migrate existing objects", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "AccessDenied")]
    [InlineData(HttpStatusCode.MovedPermanently, "PermanentRedirect")]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "inaccessible-or-misdirected-bucket-fails-closed")]
    public async Task EnsureReady_PropagatesInaccessibleOrMisdirectedBucketFailuresAsync(
        HttpStatusCode statusCode,
        string errorCode)
    {
        LifecycleRule current = CurrentOwnedRule(14);
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        AmazonS3Exception failure = S3Failure(statusCode, errorCode);
        proxy.HeadBucketFailure = failure;
        proxy.Rules = [current];
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("existing-message-data", lifecycleExpirationDays: 14));

        AmazonS3Exception actual = await Assert.ThrowsAsync<AmazonS3Exception>(
            () => repository.EnsureReadyAsync(TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Null(proxy.PutBucketRequest);
        Assert.Null(proxy.PutRequest);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "concurrent-idempotent-bucket-creation-is-accepted")]
    public async Task EnsureReady_AcceptsConcurrentCreationOfTheConfiguredBucketAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.HeadBucketFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchBucket");
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
        proxy.HeadBucketFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchBucket");
        proxy.ClientConfiguration = DispatchProxy.Create<IClientConfig, ClientRegionConfigProxy>();
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("regionless-message-data"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EnsureReadyAsync(TestContext.Current.CancellationToken));

        Assert.Contains("authentication region", exception.Message, StringComparison.Ordinal);
        Assert.Null(proxy.PutBucketRequest);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "bucket-creation-accepts-region-endpoint-without-authentication-region")]
    public async Task EnsureReady_CreatesBucketWithRegionEndpointOnlyAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, LifecycleS3DispatchProxy>();
        var proxy = (LifecycleS3DispatchProxy)(object)client;
        proxy.HeadBucketFailure = S3Failure(HttpStatusCode.NotFound, "NoSuchBucket");
        IClientConfig config = DispatchProxy.Create<IClientConfig, ClientRegionConfigProxy>();
        ((ClientRegionConfigProxy)config).RegionEndpointValue = RegionEndpoint.EUCentral1;
        proxy.ClientConfiguration = config;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("endpoint-region-message-data"));

        await repository.EnsureReadyAsync(TestContext.Current.CancellationToken);

        Assert.Equal("endpoint-region-message-data", proxy.PutBucketRequest?.BucketName);
        Assert.Null(proxy.PutBucketRequest?.BucketRegionName);
        Assert.True(proxy.PutBucketRequest?.UseClientRegion);
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
            Id = PersistedRuleId,
            Status = LifecycleRuleStatus.Enabled,
            Filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecycleTagPredicate
                {
                    Tag = new Tag
                    {
                        Key = PersistedRuleId,
                        Value = AmazonS3MessageDataRepository.LifecycleTagValue,
                    },
                },
            },
            Expiration = new LifecycleRuleExpiration { Days = expirationDays },
        };

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The no-op observer boundary must not invoke the bus.");
    }

    private class CountingReadyS3DispatchProxy : DispatchProxy
    {
        private readonly TaskCompletionSource<HeadBucketResponse> _headBucket =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _headBucketCalls;
        private int _lifecycleCalls;

        public int HeadBucketCalls => Volatile.Read(ref _headBucketCalls);

        public int LifecycleCalls => Volatile.Read(ref _lifecycleCalls);

        public void ReleaseHeadBucket() => _headBucket.TrySetResult(new HeadBucketResponse());

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                nameof(IAmazonS3.HeadBucketAsync) => RecordHeadBucketAsync(),
                nameof(IAmazonS3.GetLifecycleConfigurationAsync) => RecordLifecycleAsync(),
                _ => throw new NotSupportedException(targetMethod?.Name),
            };

        private Task<HeadBucketResponse> RecordHeadBucketAsync()
        {
            Interlocked.Increment(ref _headBucketCalls);
            return _headBucket.Task;
        }

        private Task<GetLifecycleConfigurationResponse> RecordLifecycleAsync()
        {
            Interlocked.Increment(ref _lifecycleCalls);
            return Task.FromResult(new GetLifecycleConfigurationResponse
            {
                Configuration = new LifecycleConfiguration { Rules = [] },
            });
        }
    }

    private class FailingS3DispatchProxy : DispatchProxy
    {
        public Exception Failure { get; set; } = null!;

        public int InvocationCount { get; private set; }

        public CancellationToken ObservedCancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(IAmazonS3.HeadBucketAsync), targetMethod?.Name);
            InvocationCount++;
            ObservedCancellationToken = Assert.IsType<CancellationToken>(args![1]);
            return Task.FromException<HeadBucketResponse>(Failure);
        }
    }

    private class ClientRegionConfigProxy : DispatchProxy
    {
        public RegionEndpoint? RegionEndpointValue { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Contains(targetMethod?.Name, new[] { "get_AuthenticationRegion", "get_RegionEndpoint" });
            return targetMethod?.Name == "get_RegionEndpoint" ? RegionEndpointValue : null;
        }
    }

    private class LifecycleS3DispatchProxy : DispatchProxy
    {
        public Exception? HeadBucketFailure { get; set; }

        public IClientConfig ClientConfiguration { get; set; } = new AmazonS3Config
        {
            AuthenticationRegion = "eu-central-1",
        };

        public Exception? LifecycleFailure { get; set; }

        public PutBucketRequest? PutBucketRequest { get; private set; }

        public Exception? PutBucketFailure { get; set; }

        public IReadOnlyList<LifecycleRule> Rules { get; set; } = [];

        public PutLifecycleConfigurationRequest? PutRequest { get; private set; }

        public VersionStatus? VersioningStatus { get; set; }

        public CancellationToken ObservedCancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_Config":
                    return ClientConfiguration;
                case nameof(IAmazonS3.HeadBucketAsync):
                    ObservedCancellationToken = Assert.IsType<CancellationToken>(args![1]);
                    if (HeadBucketFailure is not null)
                        return Task.FromException<HeadBucketResponse>(HeadBucketFailure);
                    return Task.FromResult(new HeadBucketResponse());
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
                case nameof(IAmazonS3.GetBucketVersioningAsync):
                    ObservedCancellationToken = Assert.IsType<CancellationToken>(args![1]);
                    return Task.FromResult(new GetBucketVersioningResponse
                    {
                        VersioningConfig = new S3BucketVersioningConfig { Status = VersioningStatus },
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
