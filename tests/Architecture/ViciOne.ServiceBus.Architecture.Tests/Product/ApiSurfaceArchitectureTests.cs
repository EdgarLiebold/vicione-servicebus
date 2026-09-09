using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.DependencyInjection;
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
        "former-product-identity",
        "legacy-test-framework",
        "legacy-verification-paths",
        "manual-scheduler-factories",
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
            ["former-product-identity"] = "REMOVE_COMPATIBILITY_ONLY",
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
        ];
        Assert.All(applicationContracts, type => Assert.Equal("ViciOne.ServiceBus", type.Namespace));

        Assert.Equal("ViciOne.ServiceBus.Operations", typeof(IReliableMessagingOperations<>).Namespace);

        Type[] advancedContracts =
        [
            typeof(ConsumerDefinition<>),
            typeof(IConsumerDefinition),
            typeof(IConsumerDefinition<>),
            typeof(Bind<,>),
            typeof(Bind<,,>),
            typeof(Bind<>),
            typeof(MessageSchedulerBusExtensions),
            typeof(SerializedDurableSend),
            typeof(IOutboxStore<>),
            typeof(IDurableSendDispatcher<>),
        ];
        Assert.All(advancedContracts, type =>
        {
            Assert.True(type.IsPublic);
            Assert.True(
                type.Namespace!.StartsWith("ViciOne.ServiceBus.Advanced", StringComparison.Ordinal)
                || type.Namespace.StartsWith("ViciOne.ServiceBus.Providers", StringComparison.Ordinal));
            Assert.NotEqual(EditorBrowsableState.Never, type.GetCustomAttribute<EditorBrowsableAttribute>()?.State);
        });
        Assert.Equal("ViciOne.ServiceBus.Configuration", typeof(ValidateViciOneServiceBusHostOptions).Namespace);

        string rabbitRegistration = Source(
            "src/Transports/ViciOne.ServiceBus.RabbitMq/Configuration/RabbitMqBusFactoryConfiguratorExtensions.cs");
        string rabbitOperations = Source(
            "src/Transports/ViciOne.ServiceBus.RabbitMq/Operations/IRabbitMqQueueOperations.cs");
        Assert.Contains("public static void UsingRabbitMq", rabbitRegistration, StringComparison.Ordinal);
        Assert.Contains("namespace ViciOne.ServiceBus.Configuration;", rabbitRegistration, StringComparison.Ordinal);
        Assert.Contains("public interface IRabbitMqQueueOperations", rabbitOperations, StringComparison.Ordinal);

        (string Path, string Method)[] transportSelectionEntryPoints =
        [
            ("src/ViciOne.ServiceBus/InMemoryTransport/InMemoryConfigurationExtensions.cs", "public static void UsingInMemory"),
            ("src/Transports/ViciOne.ServiceBus.ActiveMq/Configuration/ActiveMqBusFactoryConfiguratorExtensions.cs", "public static void UsingActiveMq"),
            ("src/Transports/ViciOne.ServiceBus.AmazonSqs/Configuration/AmazonSqsBusFactoryConfiguratorExtensions.cs", "public static void UsingAmazonSqs"),
            ("src/Transports/ViciOne.ServiceBus.AzureServiceBus/Configuration/AzureBusFactory.cs", "public static IBusControl CreateUsingServiceBus"),
            ("src/Transports/ViciOne.ServiceBus.AzureServiceBus/Configuration/ServiceBusConfigurationExtensions.cs", "public static void UsingAzureServiceBus"),
            ("src/Transports/ViciOne.ServiceBus.EventHubs/Configuration/EventHubIntegrationExtensions.cs", "public static void UsingEventHub"),
            ("src/Transports/ViciOne.ServiceBus.RabbitMq/Configuration/RabbitMqBusFactoryConfiguratorExtensions.cs", "public static void UsingRabbitMq"),
            ("src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/Configuration/PostgresBusFactoryConfiguratorExtensions.cs", "public static void UsingPostgres"),
            ("src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/Configuration/SqlServerBusFactoryConfiguratorExtensions.cs", "public static void UsingSqlServer"),
        ];
        Assert.All(transportSelectionEntryPoints, entry =>
        {
            string entryPoint = Source(entry.Path);
            Assert.Contains(entry.Method, entryPoint, StringComparison.Ordinal);
            Assert.Contains("namespace ViciOne.ServiceBus.Configuration;", entryPoint, StringComparison.Ordinal);
        });

        string[] transportRoots =
        [
            Path.Combine(RepositoryLayout.Root, "src", "Transports"),
            Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "InMemoryTransport"),
        ];
        Regex transportSelector = new(
            @"public\s+static[^\r\n{;]*\b(?:Using|CreateUsing)[A-Za-z0-9_]*\s*\(",
            RegexOptions.CultureInvariant);
        string[] discoveredTransportSelectors = transportRoots
            .SelectMany(static root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => transportSelector.IsMatch(File.ReadAllText(path)))
            .ToArray();
        Assert.Equal(transportSelectionEntryPoints.Length, discoveredTransportSelectors.Length);
        Assert.All(discoveredTransportSelectors, path => Assert.Contains(
            "namespace ViciOne.ServiceBus.Configuration;",
            File.ReadAllText(path),
            StringComparison.Ordinal));

        string shipping = Source("ViciOne.ServiceBus.slnx");
        string engineering = Source("ViciOne.ServiceBus.Engineering.slnx");
        Assert.DoesNotContain("ViciOne.ServiceBus.Testing.csproj", shipping, StringComparison.Ordinal);
        Assert.Contains("ViciOne.ServiceBus.Testing.csproj", engineering, StringComparison.Ordinal);
        Assert.DoesNotContain("ViciOne.ServiceBus.RabbitMq.Testing.csproj", shipping, StringComparison.Ordinal);
        Assert.Contains("ViciOne.ServiceBus.RabbitMq.Testing.csproj", engineering, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-API-LAYERING", "lifecycle-spi-is-one-advanced-middleware-family")]
    public void LifecycleSpi_IsConsolidatedUnderAdvancedMiddleware()
    {
        Type[] lifecycleTypes =
        [
            typeof(Agent),
            typeof(AgentExtensions),
            typeof(IAgent),
            typeof(IAgent<>),
            typeof(ISupervisor),
            typeof(ISupervisor<>),
            typeof(StopContext),
            typeof(StopSupervisorContext),
            typeof(Supervisor),
            typeof(ActivePipeContext<>),
            typeof(ActivePipeContextAgent<>),
            typeof(AsyncPipeContextAgent<>),
            typeof(AsyncPipeContextFilter<>),
            typeof(AsyncPipeContextHandle<>),
            typeof(AsyncPipeContextPipe<>),
            typeof(ConstantPipeContextHandle<>),
            typeof(IActivePipeContextAgent<>),
            typeof(IActivePipeContextHandle<>),
            typeof(IAsyncPipeContextAgent<>),
            typeof(IAsyncPipeContextHandle<>),
            typeof(IPipeContextAgent<>),
            typeof(IPipeContextFactory<>),
            typeof(IPipeContextHandle<>),
            typeof(PipeContextAgent<>),
            typeof(PipeContextSupervisor<>),
            typeof(SupervisorExtensions),
        ];

        Assert.All(lifecycleTypes, type => Assert.Equal("ViciOne.ServiceBus.Advanced.Middleware", type.Namespace));

        Type[] exportedTypes = lifecycleTypes
            .Select(static type => type.Assembly)
            .Distinct()
            .SelectMany(static assembly => assembly.GetExportedTypes())
            .ToArray();
        Assert.DoesNotContain(exportedTypes, static type => type.Namespace == "ViciOne.ServiceBus.Agents");
        Assert.DoesNotContain(exportedTypes, static type => type.FullName is
            "ViciOne.ServiceBus.Middleware.Agent" or
            "ViciOne.ServiceBus.Middleware.Supervisor" or
            "ViciOne.ServiceBus.Advanced.IAsyncPipeContextHandle`1" or
            "ViciOne.ServiceBus.Advanced.IPipeContextHandle`1");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-API-LAYERING", "flow-control-facades-and-partition-spi-hide-runtime-mechanics")]
    public void FlowControlApi_ExposesConfigurationFacadesAndPartitionSpiWithoutRuntimeMechanics()
    {
        Type[] partitionSpi =
        [
            typeof(IPartitionHashGenerator),
            typeof(IPartitioner),
            typeof(IPartitioner<>),
            typeof(Murmur3PartitionHashGenerator),
            typeof(PipePartitioner),
            typeof(PartitionKeyProvider<>),
        ];
        Assert.All(partitionSpi, type =>
        {
            Assert.True(type.IsPublic || type.IsNestedPublic);
            Assert.Equal("ViciOne.ServiceBus.Advanced.Middleware", type.Namespace);
        });
        Assert.DoesNotContain(typeof(IAsyncDisposable), typeof(IPartitioner).GetInterfaces());
        Assert.Contains(typeof(IAsyncDisposable), typeof(PipePartitioner).GetInterfaces());

        Type[] configurationFacades =
        [
            typeof(CircuitBreakerConfigurationExtensions),
            typeof(CircuitBreakerOptions),
            typeof(ConcurrencyLimitConfigurationExtensions),
            typeof(ConsumerConcurrencyLimitConfigurationExtensions),
            typeof(PartitionerConfigurationExtensions),
            typeof(RateLimitConfigurationExtensions),
        ];
        Assert.All(configurationFacades, type =>
        {
            Assert.True(type.IsPublic);
            Assert.Equal("ViciOne.ServiceBus.Configuration", type.Namespace);
        });

        MethodInfo[] concurrencyMethods =
        [
            .. typeof(ConcurrencyLimitConfigurationExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
            .. typeof(ConsumerConcurrencyLimitConfigurationExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
        ];
        Assert.NotEmpty(concurrencyMethods);
        Assert.All(concurrencyMethods, method => Assert.Equal("UseConcurrencyLimit", method.Name));
        Assert.All(
            concurrencyMethods.SelectMany(method => method.GetParameters()).Where(parameter => parameter.ParameterType == typeof(string)),
            parameter => Assert.Equal("limiterId", parameter.Name));

        MethodInfo[] sagaConcurrencyMethods = typeof(ViciOne.ServiceBus.Sagas.Configuration.SagaPipelineConfigurationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == "UseConcurrencyLimit")
            .ToArray();
        Assert.Equal(2, sagaConcurrencyMethods.Length);
        Assert.All(
            sagaConcurrencyMethods.SelectMany(method => method.GetParameters()).Where(parameter => parameter.ParameterType == typeof(string)),
            parameter => Assert.Equal("limiterId", parameter.Name));

        string[] removedTypes =
        [
            "ViciOne.ServiceBus.Configuration.ConcurrentMessageLimitExtensions",
            "ViciOne.ServiceBus.Middleware.ConcurrencyLimitFilter`1",
            "ViciOne.ServiceBus.Middleware.ConcurrencyLimiter",
            "ViciOne.ServiceBus.Middleware.ConsumeConcurrencyLimitFilter`1",
            "ViciOne.ServiceBus.Middleware.IConcurrencyLimiter",
            "ViciOne.ServiceBus.Middleware.IHashGenerator",
            "ViciOne.ServiceBus.Middleware.Murmur3UnsafeHashGenerator",
            "ViciOne.ServiceBus.Middleware.Partition",
            "ViciOne.ServiceBus.Middleware.PartitionFilter`1",
            "ViciOne.ServiceBus.Middleware.Partitioner",
            "ViciOne.ServiceBus.Middleware.RateLimitFilter`1",
        ];
        Assert.All(removedTypes, typeName => Assert.Null(ProductAssemblyFacts.Core.GetType(typeName, throwOnError: false)));

        string[] implementationNamespaces =
        [
            "ViciOne.ServiceBus.Middleware.CircuitBreaker",
            "ViciOne.ServiceBus.Middleware.ConcurrencyLimiting",
            "ViciOne.ServiceBus.Middleware.Partitioning",
            "ViciOne.ServiceBus.Middleware.RateLimiting",
        ];
        Assert.DoesNotContain(ProductAssemblyFacts.Core.GetExportedTypes(), type =>
            implementationNamespaces.Contains(type.Namespace, StringComparer.Ordinal));

        Type concurrencyFilter = ProductAssemblyFacts.Core.GetType(
            "ViciOne.ServiceBus.Middleware.ConcurrencyLimiting.ConcurrencyLimitFilter`1",
            throwOnError: true)!;
        Assert.Equal(typeof(object), concurrencyFilter.BaseType);
        Assert.DoesNotContain(
            concurrencyFilter.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
            method => method.Name == "StopAgentAsync");

        Assembly sagaAssembly = typeof(ViciOne.ServiceBus.Sagas.Configuration.SagaPipelineConfigurationExtensions).Assembly;
        Type partitionSagaSpecification = sagaAssembly.GetType(
            "ViciOne.ServiceBus.Configuration.PartitionSagaSpecification`1",
            throwOnError: true)!;
        Assert.False(partitionSagaSpecification.IsPublic);
        Assert.True(partitionSagaSpecification.IsSealed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-API-LAYERING", "batch-contracts-hide-collector-runtime")]
    public void BatchApi_ExposesContractsAndConfigurationWithoutRuntimeMechanics()
    {
        Assert.True(typeof(Batch<>).IsPublic);
        Assert.True(typeof(IBatchConfigurator<>).IsPublic);
        Assert.True(typeof(BatchOptions).IsPublic);
        Assert.DoesNotContain(ProductAssemblyFacts.Core.GetExportedTypes(), static type =>
            type.Namespace == "ViciOne.ServiceBus.Batching");

        string[] runtimeTypeNames =
        [
            "ViciOne.ServiceBus.Batching.BatchCollector`1",
            "ViciOne.ServiceBus.Batching.BatchCollector`2",
            "ViciOne.ServiceBus.Batching.BatchConsumer`1",
            "ViciOne.ServiceBus.Batching.BatchConsumerFactory`1",
            "ViciOne.ServiceBus.Batching.BatchCollectorLifetime",
            "ViciOne.ServiceBus.Batching.IBatchCollector`1",
            "ViciOne.ServiceBus.Batching.MessageBatch`1",
            "ViciOne.ServiceBus.Configuration.BatchConsumerMessageConnector`2",
            "ViciOne.ServiceBus.Configuration.BatchConsumerMessageSpecification`2",
            "ViciOne.ServiceBus.Configuration.BatchMessageConnectorFactory`2",
        ];
        Type[] runtimeTypes = runtimeTypeNames
            .Select(typeName => ProductAssemblyFacts.Core.GetType(typeName, throwOnError: true)!)
            .ToArray();
        Assert.All(runtimeTypes, static type => Assert.False(type.IsPublic));
        Assert.All(runtimeTypes.Where(static type => type.IsClass), static type => Assert.True(type.IsSealed));

        Type messageBatch = runtimeTypes.Single(static type => type.Name == "MessageBatch`1");
        Assert.All(
            messageBatch.GetProperties(BindingFlags.Instance | BindingFlags.Public),
            static property => Assert.Null(property.SetMethod));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-API-BASELINE", "application-root-is-exactly-the-versioned-baseline")]
    public void ApplicationApi_IsExactlyTheVersionedRootNamespaceBaseline()
    {
        string[] expected = Source("docs/api/application-api.txt")
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith('#'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Type[] publicTypes = ProductAssemblyFacts.ArchitectureAnchors
            .Concat(ProductAssemblyFacts.CapabilityAssemblies)
            .Distinct()
            .SelectMany(static assembly => assembly.GetExportedTypes())
            .Where(static type => !type.IsNested)
            .DistinctBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        string[] actual = publicTypes
            .Where(static type => type.Namespace == "ViciOne.ServiceBus")
            .Select(static type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);

        int rootExtensionMethods = publicTypes
            .Where(static type => type.Namespace == "ViciOne.ServiceBus")
            .SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Count(static method => method.IsDefined(typeof(ExtensionAttribute), inherit: false));
        Assert.InRange(rootExtensionMethods, 0, 120);

        Assert.DoesNotContain(publicTypes, static type =>
            type.GetCustomAttribute<EditorBrowsableAttribute>()?.State == EditorBrowsableState.Never);
        Assert.DoesNotContain(publicTypes, static type => type.IsDefined(typeof(ObsoleteAttribute), inherit: false));
        Assert.DoesNotContain(publicTypes, static type =>
            (type.Namespace ?? string.Empty).Contains(".Internals", StringComparison.Ordinal));

        Type applicationBuilder = typeof(IBusRegistrationConfigurator);
        Type[] builderClosure = [applicationBuilder, .. applicationBuilder.GetInterfaces()];
        int builderMembers = builderClosure
            .SelectMany(static type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(static member => member is PropertyInfo or EventInfo
                || member is MethodInfo { IsSpecialName: false })
            .Select(static member => member.ToString())
            .Distinct(StringComparer.Ordinal)
            .Count();
        Assert.InRange(builderMembers, 0, 20);
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
            "IOutboxStore<",
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
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "exactly-three-explicit-adapters-live-under-reliable-messaging")]
    public void SchedulerAdapterEntryPoints_AreOnlyExplicitReliableMessagingChoices()
    {
        string sourceRoot = Path.Combine(RepositoryLayout.Root, "src");
        Regex declaration = new(
            @"public\s+static[^\r\n{;]*\b(?<name>Use[A-Za-z0-9_]*Scheduler)(?:<[^>\r\n]+>)?\s*\(\s*this\s+(?<receiver>[A-Za-z0-9_<>.,?]+)",
            RegexOptions.CultureInvariant);
        var entryPoints = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => declaration.Matches(File.ReadAllText(path)).Select(match => new
            {
                Path = path,
                Name = match.Groups["name"].Value,
                Receiver = match.Groups["receiver"].Value,
            }))
            .OrderBy(static entry => entry.Name, StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            new
            {
                Name = "UseInMemoryQuartzScheduler",
                Receiver = "IReliableMessagingConfigurator<TBus>",
                Namespace = "ViciOne.ServiceBus.Quartz",
            },
            new
            {
                Name = "UseQuartzScheduler",
                Receiver = "IReliableMessagingConfigurator<TBus>",
                Namespace = "ViciOne.ServiceBus.Quartz",
            },
            new
            {
                Name = "UseTransportScheduler",
                Receiver = "IReliableMessagingConfigurator",
                Namespace = "ViciOne.ServiceBus.Configuration",
            },
        };

        Assert.Equal(expected.Select(static entry => entry.Name), entryPoints.Select(static entry => entry.Name));
        Assert.All(expected.Zip(entryPoints), pair =>
        {
            var (contract, entry) = pair;
            string source = File.ReadAllText(entry.Path);
            Assert.Equal(contract.Receiver, entry.Receiver);
            Assert.Contains($"namespace {contract.Namespace};", source, StringComparison.Ordinal);
            Assert.Contains("IReliableMessagingProviderConfigurator", source, StringComparison.Ordinal);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-CONFIGURATION", "complete-fail-fast-family-inventory")]
    public void StaticConfigurationInventory_IsCompleteAndBoundToActionableExecutingTests()
    {
        using JsonDocument manifest = JsonDocument.Parse(Source("docs/static-configuration-validation.json"));
        JsonElement root = manifest.RootElement;
        Assert.Equal(2, root.GetProperty("schemaVersion").GetInt32());
        Assert.Contains("before runtime messaging begins", root.GetProperty("scope").GetString(), StringComparison.Ordinal);

        JsonElement[] optionEntries = root.GetProperty("options").EnumerateArray().ToArray();
        int declaredModelCount = root.GetProperty("reviewBaseline").GetProperty("currentConcreteOptionsModels").GetInt32();
        Assert.Equal(declaredModelCount, optionEntries.Length);
        Assert.Equal(41, declaredModelCount);

        Regex optionDeclaration = new(
            @"public\s+(?<modifiers>(?:(?:sealed|abstract)\s+)*)class\s+(?<name>[A-Za-z0-9_]+Options)(?<generic><[^>{\r\n]+>)?(?=\s|:)",
            RegexOptions.CultureInvariant);
        string sourceRoot = Path.Combine(RepositoryLayout.Root, "src");
        var declarations = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => optionDeclaration.Matches(File.ReadAllText(path)).Select(match => new
            {
                Type = match.Groups["name"].Value + match.Groups["generic"].Value.Replace(" ", string.Empty, StringComparison.Ordinal),
                Source = Path.GetRelativePath(RepositoryLayout.Root, path).Replace(Path.DirectorySeparatorChar, '/'),
                Modifiers = match.Groups["modifiers"].Value,
            }))
            .ToArray();

        string[] excluded = root.GetProperty("excludedDeclarations").EnumerateArray()
            .Select(static entry => $"{entry.GetProperty("type").GetString()}|{entry.GetProperty("source").GetString()}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] discoveredExcluded = declarations
            .Where(declaration => declaration.Modifiers.Contains("abstract", StringComparison.Ordinal)
                || declaration.Type is "ConfigureBusHealthCheckServiceOptions" or "ValidateViciOneServiceBusHostOptions")
            .Select(static declaration => $"{declaration.Type}|{declaration.Source}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(excluded, discoveredExcluded);

        var concreteDeclarations = declarations
            .Where(declaration => !discoveredExcluded.Contains($"{declaration.Type}|{declaration.Source}", StringComparer.Ordinal))
            .ToDictionary(static declaration => $"{declaration.Type}|{declaration.Source}", StringComparer.Ordinal);
        string[] inventoried = optionEntries
            .Select(static entry => $"{entry.GetProperty("type").GetString()}|{entry.GetProperty("source").GetString()}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(concreteDeclarations.Keys.Order(StringComparer.Ordinal), inventoried);

        Assert.All(optionEntries, entry =>
        {
            string key = $"{entry.GetProperty("type").GetString()}|{entry.GetProperty("source").GetString()}";
            Assert.Contains("sealed", concreteDeclarations[key].Modifiers, StringComparison.Ordinal);

            string boundary = entry.GetProperty("boundary").GetString()!;
            Assert.Contains(boundary, new[] { "construction", "configuration-materialization", "host-start", "registration" });
            AssertEvidence(entry.GetProperty("positiveTest"));

            int invariantCount = entry.GetProperty("invariantCount").GetInt32();
            JsonElement negativeTest = entry.GetProperty("negativeTest");
            if (invariantCount == 0)
                Assert.Equal(JsonValueKind.Null, negativeTest.ValueKind);
            else
                AssertEvidence(negativeTest);

            if (boundary == "host-start")
            {
                string registration = Source(entry.GetProperty("registrationSource").GetString()!);
                Assert.Contains("ValidateOnStart", registration, StringComparison.Ordinal);
            }
        });

        string invocationSource = string.Join('\n', Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        Assert.All(root.GetProperty("invocationValueRecords").EnumerateArray(), record =>
            Assert.Matches($@"public\s+sealed\s+record\s+{Regex.Escape(record.GetString()!)}\b", invocationSource));

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

        static void AssertEvidence(JsonElement evidence)
        {
            string path = evidence.GetProperty("path").GetString()!;
            string method = evidence.GetProperty("method").GetString()!;
            string test = Source(path);
            Assert.Contains($"{method}(", test, StringComparison.Ordinal);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HERITAGE-DISPOSITION", "all-retained-shapes-carry-current-capability")]
    public void HeritageDisposition_IsTerminalAndCompatibilityOnlyIdentityIsAbsent()
    {
        using JsonDocument manifest = JsonDocument.Parse(Source("docs/api-heritage-disposition.json"));
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
        string formerProductIdentity = "Mass" + "Transit";
        Assert.DoesNotContain(productIdentityFiles, path =>
            File.ReadAllText(path).Contains(formerProductIdentity, StringComparison.OrdinalIgnoreCase));
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));

}
