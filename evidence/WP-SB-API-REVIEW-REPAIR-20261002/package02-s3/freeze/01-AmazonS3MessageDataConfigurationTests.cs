using System.Reflection;
using global::Amazon.Runtime;
using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonS3.Tests.MessageData;

public sealed class AmazonS3MessageDataConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CONFIGURATION", "direct-constructor-rejects-null-dependencies")]
    public void RepositoryConstructor_RejectsNullClientAndOptions()
    {
        var options = new AmazonS3MessageDataRepositoryOptions("direct-constructor-data");
        using AmazonS3Client client = CreateNonNetworkClient();

        Assert.Equal(
            "client",
            Assert.Throws<ArgumentNullException>(() => new AmazonS3MessageDataRepository(null!, options)).ParamName);
        Assert.Equal(
            "options",
            Assert.Throws<ArgumentNullException>(() => new AmazonS3MessageDataRepository(client, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-CONFIGURATION", "factories-preserve-caller-client-and-settings")]
    public async Task Factories_UseTheSuppliedClientAndBucketLifecycleSettingsAsync()
    {
        IAmazonS3 directClient = DispatchProxy.Create<IAmazonS3, RecordingS3DispatchProxy>();
        var directProxy = (RecordingS3DispatchProxy)(object)directClient;
        var directOptions = new AmazonS3MessageDataRepositoryOptions("direct-factory-data");
        IAmazonS3 selectorClient = DispatchProxy.Create<IAmazonS3, RecordingS3DispatchProxy>();
        var selectorProxy = (RecordingS3DispatchProxy)(object)selectorClient;
        var selectorOptions = new AmazonS3MessageDataRepositoryOptions("selector-factory-data", 7);
        var selector = new StubSelector();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AmazonS3MessageDataRepository directRepository = directClient.CreateMessageDataRepository(directOptions);
        await directRepository.EnsureReadyAsync(cancellationToken);
        Assert.Equal(directOptions.BucketName, directProxy.HeadBucketRequest?.BucketName);
        Assert.Null(directProxy.PutLifecycleRequest);
        Assert.Null(selectorProxy.HeadBucketRequest);

        var selectedRepository = selector.UseAmazonS3(selectorClient, selectorOptions);
        await Assert.IsType<AmazonS3MessageDataRepository>(selectedRepository).EnsureReadyAsync(cancellationToken);
        Assert.Equal(selectorOptions.BucketName, selectorProxy.HeadBucketRequest?.BucketName);
        PutLifecycleConfigurationRequest lifecycleRequest = Assert.IsType<PutLifecycleConfigurationRequest>(
            selectorProxy.PutLifecycleRequest);
        LifecycleRule ownedRule = Assert.Single(lifecycleRequest.Configuration.Rules);
        Assert.Equal(selectorOptions.LifecycleExpirationDays!.Value, ownedRule.Expiration.Days);
        Assert.Equal(selectorOptions.BucketName, lifecycleRequest.BucketName);
        Assert.Equal(directOptions.BucketName, directProxy.HeadBucketRequest?.BucketName);
    }

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
            "amzn-s3-demo-reserved",
            "192.168.0.1",
            "reserved-s3alias",
            "reserved--ol-s3",
            "reserved.mrap",
            "reserved--x-s3",
            "reserved--table-s3",
        ];
        Assert.All(
            invalidBucketNames,
            bucketName => Assert.Equal("bucketName", Assert.Throws<ArgumentException>(
                () => new AmazonS3MessageDataRepositoryOptions(bucketName)).ParamName));
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
            ["abc", "valid.bucket-name", new string('a', 63), "amzn-s3-demo", "xamzn-s3-demo-review"],
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

    [Theory]
    [InlineData("s3://safe-message-data:9000/object")]
    [InlineData("s3://safe-message-data/object#fragment")]
    [InlineData("object")]
    [RequirementCoverage("REQ-VSB-AWS-S3-DATA-BOUNDARY", "port-fragment-and-relative-uri-rejected-before-client-use")]
    public async Task GetAsync_RejectsPortFragmentAndRelativeAddressBeforeClientUseAsync(string addressText)
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, RecordingS3DispatchProxy>();
        var proxy = (RecordingS3DispatchProxy)(object)client;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("safe-message-data"));
        var address = new Uri(addressText, UriKind.RelativeOrAbsolute);

        ArgumentException failure = await Assert.ThrowsAsync<ArgumentException>(
            () => repository.GetAsync(address, TestContext.Current.CancellationToken));

        Assert.Equal("address", failure.ParamName);
        Assert.Null(proxy.GetObjectRequest);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-DATA-BOUNDARY", "safe-hyphen-underscore-key-reaches-s3-client")]
    public async Task GetAsync_AcceptsSafeHyphenAndUnderscoreKeyAsync()
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, RecordingS3DispatchProxy>();
        var proxy = (RecordingS3DispatchProxy)(object)client;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("safe-message-data"));
        var expectedFailure = new InvalidOperationException("S3 client boundary reached");
        proxy.GetObjectFailure = expectedFailure;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.GetAsync(
                new Uri("s3://safe-message-data/key_A-9", UriKind.Absolute),
                TestContext.Current.CancellationToken));

        Assert.Same(expectedFailure, actual);
        Assert.Equal("safe-message-data", proxy.GetObjectRequest?.BucketName);
        Assert.Equal("key_A-9", proxy.GetObjectRequest?.Key);
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

    private class RecordingS3DispatchProxy : DispatchProxy
    {
        public HeadBucketRequest? HeadBucketRequest { get; private set; }

        public PutLifecycleConfigurationRequest? PutLifecycleRequest { get; private set; }

        public GetObjectRequest? GetObjectRequest { get; private set; }

        public Exception? GetObjectFailure { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_Config":
                    return new AmazonS3Config { AuthenticationRegion = "eu-central-1" };
                case nameof(IAmazonS3.HeadBucketAsync):
                    HeadBucketRequest = Assert.IsType<HeadBucketRequest>(args![0]);
                    return Task.FromResult(new HeadBucketResponse());
                case nameof(IAmazonS3.GetBucketVersioningAsync):
                    return Task.FromResult(new GetBucketVersioningResponse
                    {
                        VersioningConfig = new S3BucketVersioningConfig(),
                    });
                case nameof(IAmazonS3.GetLifecycleConfigurationAsync):
                    return Task.FromResult(
                        new GetLifecycleConfigurationResponse
                        {
                            Configuration = new LifecycleConfiguration { Rules = [] },
                        });
                case nameof(IAmazonS3.PutLifecycleConfigurationAsync):
                    PutLifecycleRequest = Assert.IsType<PutLifecycleConfigurationRequest>(args![0]);
                    return Task.FromResult(new PutLifecycleConfigurationResponse());
                case nameof(IAmazonS3.GetObjectAsync):
                    GetObjectRequest = args![0] switch
                    {
                        GetObjectRequest request => request,
                        string bucketName => new GetObjectRequest
                        {
                            BucketName = bucketName,
                            Key = Assert.IsType<string>(args[1]),
                        },
                        _ => throw new InvalidOperationException("Unexpected S3 object request overload."),
                    };
                    return Task.FromException<GetObjectResponse>(
                        GetObjectFailure ?? new InvalidOperationException("Unexpected S3 object read."));
                default:
                    throw new NotSupportedException(targetMethod?.Name);
            }
        }
    }
}
