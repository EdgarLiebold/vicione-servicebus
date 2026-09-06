using Azure;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.EventHubs.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubCheckpointStorageTests
{
    [Theory]
    [InlineData(StorageAuthentication.Anonymous)]
    [InlineData(StorageAuthentication.TokenCredential)]
    [InlineData(StorageAuthentication.SharedKey)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CHECKPOINT-STORAGE", "container-uri-is-complete-for-every-uri-authentication-mode")]
    public void ContainerUri_IsTheCompleteContainerAddressForEveryAuthenticationMode(
        StorageAuthentication authentication)
    {
        var containerUri = new Uri("https://account.blob.core.windows.net/checkpoints");
        var settings = new StorageSettings { ContainerUri = containerUri };

        switch (authentication)
        {
            case StorageAuthentication.TokenCredential:
                settings.TokenCredential = new StubTokenCredential();
                break;
            case StorageAuthentication.SharedKey:
                settings.SharedKeyCredential = new StorageSharedKeyCredential(
                    "account",
                    Convert.ToBase64String(new byte[32]));
                break;
        }

        BlobContainerClient client = EventHubCheckpointContainerClientFactory.Create(
            settings,
            endpointContainerName: "must-not-be-appended");

        Assert.Equal(containerUri, client.Uri);
        Assert.Equal("checkpoints", client.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CHECKPOINT-STORAGE", "existing-container-skips-create-and-forwards-cancellation")]
    public async Task ExistingContainer_SkipsCreationAndForwardsCancellationAsync()
    {
        var client = new StubBlobContainerClient(exists: true);
        var filter = new EventHubBlobContainerFactoryFilter(client);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await filter.EnsureContainerExistsAsync(cancellationToken);

        Assert.Equal(cancellationToken, client.ExistsCancellationToken);
        Assert.Equal(0, client.CreateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CHECKPOINT-STORAGE", "missing-container-is-created-and-forwards-cancellation")]
    public async Task MissingContainer_IsCreatedAndForwardsCancellationAsync()
    {
        var client = new StubBlobContainerClient(exists: false);
        var filter = new EventHubBlobContainerFactoryFilter(client);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await filter.EnsureContainerExistsAsync(cancellationToken);

        Assert.Equal(cancellationToken, client.ExistsCancellationToken);
        Assert.Equal(cancellationToken, client.CreateCancellationToken);
        Assert.Equal(1, client.CreateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CHECKPOINT-STORAGE", "create-failure-propagates-and-remains-retryable")]
    public async Task ContainerCreationFailure_PropagatesToPreventCachingFalseSuccessAsync()
    {
        var expected = new RequestFailedException(403, "forbidden");
        var client = new StubBlobContainerClient(exists: false) { CreateException = expected };
        var filter = new EventHubBlobContainerFactoryFilter(client);

        RequestFailedException actual = await Assert.ThrowsAsync<RequestFailedException>(() =>
            filter.EnsureContainerExistsAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, client.CreateCalls);
    }

    public enum StorageAuthentication
    {
        Anonymous,
        TokenCredential,
        SharedKey
    }

    private sealed class StubBlobContainerClient(bool exists) : BlobContainerClient
    {
        public CancellationToken CreateCancellationToken { get; private set; }
        public int CreateCalls { get; private set; }
        public Exception? CreateException { get; set; }
        public CancellationToken ExistsCancellationToken { get; private set; }

        public override Task<global::Azure.Response<BlobContainerInfo>> CreateIfNotExistsAsync(
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            return CreateAsync(cancellationToken);
        }

        public override Task<global::Azure.Response<BlobContainerInfo>> CreateIfNotExistsAsync(
            PublicAccessType publicAccessType,
            IDictionary<string, string>? metadata,
            BlobContainerEncryptionScopeOptions? encryptionScopeOptions,
            CancellationToken cancellationToken = default)
        {
            return CreateAsync(cancellationToken);
        }

        private Task<global::Azure.Response<BlobContainerInfo>> CreateAsync(CancellationToken cancellationToken)
        {
            CreateCalls++;
            CreateCancellationToken = cancellationToken;
            return CreateException is null
                ? Task.FromResult(global::Azure.Response.FromValue(
                    BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow),
                    new StubResponse()))
                : Task.FromException<global::Azure.Response<BlobContainerInfo>>(CreateException);
        }

        public override Task<global::Azure.Response<bool>> ExistsAsync(CancellationToken cancellationToken = default)
        {
            ExistsCancellationToken = cancellationToken;
            return Task.FromResult(global::Azure.Response.FromValue(exists, new StubResponse()));
        }
    }

    private sealed class StubResponse : global::Azure.Response
    {
        public override int Status => 200;
        public override string ReasonPhrase => "OK";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = "event-hub-checkpoint-storage-test";

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

        protected override bool TryGetHeader(string name, out string value)
        {
            value = null!;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = null!;
            return false;
        }
    }

    private sealed class StubTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken("token", DateTimeOffset.MaxValue);
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(GetToken(requestContext, cancellationToken));
        }
    }
}
