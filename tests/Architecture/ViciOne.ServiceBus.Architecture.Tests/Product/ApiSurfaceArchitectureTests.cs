using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DurableSend;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class ApiSurfaceArchitectureTests
{
    private static readonly string[] ExpectedConfigurationFamilies =
    [
        "bus-persistence-identity",
        "consumer-concurrency",
        "durable-sender-composition",
        "durable-sender-policy",
        "endpoint-qos-ownership",
        "host-lifecycle",
        "message-contract-catalog",
        "message-data-owner",
        "payload-admission",
        "provider-address-and-topology",
        "transport-selection",
    ];

    private static readonly string[] ExpectedHeritageEntries =
    [
        "client-factory-internals",
        "consumer-contract",
        "consumer-definition",
        "endpoint-providers",
        "legacy-test-framework",
        "legacy-verification-paths",
        "manual-scheduler-factories",
        "masstransit-product-identity",
        "pipeline-configurators",
        "serialized-durable-envelope",
        "standard-messaging-vocabulary",
        "typed-bind-wrapper",
    ];

    private static readonly IReadOnlyDictionary<string, string> ExpectedHeritageDispositions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client-factory-internals"] = "RETAIN_PROVIDER_SPI",
            ["consumer-contract"] = "RETAIN_APPLICATION_CAPABILITY",
            ["consumer-definition"] = "RETAIN_ADVANCED_HIDDEN",
            ["endpoint-providers"] = "RETAIN_APPLICATION_CAPABILITY",
            ["legacy-test-framework"] = "REMOVE_COMPATIBILITY_ONLY",
            ["legacy-verification-paths"] = "REMOVE_COMPATIBILITY_ONLY",
            ["manual-scheduler-factories"] = "RETAIN_ADVANCED_HIDDEN",
            ["masstransit-product-identity"] = "REMOVE_COMPATIBILITY_ONLY",
            ["pipeline-configurators"] = "RETAIN_PROVIDER_SPI",
            ["serialized-durable-envelope"] = "RETAIN_ADVANCED_HIDDEN",
            ["standard-messaging-vocabulary"] = "RETAIN_DOMAIN_STANDARD",
            ["typed-bind-wrapper"] = "RETAIN_ADVANCED_HIDDEN",
        };

    [Fact]
    [RequirementCoverage("REQ-VSB-API-LAYERING", "five-layers-have-enforced-discovery-boundaries")]
    public void FiveApiLayers_HaveEnforcedDiscoveryAndPackageBoundaries()
    {
        string documentation = Source("docs/api-surface.md");
        Assert.Contains("## Application API", documentation, StringComparison.Ordinal);
        Assert.Contains("## Advanced SPI", documentation, StringComparison.Ordinal);
        Assert.Contains("## Provider API", documentation, StringComparison.Ordinal);
        Assert.Contains("## Operations API", documentation, StringComparison.Ordinal);
        Assert.Contains("## Testing API", documentation, StringComparison.Ordinal);

        Type[] applicationContracts =
        [
            typeof(IConsumer<>),
            typeof(ISendEndpointProvider),
            typeof(IPublishEndpoint),
            typeof(IRequestClient<>),
            typeof(IMessageScheduler),
            typeof(IDurableSender<>),
            typeof(IDurableSenderOperations<>),
        ];
        Assert.All(applicationContracts, AssertApplicationVisible);

        Type[] advancedContracts =
        [
            typeof(ConsumerDefinition<>),
            typeof(IConsumerDefinition),
            typeof(IConsumerDefinition<>),
            typeof(Bind<,>),
            typeof(Bind<,,>),
            typeof(Bind<>),
            typeof(MessageSchedulerBusExtensions),
            typeof(ValidateViciOneServiceBusHostOptions),
            typeof(SerializedDurableSend),
            typeof(IDurableSendStore<>),
            typeof(IDurableSendDispatcher<>),
        ];
        Assert.All(advancedContracts, AssertAdvancedHidden);

        string rabbitRegistration = Source(
            "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/Configuration/RabbitMqBusFactoryConfiguratorExtensions.cs");
        string rabbitOperations = Source(
            "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/Operations/IRabbitMqQueueOperations.cs");
        Assert.Contains("public static void UsingRabbitMq", rabbitRegistration, StringComparison.Ordinal);
        Assert.Contains("public interface IRabbitMqQueueOperations", rabbitOperations, StringComparison.Ordinal);

        string shipping = Source("ViciOne.ServiceBus.slnx");
        string engineering = Source("ViciOne.ServiceBus.Engineering.slnx");
        Assert.DoesNotContain("ViciOne.ServiceBus.Testing.csproj", shipping, StringComparison.Ordinal);
        Assert.Contains("ViciOne.ServiceBus.Testing.csproj", engineering, StringComparison.Ordinal);
        Assert.DoesNotContain("RabbitMqTransport.Testing.csproj", shipping, StringComparison.Ordinal);
        Assert.Contains("RabbitMqTransport.Testing.csproj", engineering, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-API-DISCOVERABILITY", "preferred-journeys-use-no-advanced-or-historical-path")]
    public void PreferredPackageJourneys_UseOnlyApplicationAndProviderEntryPoints()
    {
        string directory = Path.Combine(RepositoryLayout.Root, "samples", "DeveloperJourneys");
        string source = string.Join('\n', Directory.GetFiles(directory, "Journey*.cs")
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText));
        string[] forbidden =
        [
            "ConsumerDefinition<",
            "Bind<",
            "CreateMessageScheduler(",
            "CreateDelayedMessageScheduler(",
            "SerializedDurableSend",
            "IDurableSendStore<",
            "IDurableSendDispatcher<",
            "PartitionedConsumerConcurrencyGate<",
            "ConsumerConcurrencyGate<",
        ];

        Assert.All(forbidden, token => Assert.DoesNotContain(token, source, StringComparison.Ordinal));
        Assert.Contains("AddConsumer<SubmitOrderConsumer>(consumer =>", source, StringComparison.Ordinal);
        Assert.Contains("consumer.UseMessageRetry", source, StringComparison.Ordinal);
        Assert.Contains("consumer.UsePartitionedConcurrency", source, StringComparison.Ordinal);
        Assert.Contains("IDurableSender<IOrdersBus>", source, StringComparison.Ordinal);
        Assert.Contains("IMessageScheduler scheduler", source, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-CONFIGURATION", "complete-fail-fast-family-inventory")]
    public void StaticConfigurationInventory_IsCompleteAndBoundToActionableExecutingTests()
    {
        using JsonDocument manifest = JsonDocument.Parse(Source("docs/static-configuration-validation.json"));
        JsonElement root = manifest.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Contains("before runtime messaging begins", root.GetProperty("scope").GetString(), StringComparison.Ordinal);

        JsonElement[] families = root.GetProperty("families").EnumerateArray().ToArray();
        Assert.Equal(ExpectedConfigurationFamilies, families
            .Select(static family => family.GetProperty("id").GetString())
            .Order(StringComparer.Ordinal));

        Assert.All(families, family =>
        {
            string id = family.GetProperty("id").GetString()!;
            string boundary = family.GetProperty("boundary").GetString()!;
            Assert.Contains(boundary, new[] { "registration", "host-start" });

            string sourcePath = family.GetProperty("source").GetString()!;
            string testPath = family.GetProperty("test").GetString()!;
            string source = Source(sourcePath);
            string test = Source(testPath);
            Assert.Contains(family.GetProperty("testMethod").GetString()!, test, StringComparison.Ordinal);
            Assert.Contains(family.GetProperty("exception").GetString()!, source + test, StringComparison.Ordinal);
            Assert.Contains(
                family.GetProperty("actionableFragment").GetString()!,
                source + test,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TODO", family.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(id));
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HERITAGE-DISPOSITION", "all-retained-shapes-carry-current-capability")]
    public void HeritageDisposition_IsTerminalAndCompatibilityOnlyIdentityIsAbsent()
    {
        using JsonDocument manifest = JsonDocument.Parse(Source("docs/mass-transit-heritage-disposition.json"));
        JsonElement root = manifest.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        JsonElement[] entries = root.GetProperty("entries").EnumerateArray().ToArray();
        Assert.Equal(ExpectedHeritageEntries, entries
            .Select(static entry => entry.GetProperty("id").GetString())
            .Order(StringComparer.Ordinal));

        string[] allowedDispositions =
        [
            "REMOVE_COMPATIBILITY_ONLY",
            "RETAIN_ADVANCED_HIDDEN",
            "RETAIN_APPLICATION_CAPABILITY",
            "RETAIN_DOMAIN_STANDARD",
            "RETAIN_PROVIDER_SPI",
        ];
        Assert.All(entries, entry =>
        {
            string id = entry.GetProperty("id").GetString()!;
            string disposition = entry.GetProperty("disposition").GetString()!;
            Assert.Contains(disposition, allowedDispositions);
            Assert.Equal(ExpectedHeritageDispositions[id], disposition);
            Assert.True(entry.GetProperty("rationale").GetString()!.Length >= 80, entry.GetProperty("id").GetString());
            Assert.True(File.Exists(Path.Combine(RepositoryLayout.Root, entry.GetProperty("evidence").GetString()!))
                || Directory.Exists(Path.Combine(RepositoryLayout.Root, entry.GetProperty("evidence").GetString()!)));
            Assert.DoesNotContain("PENDING", entry.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UNREVIEWED", entry.ToString(), StringComparison.OrdinalIgnoreCase);
        });

        string[] productIdentityFiles = Directory
            .EnumerateFiles(Path.Combine(RepositoryLayout.Root, "src"), "*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(RepositoryLayout.Root, "samples"), "*", SearchOption.AllDirectories))
            .Where(static path => path.EndsWith(".cs", StringComparison.Ordinal)
                || path.EndsWith(".csproj", StringComparison.Ordinal)
                || path.EndsWith(".props", StringComparison.Ordinal)
                || path.EndsWith(".targets", StringComparison.Ordinal))
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.DoesNotContain(productIdentityFiles, path =>
            File.ReadAllText(path).Contains("MassTransit", StringComparison.OrdinalIgnoreCase));
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));

    private static void AssertApplicationVisible(Type type)
    {
        Assert.True(type.IsPublic);
        Assert.NotEqual(EditorBrowsableState.Never, type.GetCustomAttribute<EditorBrowsableAttribute>()?.State);
    }

    private static void AssertAdvancedHidden(Type type)
    {
        Assert.True(type.IsPublic);
        Assert.Equal(EditorBrowsableState.Never, type.GetCustomAttribute<EditorBrowsableAttribute>()?.State);
    }
}
