using System.Reflection;
using global::Amazon.Runtime;
using global::Amazon.S3;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.Tests.MessageData;

public sealed class AmazonS3MessageDataConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CONFIGURATION", "validated-client-owned-repository-boundary")]
    public void PublicApi_UsesCallerOwnedClientsAndRejectsInvalidArguments()
    {
        var options = new AmazonS3MessageDataRepositoryOptions("valid-message-data", 7);
        using AmazonS3Client client = CreateNonNetworkClient();
        var selector = new StubSelector();

        Assert.IsType<AmazonS3MessageDataRepository>(selector.UseAmazonS3(client, options));
        Assert.IsType<AmazonS3MessageDataRepository>(client.CreateMessageDataRepository(options));
        Assert.Throws<ArgumentNullException>(
            () => global::ViciOne.ServiceBus.AmazonS3.MessageDataRepositorySelectorExtensions.UseAmazonS3(
                null!,
                client,
                options));
        Assert.Throws<ArgumentNullException>(() => selector.UseAmazonS3(null!, options));
        Assert.Throws<ArgumentNullException>(() => selector.UseAmazonS3(client, null!));
        Assert.Throws<ArgumentNullException>(
            () => AmazonS3ClientExtensions.CreateMessageDataRepository(null!, options));
        Assert.Throws<ArgumentNullException>(() => client.CreateMessageDataRepository(null!));

        ConstructorInfo constructor = Assert.Single(typeof(AmazonS3MessageDataRepository).GetConstructors());
        Assert.Equal(
            [typeof(IAmazonS3), typeof(AmazonS3MessageDataRepositoryOptions)],
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.True(typeof(AmazonS3MessageDataRepository).IsSealed);
        Assert.True(typeof(AmazonS3MessageDataRepositoryOptions).IsSealed);

        MethodInfo selectorMethod = Assert.Single(
            typeof(global::ViciOne.ServiceBus.AmazonS3.MessageDataRepositorySelectorExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static));
        Assert.Equal("UseAmazonS3", selectorMethod.Name);
        Assert.Equal(
            [typeof(IMessageDataRepositorySelector), typeof(IAmazonS3), typeof(AmazonS3MessageDataRepositoryOptions)],
            selectorMethod.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.DoesNotContain(
            typeof(AmazonS3MessageDataRepository).Assembly.GetExportedTypes(),
            type => type.Name == "AmazonS3MessageDataRepositorySelectorExtensions");
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
        Assert.Throws<ArgumentNullException>(() => new AmazonS3MessageDataRepositoryOptions(null!));
        Assert.Throws<ArgumentException>(() => new AmazonS3MessageDataRepositoryOptions("   "));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmazonS3MessageDataRepositoryOptions("valid-message-data", 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmazonS3MessageDataRepositoryOptions("valid-message-data", -1));

        var options = new AmazonS3MessageDataRepositoryOptions("valid-message-data", 7);
        Assert.Equal("valid-message-data", options.BucketName);
        Assert.Equal(7, options.LifecycleExpirationDays);
        options.ValidateTimeToLive(null);
        options.ValidateTimeToLive(TimeSpan.FromDays(7));
        Assert.False(typeof(AmazonS3MessageDataRepositoryOptions)
            .GetProperty(nameof(AmazonS3MessageDataRepositoryOptions.BucketName))!
            .CanWrite);
        Assert.All(
            ["abc", "valid.bucket-name", new string('a', 63)],
            bucketName => Assert.Equal(
                bucketName,
                new AmazonS3MessageDataRepositoryOptions(bucketName).BucketName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-DATA-BOUNDARY", "stream-retention-and-s3-address-validation")]
    public async Task Repository_RejectsUnreadableStreamsAndUnsafeAddressesBeforeClientUseAsync()
    {
        var options = new AmazonS3MessageDataRepositoryOptions("valid-message-data", 7);
        using AmazonS3Client client = CreateNonNetworkClient();
        var repository = new AmazonS3MessageDataRepository(client, options);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var unreadable = new MemoryStream();
        unreadable.Dispose();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.PutAsync(null!, cancellationToken: cancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => repository.PutAsync(unreadable, cancellationToken: cancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.PutAsync(new MemoryStream([1]), TimeSpan.Zero, cancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.PutAsync(new MemoryStream([1]), TimeSpan.FromDays(-1), cancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repository.PutAsync(new MemoryStream([1]), TimeSpan.FromHours(1), cancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repository.PutAsync(new MemoryStream([1]), TimeSpan.FromDays(8), cancellationToken));
        Uri[] invalidAddresses =
        [
            new("urn:file:legacy-key", UriKind.Absolute),
            new("urn:file:../foreign", UriKind.Absolute),
            new("s3://other-message-data/object", UriKind.Absolute),
            new("s3://valid-message-data/path/to/object", UriKind.Absolute),
            new("s3://valid-message-data/path%2Fto%2Fobject", UriKind.Absolute),
            new("s3://valid-message-data/", UriKind.Absolute),
            new("s3://user@valid-message-data/object", UriKind.Absolute),
            new("s3://valid-message-data/object?versionId=foreign", UriKind.Absolute),
            new("https://example.test/object", UriKind.Absolute),
        ];
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.GetAsync(null!, cancellationToken));
        foreach (Uri invalidAddress in invalidAddresses)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => repository.GetAsync(invalidAddress, cancellationToken));
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
