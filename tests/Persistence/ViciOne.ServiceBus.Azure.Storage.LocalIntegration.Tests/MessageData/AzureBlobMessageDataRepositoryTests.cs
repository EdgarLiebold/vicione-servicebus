using System.Reflection;
using Azure;
using Azure.Storage.Blobs.Models;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Azure.Storage.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Azure.Storage.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Storage.LocalIntegration.Tests.MessageData;

public sealed class AzureBlobMessageDataRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-LOCAL-STARTUP", "missing-container-created-before-ready")]
    public async Task PreStartAsync_CreatesTheMissingContainerBeforeReportingReadyAsync()
    {
        await using AzureBlobTestContainer fixture = AzureBlobTestContainer.Create("startup");
        var repository = new AzureBlobMessageDataRepository(fixture.Container);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.False((await fixture.Container.ExistsAsync(cancellationToken)).Value);

        await repository.PreStartAsync(CreateBus()).WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.True((await fixture.Container.ExistsAsync(cancellationToken)).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-LOCAL-PERSISTENCE", "plain-and-compressed-round-trip")]
    public async Task PutAndGetAsync_RoundTripExactBytesThroughAzuriteAsync(bool compress)
    {
        await using AzureBlobTestContainer fixture = AzureBlobTestContainer.Create(
            compress ? "compressed" : "plain");
        var repository = new AzureBlobMessageDataRepository(fixture.Container, compress);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] expected = [0, 1, 2, 127, 128, 254, 255, 42, 17];
        await repository.PreStartAsync(CreateBus()).WaitAsync(fixture.OperationTimeout, cancellationToken);

        Uri address = await repository
            .PutAsync(new MemoryStream(expected, writable: false), cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await using Stream stored = await repository
            .GetAsync(address, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        using var actual = new MemoryStream();
        await stored.CopyToAsync(actual, cancellationToken);

        Assert.Equal(expected, actual.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-LOCAL-PROPERTIES", "ttl-and-content-encoding-committed-atomically")]
    public async Task PutAsync_CommitsExpirationAndContentEncodingWithTheBlobAsync()
    {
        await using AzureBlobTestContainer fixture = AzureBlobTestContainer.Create("properties");
        var repository = new AzureBlobMessageDataRepository(
            fixture.Container,
            new FixedBlobNameGenerator("properties"),
            compress: true,
            timeProvider: new FixedTimeProvider(Now));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await repository.PreStartAsync(CreateBus()).WaitAsync(fixture.OperationTimeout, cancellationToken);

        Uri address = await repository
            .PutAsync(
                new MemoryStream([1, 2, 3], writable: false),
                TimeSpan.FromMinutes(5),
                cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        BlobProperties properties = (await fixture.Container
                .GetBlobClient("properties")
                .GetPropertiesAsync(cancellationToken: cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken))
            .Value;

        Assert.Equal(fixture.Container.GetBlobClient("properties").Uri, address);
        Assert.Equal("gzip", properties.ContentEncoding);
        Assert.Equal(
            (Now + TimeSpan.FromMinutes(5)).UtcDateTime.ToString("O"),
            properties.Metadata["ValidUntilUtc"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-LOCAL-COLLISION", "duplicate-generated-name-cannot-overwrite")]
    public async Task PutAsync_DoesNotOverwriteAnExistingBlobAsync()
    {
        await using AzureBlobTestContainer fixture = AzureBlobTestContainer.Create("collision");
        var repository = new AzureBlobMessageDataRepository(
            fixture.Container,
            new FixedBlobNameGenerator("fixed"),
            compress: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] original = [1, 3, 5, 7];
        await repository.PreStartAsync(CreateBus()).WaitAsync(fixture.OperationTimeout, cancellationToken);
        Uri address = await repository
            .PutAsync(new MemoryStream(original), cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        RequestFailedException exception = await Assert.ThrowsAsync<RequestFailedException>(
            () => repository.PutAsync(new MemoryStream([2, 4, 6, 8]), cancellationToken: cancellationToken));
        await using Stream stored = await repository
            .GetAsync(address, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        using var actual = new MemoryStream();
        await stored.CopyToAsync(actual, cancellationToken);

        Assert.Contains(exception.Status, new[] { 409, 412 });
        Assert.Equal(original, actual.ToArray());
    }

    private static IBus CreateBus() => DispatchProxy.Create<IBus, NoOpDispatchProxy>();

    private sealed class FixedBlobNameGenerator(string name) : IBlobNameGenerator
    {
        public string GenerateBlobName() => name;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The repository must not invoke the observed bus.");
    }
}
