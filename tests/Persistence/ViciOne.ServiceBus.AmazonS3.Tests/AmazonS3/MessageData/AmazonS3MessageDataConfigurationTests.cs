using System.Reflection;
using global::Amazon.Runtime;
using global::Amazon.S3;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.Tests.AmazonS3.MessageData;

public sealed class AmazonS3MessageDataConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CONFIGURATION", "validated-client-owned-repository-boundary")]
    public void SelectorAndRepository_RejectInvalidArgumentsBeforeClientUse()
    {
        var options = new AmazonS3MessageDataRepositoryOptions("valid-message-data", 7);
        using AmazonS3Client client = CreateNonNetworkClient();
        var selector = new StubSelector();

        Assert.IsType<AmazonS3MessageDataRepository>(selector.AmazonS3(client, options));
        Assert.IsType<AmazonS3MessageDataRepository>(client.CreateMessageDataRepository(options));
        Assert.Throws<ArgumentNullException>(
            () => AmazonS3MessageDataRepositorySelectorExtensions.AmazonS3(null!, client, options));
        Assert.Throws<ArgumentNullException>(() => selector.AmazonS3(null!, options));
        Assert.Throws<ArgumentNullException>(() => selector.AmazonS3(client, null!));

        ConstructorInfo constructor = Assert.Single(typeof(AmazonS3MessageDataRepository).GetConstructors());
        Assert.Equal(
            [typeof(IAmazonS3), typeof(AmazonS3MessageDataRepositoryOptions)],
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-OPTIONS", "bucket-and-retention-boundaries")]
    public void Options_EnforceBucketAndRetentionBoundaries()
    {
        string[] invalidBucketNames =
        [
            "ab",
            new('a', 64),
            "Invalid_Bucket",
            "-invalid",
            "invalid-",
            "invalid..bucket",
            "invalid.-bucket",
            "invalid-.bucket",
            "xn--reserved",
            "sthree-reserved",
            "amzn_s3_demo_reserved",
            "192.168.0.1",
            "reserved-s3alias",
            "reserved--ol-s3",
            "reserved.mrap",
            "reserved--x-s3",
            "reserved--table-s3",
        ];
        Assert.All(
            invalidBucketNames,
            bucketName => Assert.Throws<ArgumentException>(
                () => new AmazonS3MessageDataRepositoryOptions(bucketName)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmazonS3MessageDataRepositoryOptions("valid-message-data", 0));

        var options = new AmazonS3MessageDataRepositoryOptions("valid-message-data", 7);
        Assert.Equal("valid-message-data", options.BucketName);
        Assert.Equal(7, options.LifecycleExpirationDays);
        options.ValidateTimeToLive(null);
        options.ValidateTimeToLive(TimeSpan.FromDays(7));
        Assert.False(typeof(AmazonS3MessageDataRepositoryOptions)
            .GetProperty(nameof(AmazonS3MessageDataRepositoryOptions.BucketName))!
            .CanWrite);
        Assert.All(
            new[] { "abc", "valid.bucket-name", new string('a', 63) },
            bucketName => Assert.Equal(
                bucketName,
                new AmazonS3MessageDataRepositoryOptions(bucketName).BucketName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-DATA-BOUNDARY", "stream-retention-and-address-validation")]
    public async Task Repository_RejectsUnreadableStreamsAndUnsafeAddressesBeforeClientUse()
    {
        var options = new AmazonS3MessageDataRepositoryOptions("valid-message-data", 7);
        using AmazonS3Client client = CreateNonNetworkClient();
        var repository = new AmazonS3MessageDataRepository(client, options);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var unreadable = new MemoryStream();
        unreadable.Dispose();
        await Assert.ThrowsAsync<ArgumentException>(() => repository.Put(unreadable, cancellationToken: cancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repository.Put(new MemoryStream([1]), TimeSpan.FromHours(1), cancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repository.Put(new MemoryStream([1]), TimeSpan.FromDays(8), cancellationToken));
        Uri[] invalidAddresses =
        [
            new("urn:file:../foreign", UriKind.Absolute),
            new("urn:file:path/to/object", UriKind.Absolute),
            new("urn:file:path%2Fto%2Fobject", UriKind.Absolute),
            new("urn:other:object", UriKind.Absolute),
            new("https://example.test/object", UriKind.Absolute),
        ];
        foreach (Uri invalidAddress in invalidAddresses)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => repository.Get(invalidAddress, cancellationToken));
        }
    }

    private static AmazonS3Client CreateNonNetworkClient() =>
        new(
            new AnonymousAWSCredentials(),
            new AmazonS3Config
            {
                ServiceURL = "http://127.0.0.1:1",
                AuthenticationRegion = "eu-central-1",
                ForcePathStyle = true,
            });

    private sealed class StubSelector : IMessageDataRepositorySelector
    {
        public IBusFactoryConfigurator Configurator => null!;
    }
}
