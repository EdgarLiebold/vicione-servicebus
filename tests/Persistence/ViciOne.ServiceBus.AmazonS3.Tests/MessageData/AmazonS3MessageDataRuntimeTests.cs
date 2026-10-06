using System.Reflection;
using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.Tests.MessageData;

public sealed class AmazonS3MessageDataRuntimeTests
{
    private const string Bucket = "runtime-message-data";
    private static string RuleId => string.Join("-", "vicione", "servicebus", "message", "data", "expiration");

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-PERSISTENCE", "sdk-upload-preserves-position-and-caller-ownership")]
    public async Task Put_UploadsRemainingBytesWithoutClosingOrResettingCallerStreamAsync()
    {
        var (repository, proxy) = CreateRepository();
        using var input = new MemoryStream([91, 92, 1, 2, 3], writable: false);
        input.Position = 2;
        using var cancellation = new CancellationTokenSource();

        Uri address = await repository.PutAsync(input, cancellationToken: cancellation.Token);

        Upload upload = Assert.Single(proxy.Uploads);
        Assert.Same(input, upload.InputStream);
        Assert.Equal(2, upload.InitialPosition);
        Assert.False(upload.AutoCloseStream);
        Assert.False(upload.AutoResetStreamPosition);
        Assert.Equal([1, 2, 3], upload.Payload);
        Assert.Equal(Bucket, upload.BucketName);
        Assert.Equal("s3", address.Scheme);
        Assert.Equal(Bucket, address.Host);
        Assert.Equal(upload.Key, address.AbsolutePath.TrimStart('/'));
        Assert.NotEmpty(upload.Key);
        Assert.All(upload.Key, character => Assert.True(char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
        Assert.Empty(upload.Tags);
        Assert.Equal(cancellation.Token, upload.Token);
        Assert.True(input.CanRead);
        Assert.Equal(input.Length, input.Position);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "sdk-ttl-tag-and-ready-versioning-recheck")]
    public async Task Put_TagsOnlyExplicitRetentionAndRejectsLaterVersioningBeforeUploadAsync()
    {
        var (repository, proxy) = CreateRepository(14);
        using var indefinite = new MemoryStream([1]);
        using var expiring = new MemoryStream([2]);
        using var rejected = new MemoryStream([3]);

        CancellationToken token = TestContext.Current.CancellationToken;
        Uri first = await repository.PutAsync(indefinite, cancellationToken: token);
        Uri second = await repository.PutAsync(expiring, TimeSpan.FromDays(14), token);
        proxy.VersionStatus = VersionStatus.Enabled;
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.PutAsync(rejected, TimeSpan.FromDays(14), token));

        Assert.Contains("versioning", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, proxy.Uploads.Count);
        Assert.Empty(proxy.Uploads[0].Tags);
        Tag tag = Assert.Single(proxy.Uploads[1].Tags);
        Assert.Equal(RuleId, tag.Key);
        Assert.Equal("enabled", tag.Value);
        Assert.NotEqual(first, second);
        Assert.Equal(3, proxy.VersioningRequests);
        Assert.Equal(0, rejected.Position);
        Assert.True(rejected.CanRead);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-PERSISTENCE", "sdk-download-stream-lives-until-caller-disposes")]
    public async Task Get_ReturnsReadableResponseStreamWithoutDisposingItOrClientAsync()
    {
        var (repository, proxy) = CreateRepository();
        using var responseStream = new TrackingStream([0, 127, 128, 255, 42]);
        proxy.DownloadStream = responseStream;
        using var cancellation = new CancellationTokenSource();

        Stream result = await repository.GetAsync(new Uri($"s3://{Bucket}/key_A-9"), cancellation.Token);

        Assert.Same(responseStream, result);
        Assert.Equal(Bucket, proxy.GetRequest?.BucketName);
        Assert.Equal("key_A-9", proxy.GetRequest?.Key);
        Assert.Equal(cancellation.Token, proxy.GetToken);
        Assert.Equal(0, responseStream.DisposeCount);
        using var bytes = new MemoryStream();
        await result.CopyToAsync(bytes, TestContext.Current.CancellationToken);
        Assert.Equal([0, 127, 128, 255, 42], bytes.ToArray());
        result.Dispose();
        Assert.Equal(1, responseStream.DisposeCount);
        Assert.False(result.CanRead);
        Assert.Equal(0, proxy.DisposeCount);
        Assert.Equal(0, proxy.HeadRequests);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-S3-CANCELLATION", "sdk-upload-download-preserve-failure-and-token")]
    public async Task DataOperations_PropagateSdkFailureAndCancellationWithoutTakingOwnershipAsync(bool get, bool canceled)
    {
        var (repository, proxy) = CreateRepository();
        using var cancellation = new CancellationTokenSource();
        using var input = new MemoryStream([4, 5, 6]);
        Exception expected = canceled
            ? new OperationCanceledException("SDK canceled", cancellation.Token)
            : new InvalidOperationException("SDK operation failed");
        proxy.DataFailure = expected;

        Exception? actual = await Record.ExceptionAsync(async () =>
        {
            if (get)
                _ = await repository.GetAsync(new Uri($"s3://{Bucket}/key_A-9"), cancellation.Token);
            else
                _ = await repository.PutAsync(input, cancellationToken: cancellation.Token);
        });

        Assert.Same(expected, actual);
        if (canceled)
            Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(actual).CancellationToken);
        Assert.Equal(cancellation.Token, get ? proxy.GetToken : Assert.Single(proxy.Uploads).Token);
        Assert.True(input.CanRead);
        Assert.Equal(0, input.Position);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CANCELLATION", "canceled-cold-readiness-allows-healthy-retry")]
    public async Task Put_CanceledColdReadinessLeavesStreamAndGateUsableForRetryAsync()
    {
        var (repository, proxy) = CreateRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var input = new MemoryStream([7, 8, 9]);

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.PutAsync(input, cancellationToken: cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, proxy.HeadRequests);
        Assert.Empty(proxy.Uploads);
        Assert.Equal(0, input.Position);
        Assert.True(input.CanRead);

        _ = await repository.PutAsync(input, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal([7, 8, 9], Assert.Single(proxy.Uploads).Payload);
        Assert.Equal(1, proxy.HeadRequests);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "selected-repository-gates-public-bus-start-and-preserves-failure")]
    public async Task SelectedRepository_GatesPublicBusStartupOnReadinessAsync(bool fail)
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, RuntimeS3Proxy>();
        var proxy = (RuntimeS3Proxy)(object)client;
        var entered = new TaskCompletionSource(CreationOptions);
        var release = new TaskCompletionSource(CreationOptions);
        var expected = new InvalidOperationException("readiness failure");
        proxy.HeadHandler = async token =>
        {
            Assert.Equal(CancellationToken.None, token);
            entered.SetResult();
            await release.Task;
            if (fail)
                throw expected;
            return new HeadBucketResponse();
        };
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configure =>
            configure.UseMessageData(selector => selector.UseAmazonS3(client, new AmazonS3MessageDataRepositoryOptions(Bucket))));
        Task? start = null;
        try
        {
            start = bus.StartAsync(TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(start.IsCompleted);
            Assert.Equal(0, proxy.LifecycleReads);
            release.SetResult();
            if (fail)
                Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => start));
            else
            {
                await start;
                Assert.Equal(1, proxy.LifecycleReads);
            }
        }
        finally
        {
            release.TrySetResult();
            if (start is not null)
                _ = await Record.ExceptionAsync(() => start);
            await bus.StopAsync(CancellationToken.None);
        }
        Assert.Equal(1, proxy.HeadRequests);
        Assert.Equal(fail ? 0 : 1, proxy.LifecycleReads);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-LIFECYCLE", "public-prestart-removes-duplicate-owned-rules-and-is-idempotent")]
    public async Task PreStart_CanonicalizesDuplicateOwnedRulesAndPreservesForeignOrderAsync()
    {
        var (repository, proxy) = CreateRepository(14);
        var before = new LifecycleRule { Id = "foreign-before" };
        var after = new LifecycleRule { Id = "foreign-after" };
        proxy.Rules = [before, OwnedRule(3), after, OwnedRule(7)];
        IBusControl bus = Bus.Factory.CreateUsingInMemory(_ => { });

        await repository.PreStartAsync(bus);
        Assert.Equal(1, proxy.LifecycleWrites);
        Assert.Equal(3, proxy.Rules.Count);
        Assert.Same(before, proxy.Rules[0]);
        Assert.Same(after, proxy.Rules[2]);
        LifecycleRule owned = proxy.Rules[1];
        Assert.Equal(RuleId, owned.Id);
        Assert.Equal(14, owned.Expiration.Days);
        Assert.Equal(LifecycleRuleStatus.Enabled, owned.Status);
        Tag tag = Assert.IsType<LifecycleTagPredicate>(owned.Filter.LifecycleFilterPredicate).Tag;
        Assert.Equal(RuleId, tag.Key);
        Assert.Equal("enabled", tag.Value);

        await repository.PreStartAsync(bus);
        Assert.Equal(2, proxy.HeadRequests);
        Assert.Equal(2, proxy.LifecycleReads);
        Assert.Equal(1, proxy.LifecycleWrites);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-OBSERVER", "poststart-does-not-await-or-own-readiness-task")]
    public async Task PostStart_CompletesWhileBusReadinessIsPendingOrFaultedAsync()
    {
        var (repository, proxy) = CreateRepository();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(_ => { });
        var pending = new TaskCompletionSource<BusReady>(CreationOptions);
        var expected = new InvalidOperationException("readiness is independently owned");
        Task<BusReady> faulted = Task.FromException<BusReady>(expected);

        Assert.True(repository.PostStartAsync(bus, pending.Task).IsCompletedSuccessfully);
        Assert.False(pending.Task.IsCompleted);
        Assert.True(repository.PostStartAsync(bus, faulted).IsCompletedSuccessfully);
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => faulted));
        Assert.Equal(0, proxy.HeadRequests);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CANCELLATION", "cancel-readiness-waiter-preserves-active-initialization")]
    public async Task Put_CancelsReadinessWaiterWithoutCancelingInitializerAsync()
    {
        var (repository, proxy) = CreateRepository();
        var entered = new TaskCompletionSource(CreationOptions);
        var release = new TaskCompletionSource(CreationOptions);
        proxy.HeadHandler = async _ =>
        {
            entered.SetResult();
            await release.Task;
            return new HeadBucketResponse();
        };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var input = new MemoryStream([11, 12, 13]);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(_ => { });
        Task initializer = repository.PreStartAsync(bus);
        Task<Uri>? waiting = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            waiting = repository.PutAsync(input, cancellationToken: cancellation.Token);
            Assert.False(waiting.IsCompleted);
            cancellation.Cancel();
            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.False(initializer.IsCompleted);
            Assert.Empty(proxy.Uploads);
            Assert.Equal(0, input.Position);
        }
        finally
        {
            release.TrySetResult();
            await initializer;
            if (waiting is not null)
                _ = await Record.ExceptionAsync(() => waiting.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        }
        _ = await repository.PutAsync(input, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal([11, 12, 13], Assert.Single(proxy.Uploads).Payload);
        Assert.Equal(1, proxy.HeadRequests);
        Assert.True(input.CanRead);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-S3-CANCELLATION", "in-flight-sdk-cancellation-preserves-caller-token-and-ownership")]
    public async Task DataOperations_CancelWhileSdkOperationIsPendingAsync(bool get)
    {
        var (repository, proxy) = CreateRepository();
        var entered = new TaskCompletionSource(CreationOptions);
        var release = new TaskCompletionSource(CreationOptions);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var input = new MemoryStream([14, 15, 16]);
        proxy.UploadHandler = async (_, token) =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(token);
            return new PutObjectResponse();
        };
        proxy.DownloadHandler = async (_, token) =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(token);
            return new GetObjectResponse();
        };
        Task operation = get
            ? repository.GetAsync(new Uri($"s3://{Bucket}/key_A-9"), cancellation.Token)
            : repository.PutAsync(input, cancellationToken: cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            cancellation.Cancel();
            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(cancellation.Token, get ? proxy.GetToken : Assert.Single(proxy.Uploads).Token);
            Assert.True(input.CanRead);
            Assert.Equal(0, proxy.DisposeCount);
        }
        finally
        {
            release.TrySetResult();
            _ = await Record.ExceptionAsync(() => operation);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-STARTUP", "public-prestart-failure-allows-retry-and-restart")]
    public async Task PreStart_ReleasesFailedReadinessAndRevalidatesEveryRestartAsync()
    {
        var (repository, proxy) = CreateRepository();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(_ => { });
        var expected = new InvalidOperationException("first readiness failed");
        proxy.HeadHandler = token =>
        {
            Assert.Equal(CancellationToken.None, token);
            return Task.FromException<HeadBucketResponse>(expected);
        };
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PreStartAsync(bus)));
        Assert.Equal(0, proxy.LifecycleReads);
        proxy.HeadHandler = token =>
        {
            Assert.Equal(CancellationToken.None, token);
            return Task.FromResult(new HeadBucketResponse());
        };
        await repository.PreStartAsync(bus);
        await repository.PreStartAsync(bus);
        Assert.Equal(3, proxy.HeadRequests);
        Assert.Equal(2, proxy.LifecycleReads);
        Assert.Equal(0, proxy.LifecycleWrites);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CONFIGURATION", "package-public-factory-defaults-and-observer-contract")]
    public async Task ClientFactory_UsesDefaultOptionsAndPreservesNoOpObserverContractAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, RuntimeS3Proxy>();
        var proxy = (RuntimeS3Proxy)(object)client;
        var options = new AmazonS3MessageDataRepositoryOptions(Bucket);
        var maximum = new AmazonS3MessageDataRepositoryOptions(Bucket, int.MaxValue);
        Assert.Equal(Bucket, options.BucketName);
        Assert.Null(options.LifecycleExpirationDays);
        Assert.Equal(int.MaxValue, maximum.LifecycleExpirationDays);
        AmazonS3MessageDataRepository repository = client.CreateMessageDataRepository(options);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(_ => { });
        IBusObserver observer = repository;
        var failure = new InvalidOperationException("observer fault notification");
        observer.PostCreate(bus);
        observer.CreateFaulted(failure);
        Assert.True(observer.StartFaultedAsync(bus, failure).IsCompletedSuccessfully);
        Assert.True(observer.PreStopAsync(bus).IsCompletedSuccessfully);
        Assert.True(observer.PostStopAsync(bus).IsCompletedSuccessfully);
        Assert.True(observer.StopFaultedAsync(bus, failure).IsCompletedSuccessfully);
        Assert.Equal(0, proxy.HeadRequests);
        await repository.PreStartAsync(bus);
        Assert.Equal(1, proxy.HeadRequests);
        Assert.Equal(1, proxy.LifecycleReads);
        Assert.Equal(0, proxy.LifecycleWrites);
        Assert.Equal(0, proxy.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CANCELLATION", "cancel-active-readiness-releases-gate-for-retry")]
    public async Task Put_CancelsActiveReadinessAndAllowsHealthyRetryAsync()
    {
        var (repository, proxy) = CreateRepository();
        var entered = new TaskCompletionSource(CreationOptions);
        var release = new TaskCompletionSource(CreationOptions);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var input = new MemoryStream([17, 18, 19]);
        proxy.HeadHandler = async token =>
        {
            Assert.Equal(cancellation.Token, token);
            entered.SetResult();
            await release.Task.WaitAsync(token);
            return new HeadBucketResponse();
        };
        Task<Uri> operation = repository.PutAsync(input, cancellationToken: cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            cancellation.Cancel();
            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Empty(proxy.Uploads);
            Assert.Equal(0, proxy.LifecycleReads);
            Assert.Equal(0, input.Position);
            Assert.True(input.CanRead);
        }
        finally
        {
            release.TrySetResult();
            _ = await Record.ExceptionAsync(() => operation);
        }
        proxy.HeadHandler = _ => Task.FromResult(new HeadBucketResponse());
        _ = await repository.PutAsync(input, cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal([17, 18, 19], Assert.Single(proxy.Uploads).Payload);
        Assert.Equal(2, proxy.HeadRequests);
        Assert.Equal(1, proxy.LifecycleReads);
        Assert.Equal(0, proxy.DisposeCount);
    }

    private const TaskCreationOptions CreationOptions = TaskCreationOptions.RunContinuationsAsynchronously;

    private static (AmazonS3MessageDataRepository Repository, RuntimeS3Proxy Proxy) CreateRepository(int? days = null)
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, RuntimeS3Proxy>();
        return (new AmazonS3MessageDataRepository(client, new AmazonS3MessageDataRepositoryOptions(Bucket, days)),
            (RuntimeS3Proxy)(object)client);
    }

    private static LifecycleRule OwnedRule(int days) => new()
    {
        Id = RuleId,
        Status = LifecycleRuleStatus.Enabled,
        Filter = new LifecycleFilter { LifecycleFilterPredicate = new LifecycleTagPredicate
            { Tag = new Tag { Key = RuleId, Value = "enabled" } } },
        Expiration = new LifecycleRuleExpiration { Days = days },
    };

    private sealed record Upload(Stream InputStream, long InitialPosition, bool AutoCloseStream,
        bool AutoResetStreamPosition, string BucketName, string Key, Tag[] Tags, CancellationToken Token, byte[] Payload);

    private sealed class TrackingStream(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        public int DisposeCount { get; private set; }
        protected override void Dispose(bool disposing)
        {
            if (disposing && CanRead)
                DisposeCount++;
            base.Dispose(disposing);
        }
    }

    private class RuntimeS3Proxy : DispatchProxy
    {
        public List<Upload> Uploads { get; } = [];
        public List<LifecycleRule> Rules { get; set; } = [];
        public Stream? DownloadStream { get; set; }
        public Exception? DataFailure { get; set; }
        public VersionStatus? VersionStatus { get; set; }
        public Func<CancellationToken, Task<HeadBucketResponse>>? HeadHandler { get; set; }
        public Func<PutObjectRequest, CancellationToken, Task<PutObjectResponse>>? UploadHandler { get; set; }
        public Func<GetObjectRequest, CancellationToken, Task<GetObjectResponse>>? DownloadHandler { get; set; }
        public GetObjectRequest? GetRequest { get; private set; }
        public CancellationToken GetToken { get; private set; }
        public int DisposeCount { get; private set; }
        public int HeadRequests { get; private set; }
        public int VersioningRequests { get; private set; }
        public int LifecycleReads { get; private set; }
        public int LifecycleWrites { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method?.Name)
            {
                case "get_Config":
                    return new AmazonS3Config { AuthenticationRegion = "eu-central-1" };
                case nameof(IDisposable.Dispose):
                    DisposeCount++;
                    return null;
                case nameof(IAmazonS3.HeadBucketAsync):
                    Assert.Equal(Bucket, Assert.IsType<HeadBucketRequest>(args![0]).BucketName);
                    HeadRequests++;
                    var headToken = Assert.IsType<CancellationToken>(args[1]);
                    return HeadHandler?.Invoke(headToken) ?? Task.FromResult(new HeadBucketResponse());
                case nameof(IAmazonS3.GetBucketVersioningAsync):
                    VersioningRequests++;
                    return Task.FromResult(new GetBucketVersioningResponse
                        { VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus } });
                case nameof(IAmazonS3.GetLifecycleConfigurationAsync):
                    LifecycleReads++;
                    return Task.FromResult(new GetLifecycleConfigurationResponse
                        { Configuration = new LifecycleConfiguration { Rules = Rules } });
                case nameof(IAmazonS3.PutLifecycleConfigurationAsync):
                    LifecycleWrites++;
                    Rules = Assert.IsType<PutLifecycleConfigurationRequest>(args![0]).Configuration.Rules;
                    return Task.FromResult(new PutLifecycleConfigurationResponse());
                case nameof(IAmazonS3.PutObjectAsync):
                    return PutObjectAsync(Assert.IsType<PutObjectRequest>(args![0]), Assert.IsType<CancellationToken>(args[1]));
                case nameof(IAmazonS3.GetObjectAsync):
                    GetRequest = args![0] switch
                    {
                        GetObjectRequest request => request,
                        string bucket => new GetObjectRequest { BucketName = bucket, Key = Assert.IsType<string>(args[1]) },
                        _ => throw new InvalidOperationException("Unexpected object-read overload."),
                    };
                    GetToken = Assert.IsType<CancellationToken>(args[^1]);
                    if (DownloadHandler is not null)
                        return DownloadHandler(GetRequest, GetToken);
                    return DataFailure is not null
                        ? Task.FromException<GetObjectResponse>(DataFailure)
                        : Task.FromResult(new GetObjectResponse { ResponseStream = DownloadStream });
                default:
                    throw new NotSupportedException(method?.Name);
            }
        }

        private async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken token)
        {
            long position = request.InputStream.Position;
            using var bytes = new MemoryStream();
            if (DataFailure is null)
                await request.InputStream.CopyToAsync(bytes, token);
            Uploads.Add(new Upload(request.InputStream, position, request.AutoCloseStream,
                request.AutoResetStreamPosition, request.BucketName, request.Key,
                (request.TagSet ?? []).ToArray(), token, bytes.ToArray()));
            if (DataFailure is not null)
                throw DataFailure;
            if (UploadHandler is not null)
                return await UploadHandler(request, token);
            return new PutObjectResponse();
        }
    }
}
