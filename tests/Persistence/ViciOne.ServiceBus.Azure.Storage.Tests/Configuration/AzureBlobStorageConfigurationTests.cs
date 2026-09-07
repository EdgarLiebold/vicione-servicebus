using System.Reflection;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Azure.Storage.MessageData;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Storage.Tests.Configuration;

public sealed class AzureBlobStorageConfigurationTests
{
    private const string ConnectionString =
        "DefaultEndpointsProtocol=https;AccountName=account;AccountKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=;EndpointSuffix=core.windows.net";

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-API", "greenfield-blob-api-shape")]
    public void PublicApi_UsesOneAzureBlobVocabularyWithoutLegacyAliases()
    {
        Type repositoryType = typeof(AzureBlobMessageDataRepository);
        Type[] exportedTypes = repositoryType.Assembly.GetExportedTypes();

        Assert.True(repositoryType.IsSealed);
        Assert.True(typeof(NewIdBlobNameGenerator).IsSealed);
        Assert.DoesNotContain(exportedTypes, type => type.Namespace?.Contains("AzureStorage", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(exportedTypes, type => type.Name == "AzureStorageMessageDataRepository");

        ConstructorInfo[] constructors =
        [
            .. repositoryType.GetConstructors()
                .OrderBy(constructor => constructor.GetParameters().Length),
        ];
        Assert.Equal(2, constructors.Length);
        Assert.Equal(
            [typeof(BlobContainerClient), typeof(bool), typeof(TimeProvider)],
            constructors[0].GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(
            [typeof(BlobContainerClient), typeof(IBlobNameGenerator), typeof(bool), typeof(TimeProvider)],
            constructors[1].GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(false, constructors[0].GetParameters()[1].DefaultValue);
        Assert.Null(constructors[0].GetParameters()[2].DefaultValue);
        Assert.Equal(false, constructors[1].GetParameters()[2].DefaultValue);
        Assert.Null(constructors[1].GetParameters()[3].DefaultValue);

        MethodInfo clientMethod = Assert.Single(
            typeof(BlobServiceClientExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static));
        ParameterInfo[] clientParameters = clientMethod.GetParameters();
        Assert.Equal("CreateMessageDataRepository", clientMethod.Name);
        Assert.Equal(
            [typeof(BlobServiceClient), typeof(string), typeof(bool), typeof(TimeProvider)],
            clientParameters.Select(parameter => parameter.ParameterType));
        Assert.Equal(
            ["client", "containerName", "compress", "timeProvider"],
            clientParameters.Select(parameter => parameter.Name));
        Assert.Equal(false, clientParameters[2].DefaultValue);
        Assert.Null(clientParameters[3].DefaultValue);

        MethodInfo selectorMethod = Assert.Single(
            typeof(MessageDataRepositorySelectorExtensions).GetMethods(
                BindingFlags.Public | BindingFlags.Static));
        Assert.Equal("UseAzureBlobStorage", selectorMethod.Name);
        ParameterInfo[] selectorParameters = selectorMethod.GetParameters();
        Assert.Equal(
            [
                typeof(IMessageDataRepositorySelector),
                typeof(string),
                typeof(string),
                typeof(bool),
            ],
            selectorParameters.Select(parameter => parameter.ParameterType));
        Assert.Equal(
            ["selector", "connectionString", "containerName", "compress"],
            selectorParameters.Select(parameter => parameter.Name));
        Assert.Equal("message-data", selectorParameters[2].DefaultValue);
        Assert.Equal(false, selectorParameters[3].DefaultValue);
        Assert.DoesNotContain(
            typeof(MessageDataRepositorySelectorExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == "AzureStorage");
        Assert.DoesNotContain(
            repositoryType.Assembly.GetReferencedAssemblies(),
            assembly => assembly.Name == "Azure.Identity");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-CONSTRUCTION", "caller-owned-client-and-argument-boundaries")]
    public void Construction_UsesCallerOwnedClientsAndRejectsInvalidArguments()
    {
        var serviceClient = new BlobServiceClient(new Uri("https://account.blob.core.windows.net"));
        BlobContainerClient containerClient = serviceClient.GetBlobContainerClient("message-data");
        var generator = new FixedBlobNameGenerator();

        Assert.IsType<AzureBlobMessageDataRepository>(
            new AzureBlobMessageDataRepository(containerClient));
        Assert.IsType<AzureBlobMessageDataRepository>(
            new AzureBlobMessageDataRepository(containerClient, generator, compress: true));
        Assert.Throws<ArgumentNullException>(() => new AzureBlobMessageDataRepository(null!));
        Assert.Throws<ArgumentException>(
            () => new AzureBlobMessageDataRepository(
                new BlobContainerClient(new Uri("https://account.blob.core.windows.net"))));
        Assert.Throws<ArgumentNullException>(
            () => new AzureBlobMessageDataRepository(containerClient, null!));

        Assert.IsType<AzureBlobMessageDataRepository>(
            serviceClient.CreateMessageDataRepository("message-data"));
        Assert.Throws<ArgumentNullException>(
            () => BlobServiceClientExtensions.CreateMessageDataRepository(null!, "message-data"));
        Assert.Throws<ArgumentException>(() => serviceClient.CreateMessageDataRepository(""));
        Assert.Throws<ArgumentException>(() => serviceClient.CreateMessageDataRepository("   "));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-CONSTRUCTION", "selector-default-and-validation")]
    public void Selector_UsesExplicitVerbDefaultContainerAndStrictValidation()
    {
        var selector = new StubSelector();

        IMessageDataRepository repository = selector.UseAzureBlobStorage(ConnectionString);
        Assert.IsType<AzureBlobMessageDataRepository>(repository);
        Assert.IsType<AzureBlobMessageDataRepository>(
            selector.UseAzureBlobStorage(ConnectionString, "custom-data", compress: true));

        Assert.Throws<ArgumentNullException>(
            () => MessageDataRepositorySelectorExtensions.UseAzureBlobStorage(null!, ConnectionString));
        Assert.Throws<ArgumentException>(() => selector.UseAzureBlobStorage(""));
        Assert.Throws<ArgumentException>(() => selector.UseAzureBlobStorage("   "));
        Assert.Throws<ArgumentException>(() => selector.UseAzureBlobStorage(ConnectionString, ""));
        Assert.Throws<ArgumentException>(() => selector.UseAzureBlobStorage(ConnectionString, "   "));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-BLOB-NAMES", "default-generator-produces-nonempty-distinct-names")]
    public void DefaultBlobNameGenerator_ProducesNonEmptyDistinctNames()
    {
        var generator = new NewIdBlobNameGenerator();

        string first = generator.GenerateBlobName();
        string second = generator.GenerateBlobName();

        Assert.False(string.IsNullOrWhiteSpace(first));
        Assert.False(string.IsNullOrWhiteSpace(second));
        Assert.NotEqual(first, second);
    }

    private sealed class FixedBlobNameGenerator : IBlobNameGenerator
    {
        public string GenerateBlobName() => "fixed-name";
    }

    private sealed class StubSelector : IMessageDataRepositorySelector
    {
        public IBusFactoryConfigurator Configurator => null!;
    }
}
