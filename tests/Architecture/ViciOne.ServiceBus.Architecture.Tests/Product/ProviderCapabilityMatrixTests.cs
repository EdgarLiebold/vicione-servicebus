using System.Text.Json;
using System.Xml.Linq;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class ProviderCapabilityMatrixTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROVIDER-CAPABILITY-MATRIX", "all-shipping-providers-have-source-bound-durable-dispositions")]
    public void EveryShippingProvider_HasAnHonestSourceBoundDurableDisposition()
    {
        ProviderCapabilityMatrix matrix = ReadMatrix();
        Assert.Equal(1, matrix.SchemaVersion);
        Assert.Equal("IDurableSender<TBus>.SendAsync", matrix.DurableSenderContract.ApplicationApi);
        Assert.Equal("IDurableSendDispatcher<TBus>", matrix.DurableSenderContract.ProviderSpi);
        Assert.Equal("startup-failure-before-background-delivery", matrix.DurableSenderContract.UnsupportedBehavior);

        string[] expectedTransportProjects = Directory
            .GetFiles(Path.Combine(RepositoryLayout.Root, "src", "Transports"), "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(".Testing", StringComparison.Ordinal))
            .Append(Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "ViciOne.ServiceBus.csproj"))
            .Append(Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.SignalR", "ViciOne.ServiceBus.SignalR.csproj"))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedTransportProjects, matrix.TransportProviders.Select(static provider => provider.Project)
            .Order(StringComparer.Ordinal));

        string[] expectedPersistenceProjects = Directory
            .GetFiles(Path.Combine(RepositoryLayout.Root, "src", "Persistence"), "*.csproj", SearchOption.AllDirectories)
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedPersistenceProjects, matrix.PersistenceProviders.Select(static provider => provider.Project)
            .Order(StringComparer.Ordinal));

        Assert.All(matrix.TransportProviders, provider =>
        {
            Assert.Equal(ReadPackageId(provider.Project), provider.Package);
            Assert.True(File.Exists(Path.Combine(RepositoryLayout.Root, provider.DurableSendEvidence)), provider.Id);

            bool ownsDispatcher = Sources(provider.Project)
                .Any(static source => source.Contains("IDurableSendDispatcher<", StringComparison.Ordinal));
            Assert.Equal(ownsDispatcher, provider.DurableSendDispatch != "unsupported");
            if (ownsDispatcher)
            {
                Assert.NotEqual("not-applicable", provider.DurableSendAcceptanceBoundary);
                Assert.NotEqual("not-applicable", provider.DurableSendAcceptanceEnvironment);
                if (provider.Id == "in-memory")
                    Assert.Equal("in-process-deterministic-test", provider.DurableSendAcceptanceEnvironment);
                else
                    Assert.StartsWith("real-", provider.DurableSendAcceptanceEnvironment, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal("not-applicable", provider.DurableSendAcceptanceBoundary);
                Assert.Equal("not-applicable", provider.DurableSendAcceptanceEnvironment);
            }
        });

        Assert.All(matrix.PersistenceProviders, provider =>
        {
            Assert.Equal(ReadPackageId(provider.Project), provider.Package);
            bool ownsStore = Sources(provider.Project)
                .Any(static source => source.Contains("IOutboxStore<", StringComparison.Ordinal));
            Assert.Equal(ownsStore, provider.DurableSendStore == "supported");
            if (ownsStore)
                Assert.True(File.Exists(Path.Combine(RepositoryLayout.Root, provider.DurableSendEvidence!)), provider.Id);
        });
    }

    private static IEnumerable<string> Sources(string project)
    {
        string directory = Path.GetDirectoryName(Path.Combine(RepositoryLayout.Root, project))!;
        return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText);
    }

    private static string ReadPackageId(string project)
    {
        string path = Path.Combine(RepositoryLayout.Root, project);
        XDocument document = XDocument.Load(path);
        return document.Descendants("PackageId").Select(static element => element.Value).SingleOrDefault()
            ?? Path.GetFileNameWithoutExtension(path);
    }

    private static ProviderCapabilityMatrix ReadMatrix()
    {
        string json = File.ReadAllText(Path.Combine(RepositoryLayout.Root, "docs", "provider-capabilities.json"));
        return JsonSerializer.Deserialize<ProviderCapabilityMatrix>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Provider capability matrix is empty.");
    }

    private sealed record ProviderCapabilityMatrix(
        int SchemaVersion,
        DurableSenderContract DurableSenderContract,
        TransportProvider[] TransportProviders,
        PersistenceProvider[] PersistenceProviders);

    private sealed record DurableSenderContract(
        string ApplicationApi,
        string ProviderSpi,
        string UnsupportedBehavior);

    private sealed record TransportProvider(
        string Id,
        string Kind,
        string Package,
        string Project,
        string Registration,
        string Send,
        string Publish,
        string Request,
        string DurableSendDispatch,
        string DurableSendAcceptanceBoundary,
        string DurableSendAcceptanceEnvironment,
        string DurableSendEvidence);

    private sealed record PersistenceProvider(
        string Id,
        string Package,
        string Project,
        string DurableSendStore,
        string? DurableSendEvidence);
}
