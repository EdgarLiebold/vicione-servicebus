using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

/// <summary>Guards deterministic navigation from a C# type name to its physical source file.</summary>
public sealed class SourceFileNamingArchitectureTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ReviewedPartialFragments =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/ChildSpecificationPipeBuilder.cs"] = Types("public class PipeConfigurator`1"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/Filters/SplitFilterPipeSpecification.cs"] = Types("public class PipeConfigurator`1"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/PipeBuilder.cs"] = Types("public class PipeConfigurator`1"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/SpecificationPipeBuilder.cs"] = Types("public class PipeConfigurator`1"),
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ReviewedCohesiveMultiTypeFiles =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["src/Transports/ViciOne.ServiceBus.ActiveMq/Configuration/ActiveMqTransportOptions.cs"] = Types(
                "public enum ActiveMqTransportProtocol`0", "public class ActiveMqTransportOptions`0"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/OneTimeSetupMethod.cs"] = Types(
                "internal class OneTimeSetupMethod`0", "public interface OneTimeContext`0"),
            ["src/ViciOne.ServiceBus.Abstractions/Transports/ITransportSendFailureClassifier.cs"] = Types(
                "public enum TransportSendFailureKind`0", "public interface ITransportSendFailureClassifier`0"),
            ["src/ViciOne.ServiceBus/Configuration/BusCompositionValidation.cs"] = Types(
                "internal interface IBusCompositionRegistration`0", "internal class BusCompositionRegistration`1",
                "internal interface IBusTransportRegistration`0", "internal class BusTransportRegistration`1",
                "internal interface IBusFeatureRegistration`0", "internal class BusFeatureRegistration`1",
                "internal class BusCompositionRegistrations`0", "internal class BusCompositionStartupValidator`1"),
            ["src/ViciOne.ServiceBus/DependencyInjection/DeferredBusScopedContextProviders.cs"] = Types(
                "internal class DeferredBusScopedContextProvider`1", "internal class AmbientTransactionScopedBusContextProvider`1",
                "internal class BufferedBusScopedBusContextProvider`1"),
            ["src/ViciOne.ServiceBus/Transports/Components/KillSwitch/KillSwitchEndpoint.cs"] = Types(
                "internal interface IKillSwitchEndpoint`0", "internal class RestartableReceiveEndpointKillSwitchEndpoint`0"),
            ["tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/Internals/ReflectionImplementationTestDrivers.cs"] = Types(
                "public class DynamicImplementationBuilderTestDriver`0", "public class BusInstanceBuilderTestDriver`0",
                "public class ReadPropertyTestDriver`2", "public class WritePropertyTestDriver`2"),
            ["tests/Transports/ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests/DeployTopologyContracts/ActiveMqDeployTopologyContracts.cs"] = Types(
                "public interface OrderSubmitted`0", "public interface OrderEvent`0", "public interface PackageShipped`0",
                "public interface PackageEvent`0", "public interface CustomerEvent`0"),
            ["tests/Transports/ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests/PublishContracts/ActiveMqPublishContracts.cs"] = Types(
                "public interface FirstPublishedContract`0", "public interface SecondPublishedContract`0"),
            ["tests/Transports/ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests/DeployTopologyContracts/AmazonSqsDeployTopologyContracts.cs"] = Types(
                "public interface OrderSubmitted`0", "public interface OrderEvent`0", "public interface PackageShipped`0",
                "public interface PackageEvent`0", "public interface CustomerEvent`0"),
            ["tests/Transports/ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests/PublishContracts/AmazonSqsPublishContracts.cs"] = Types(
                "public interface FirstPublishedContract`0", "public interface SecondPublishedContract`0"),
            ["tests/Transports/ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests/TopologyContracts/AzureServiceBusTopologyContracts.cs"] = Types(
                "public interface ExplicitTopologyContract`0", "public interface NamespaceTopologyContract`0",
                "public interface ExcludedTopologyContract`0"),
            ["tests/ViciOne.ServiceBus.Tests/DependencyInjection/ContainerDiscovery/ContainerDiscoveryTypes.cs"] = Types(
                "public class DiscoveryMarker`0", "public record DiscoveryPing`0", "public record DiscoveryPong`0",
                "public record PingReceived`0", "public record PingAcknowledged`0", "public record PingCompleted`0",
                "public record PingArguments`0", "public record PingLog`0", "public class DiscoveryPingConsumer`0",
                "public class DiscoveryPingConsumerDefinition`0", "public class DiscoveryExcludedConsumer`0",
                "public class DiscoveryPingSaga`0", "public class DiscoveryPingState`0", "public class DiscoveryPingStateMachine`0",
                "public class DiscoveryPingStateDefinition`0", "public class PingActivity`0", "public class PingSecondActivity`0"),
            ["tests/ViciOne.ServiceBus.Tests/JobService/Integration/ContainerJobDiscovery/ContainerJobDiscoveryTypes.cs"] = Types(
                "public class DiscoveryMarker`0", "public record CrunchNumbers`0", "public record JobSnapshot`0",
                "public class JobObservation`0", "public class CrunchNumbersConsumer`0"),
        };

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "every-source-file-has-a-declared-primary-type-or-reviewed-grouping")]
    public void EverySourceFile_IsNamedForItsPrimaryTypeOrAReviewedCohesiveGroup()
    {
        string[] sources = RepositoryLayout.ProductProjects
            .Concat(RepositoryLayout.NativeTestProjects)
            .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath"))
            .Select(Path.GetFullPath)
            .Where(IsRepositorySource)
            .Distinct(RepositoryLayout.PathComparer)
            .OrderBy(RepositoryLayout.RelativeToRoot, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(sources);

        var inspected = sources.Select(Inspect).ToArray();
        string[] violations = inspected
            .Where(static file => !file.IsConforming && !file.IsReviewedGrouping)
            .Select(static file => $"{file.Path}: declares [{string.Join(", ", file.TypeIdentities)}]")
            .ToArray();
        string[] manifestMismatches = inspected
            .Where(static file => file.IsReviewedPath && !file.IsReviewedGrouping)
            .Select(static file => $"{file.Path}: declares [{string.Join(", ", file.TypeIdentities)}]")
            .Concat(inspected
                .Where(static file => file.IsReviewedPartialPath && !file.IsReviewedPartial)
                .Select(static file => $"{file.Path}: declares [{string.Join(", ", file.TypeIdentities)}]"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] staleExceptions = inspected
            .Where(static file => file.IsConforming && file.IsReviewedGrouping)
            .Select(static file => file.Path)
            .Concat(ReviewedCohesiveMultiTypeFiles.Keys.Except(
                inspected.Select(static file => file.Path),
                StringComparer.Ordinal))
            .Concat(ReviewedPartialFragments.Keys.Except(
                inspected.Select(static file => file.Path),
                StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Source files without a primary type:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
        Assert.True(
            manifestMismatches.Length == 0,
            $"Reviewed source-file manifests no longer match:{Environment.NewLine}{string.Join(Environment.NewLine, manifestMismatches)}");
        Assert.True(
            staleExceptions.Length == 0,
            $"Stale source-file naming exceptions:{Environment.NewLine}{string.Join(Environment.NewLine, staleExceptions)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "core-project-root-types-use-root-namespace")]
    public void CoreProjectRoot_TypesUseCoreRootNamespace()
    {
        string coreRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus");
        string[] violations = Directory.EnumerateFiles(coreRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path))
                .GetCompilationUnitRoot(TestContext.Current.CancellationToken)
                .DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Select(declaration => new
                {
                    Path = RepositoryLayout.RelativeToRoot(path),
                    Namespace = declaration.Name.ToString(),
                }))
            .Where(static declaration => declaration.Namespace != "ViciOne.ServiceBus")
            .Select(static declaration => $"{declaration.Path}: {declaration.Namespace}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "context-runtime-files-belong-to-owning-capabilities")]
    public void ContextRuntimeFiles_AreOwnedByTheirCapabilities()
    {
        (string Directory, string Namespace, string[] Files)[] groups =
        [
            ("src/ViciOne.ServiceBus/Batching/Contexts", "ViciOne.ServiceBus.Batching.Contexts",
                ["BatchConsumeContext.cs", "MessageBatch.cs"]),
            ("src/ViciOne.ServiceBus/Consumers/Contexts", "ViciOne.ServiceBus.Consumers.Contexts",
                ["ConsumerConsumeContextProxy.cs", "ConsumerConsumeContextScope.cs"]),
            ("src/ViciOne.ServiceBus/Context/Activities", "ViciOne.ServiceBus.Context",
                ["ActivityContextProxy.cs", "ActivityContextScope.cs", "CompensateContextProxy.cs", "CompensateContextScope.cs",
                    "ExecuteContextProxy.cs", "ExecuteContextScope.cs", "HostCompensateActivityContext.cs", "HostExecuteActivityContext.cs"]),
            ("src/ViciOne.ServiceBus/Context/Consumption", "ViciOne.ServiceBus.Context",
                ["BaseConsumeContext.cs", "ConsumeContextOutgoingMessages.cs", "ConsumeContextProxy.cs", "ConsumeContextScope.cs",
                    "DeserializerConsumeContext.cs", "MessageConsumeContext.cs", "UnavailableConsumeContext.cs"]),
            ("src/ViciOne.ServiceBus.Mediator/Contexts", "ViciOne.ServiceBus.Mediator.Contexts",
                ["MediatorConsumeContext.cs", "MediatorSendMessageContext.cs"]),
            ("src/ViciOne.ServiceBus/Middleware/Contexts", "ViciOne.ServiceBus.Middleware",
                ["BindContextProxy.cs", "CorrelationIdConsumeContextProxy.cs"]),
            ("src/ViciOne.ServiceBus/RetryPolicies/Contexts", "ViciOne.ServiceBus.RetryPolicies",
                ["RetryCompensateContext.cs", "RetryExecuteContext.cs"]),
            ("src/ViciOne.ServiceBus/Scheduling/Contexts", "ViciOne.ServiceBus.Scheduling",
                ["ConsumeMessageSchedulerContext.cs", "ScheduleMessageRedeliveryContext.cs"]),
            ("src/ViciOne.ServiceBus/Transactions/Contexts", "ViciOne.ServiceBus.Transactions",
                ["IManagedTransactionContext.cs", "ITransactionContextFactory.cs", "SystemTransactionContext.cs",
                    "SystemTransactionContextFactory.cs"]),
            ("src/ViciOne.ServiceBus/Transports/Receiving", "ViciOne.ServiceBus.Transports",
                ["NoLockReceiveContext.cs", "ReceiveContextProxy.cs", "TransportReceiveContext.cs"]),
            ("src/ViciOne.ServiceBus/Transports/Sending", "ViciOne.ServiceBus.Transports",
                ["MessageSendContext.cs", "TransportSendContext.cs"]),
        ];

        foreach ((string relativeDirectory, string expectedNamespace, string[] files) in groups)
        {
            string directory = Path.Combine(RepositoryLayout.Root, relativeDirectory);
            Assert.All(files, file =>
            {
                string path = Path.Combine(directory, file);
                Assert.True(File.Exists(path), RepositoryLayout.RelativeToRoot(path));
                Assert.Equal([expectedNamespace], ReadNamespaces(path, TestContext.Current.CancellationToken));
            });
        }

        string formerFlatDirectory = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Context");
        Assert.Empty(Directory.EnumerateFiles(formerFlatDirectory, "*.cs", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "consumer-event-and-message-data-files-have-explicit-owners")]
    public void ConsumerEventAndMessageDataFiles_AreGroupedByResponsibility()
    {
        (string Directory, string Namespace, string[] Files)[] groups =
        [
            ("src/ViciOne.ServiceBus/Consumers", "ViciOne.ServiceBus.Consumers",
                ["DefaultConstructorConsumerFactory.cs", "DelegateConsumerFactory.cs", "InstanceConsumerFactory.cs", "ObjectConsumerFactory.cs"]),
            ("src/ViciOne.ServiceBus/Consumers/Contexts", "ViciOne.ServiceBus.Consumers.Contexts",
                ["ConsumerConsumeContextProxy.cs", "ConsumerConsumeContextScope.cs"]),
            ("src/ViciOne.ServiceBus/Consumers/Conventions", "ViciOne.ServiceBus.Consumers.Conventions",
                ["ConsumerConventionCache.cs"]),
            ("src/ViciOne.ServiceBus/Consumers/Metadata", "ViciOne.ServiceBus.Consumers.Metadata",
                ["ConsumerMetadataCache.cs", "ConsumerRegistrationMetadata.cs"]),
            ("src/ViciOne.ServiceBus/Events/Faults", "ViciOne.ServiceBus.Events.Faults",
                ["FaultEvent.cs", "FaultExceptionInfo.cs", "ReceiveFaultEvent.cs"]),
            ("src/ViciOne.ServiceBus/Events/Readiness", "ViciOne.ServiceBus.Events.Readiness",
                ["BusReadyEvent.cs", "HostReadyEvent.cs"]),
            ("src/ViciOne.ServiceBus/Events/Receiving", "ViciOne.ServiceBus.Events.Receiving",
                ["ReceiveEndpointCompletedEvent.cs", "ReceiveEndpointFaultedEvent.cs", "ReceiveEndpointReadyEvent.cs",
                    "ReceiveEndpointStoppingEvent.cs", "ReceiveTransportCompletedEvent.cs", "ReceiveTransportFaultedEvent.cs",
                    "ReceiveTransportReadyEvent.cs"]),
            ("src/ViciOne.ServiceBus/MessageData", "ViciOne.ServiceBus.MessageData",
                ["EncryptedMessageDataRepository.cs", "FileSystemMessageDataRepository.cs", "InMemoryMessageDataRepository.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/Admission", "ViciOne.ServiceBus.MessageData.Admission",
                ["MessageDataAdmissionEvidence.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/Configuration", "ViciOne.ServiceBus.MessageData.Configuration",
                ["ActivityMessageDataConfigurationObserver.cs", "GetMessageDataObjectArrayTransformConfiguration.cs",
                    "GetMessageDataObjectDictionaryTransformConfiguration.cs", "GetMessageDataObjectTransformConfiguration.cs",
                    "GetMessageDataTransformConfiguration.cs", "GetMessageDataTransformSpecification.cs",
                    "IMessageDataTransformConfiguration.cs", "PutMessageDataObjectArrayTransformConfiguration.cs",
                    "PutMessageDataObjectDictionaryTransformConfiguration.cs", "PutMessageDataObjectTransformConfiguration.cs",
                    "PutMessageDataTransformConfiguration.cs", "PutMessageDataTransformSpecification.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/Conventions", "ViciOne.ServiceBus.MessageData.Conventions",
                ["IMessageDataMessageConsumeTopologyConvention.cs", "IMessageDataMessageSendTopologyConvention.cs",
                    "MessageDataConsumeTopologyConvention.cs", "MessageDataMessageConsumeTopology.cs",
                    "MessageDataMessageConsumeTopologyConvention.cs", "MessageDataMessageSendTopology.cs",
                    "MessageDataMessageSendTopologyConvention.cs", "MessageDataSendTopologyConvention.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/Converters", "ViciOne.ServiceBus.MessageData.Converters",
                ["ByteArrayMessageDataConverter.cs", "IMessageDataConverter.cs", "MessageDataConverter.cs",
                    "StreamMessageDataConverter.cs", "StringMessageDataConverter.cs", "SystemTextJsonObjectMessageDataConverter.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/Internals", "ViciOne.ServiceBus.MessageData.Internals",
                ["MessageDataTypeClassifier.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/PropertyProviders", "ViciOne.ServiceBus.MessageData.PropertyProviders",
                ["BytesMessageDataReader.cs", "GetMessageDataPropertyProvider.cs", "IMessageDataReader.cs",
                    "MessageDataReaderFactory.cs", "ObjectMessageDataReader.cs", "PutMessageDataPropertyProvider.cs",
                    "StreamMessageDataReader.cs", "StringMessageDataReader.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/Serialization", "ViciOne.ServiceBus.MessageData.Serialization",
                ["IInlineMessageData.cs", "IMessageDataReference.cs"]),
            ("src/ViciOne.ServiceBus/MessageData/Values", "ViciOne.ServiceBus.MessageData.Values",
                ["BytesInlineMessageData.cs", "DeserializedMessageData.cs", "EmptyMessageData.cs", "GetMessageData.cs",
                    "InlineMessageData.cs", "PutMessageData.cs", "StoredMessageData.cs", "StringInlineMessageData.cs"]),
        ];

        foreach ((string relativeDirectory, string expectedNamespace, string[] expectedFiles) in groups)
        {
            string directory = Path.Combine(RepositoryLayout.Root, relativeDirectory);
            string[] actualFiles = Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedFiles.Order(StringComparer.Ordinal), actualFiles);
            Assert.All(actualFiles, file => Assert.Equal(
                [expectedNamespace],
                ReadNamespaces(Path.Combine(directory, file), TestContext.Current.CancellationToken)));
        }

        Assert.Empty(Directory.EnumerateFiles(
            Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Events"),
            "*.cs",
            SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Consumer")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Metadata")));

        string selectorPath = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus",
            "Configuration",
            "MessageDataRepositorySelectorExtensions.cs");
        Assert.Equal(
            ["ViciOne.ServiceBus.Configuration"],
            ReadNamespaces(selectorPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "batch-runtime-files-have-dedicated-namespace")]
    public void BatchRuntimeFiles_HaveOneDedicatedDirectoryAndNamespace()
    {
        string batchingRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Batching");
        string runtimeDirectory = Path.Combine(batchingRoot, "Runtime");
        string[] expectedFiles =
        [
            "BatchCollector.cs",
            "BatchCollectorLifetime.cs",
            "BatchConsumer.cs",
            "BatchConsumerFactory.cs",
            "BatchRuntimeSettings.cs",
            "IBatchCollector.cs",
        ];

        Assert.Empty(Directory.EnumerateFiles(batchingRoot, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            expectedFiles,
            Directory.EnumerateFiles(runtimeDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
        Assert.All(expectedFiles, file => Assert.Equal(
            ["ViciOne.ServiceBus.Batching.Runtime"],
            ReadNamespaces(Path.Combine(runtimeDirectory, file), TestContext.Current.CancellationToken)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "logging-api-internals-diagnostics-and-test-support-have-explicit-owners")]
    public void LoggingFiles_AreGroupedByApiLayerAndOwningPackage()
    {
        string coreLogging = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Logging");
        Assert.Equal(
            ["EnabledLogger.cs", "ILogContext.cs", "LogMessage.cs"],
            Directory.EnumerateFiles(coreLogging, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));

        (string Directory, string Namespace, string[] Files)[] groups =
        [
            ("Diagnostics", "ViciOne.ServiceBus.Logging.Diagnostics",
                ["ActivityObservation.cs", "DiagnosticPropagationHeaders.cs", "LogContextActivityExtensions.cs", "StartedActivity.cs"]),
            ("Internal", "ViciOne.ServiceBus.Logging.Internal",
                ["BusLogContext.cs", "ServiceBusLogCategories.cs", "SingleLoggerFactory.cs"]),
            ("Monitoring", "ViciOne.ServiceBus.Logging.Monitoring",
                ["LogContextInstrumentationExtensions.cs", "LogContextInstrumentationState.cs", "MetricOperation.cs"]),
        ];

        foreach ((string directoryName, string expectedNamespace, string[] files) in groups)
        {
            string directory = Path.Combine(coreLogging, directoryName);
            Assert.Equal(
                files.Order(StringComparer.Ordinal),
                Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Order(StringComparer.Ordinal));
            Assert.All(files, file => Assert.Equal(
                [expectedNamespace],
                ReadNamespaces(Path.Combine(directory, file), TestContext.Current.CancellationToken)));
        }

        string testingLogging = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.Testing", "Logging");
        string[] testingFiles = ["TextWriterLogger.cs", "TextWriterLoggerFactory.cs", "TextWriterLoggerOptions.cs"];
        Assert.Equal(
            testingFiles.Order(StringComparer.Ordinal),
            Directory.EnumerateFiles(testingLogging, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
        Assert.All(testingFiles, file => Assert.Equal(
            ["ViciOne.ServiceBus.Testing"],
            ReadNamespaces(Path.Combine(testingLogging, file), TestContext.Current.CancellationToken)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "runtime-hosting-health-and-reliable-messaging-internals-have-explicit-owners")]
    public void RuntimeAndOperationalInternals_AreGroupedByCapability()
    {
        (string Directory, string Namespace, string[] Files)[] groups =
        [
            ("src/ViciOne.ServiceBus/Runtime", "ViciOne.ServiceBus.Runtime",
                ["ServiceBusRuntime.cs", "ServiceBusRuntime.Lifecycle.cs"]),
            ("src/ViciOne.ServiceBus/Hosting", "ViciOne.ServiceBus.Hosting",
                ["ServiceBusHostedService.cs"]),
            ("src/ViciOne.ServiceBus/Monitoring/Health", "ViciOne.ServiceBus.Monitoring.Health",
                ["BusHealthCheck.cs", "ConfigureBusHealthCheckServiceOptions.cs"]),
            ("src/ViciOne.ServiceBus/Operations/ReliableMessaging", "ViciOne.ServiceBus.Operations.ReliableMessaging",
                ["DurableSenderHealthCheck.cs", "ReliableMessagingOperations.cs"]),
        ];

        foreach ((string relativeDirectory, string expectedNamespace, string[] files) in groups)
        {
            string directory = Path.Combine(RepositoryLayout.Root, relativeDirectory);
            Assert.Equal(
                files.Order(StringComparer.Ordinal),
                Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Order(StringComparer.Ordinal));
            Assert.All(files, file => Assert.Equal(
                [expectedNamespace],
                ReadNamespaces(Path.Combine(directory, file), TestContext.Current.CancellationToken)));
        }

        string monitoringRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Monitoring");
        Assert.Equal(
            ["ServiceBusTelemetry.cs"],
            Directory.EnumerateFiles(monitoringRoot, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "core-project-root-contains-only-project-infrastructure")]
    public void CoreProjectRoot_ContainsOnlyProjectInfrastructure()
    {
        string coreRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus");
        string[] expectedFiles =
        [
            "GlobalUsings.cs",
            "ViciOne.ServiceBus.csproj",
            "packages.lock.json",
        ];

        string[] actualFiles = Directory.EnumerateFiles(coreRoot, "*", SearchOption.TopDirectoryOnly)
            .Select(static path => Path.GetFileName(path)
                ?? throw new InvalidOperationException($"Source path '{path}' has no file name."))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedFiles, actualFiles);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "abstractions-root-is-an-application-contract-boundary")]
    public void AbstractionsProjectRoot_ContainsOnlyApplicationContractsAndProjectInfrastructure()
    {
        string abstractionsRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.Abstractions");
        string[] expectedRootFiles =
        [
            "ConsumeContext.cs",
            "GlobalUsings.cs",
            "IBus.cs",
            "IBusControl.cs",
            "IConsumer.cs",
            "IOutgoingMessages.cs",
            "IPublishEndpoint.cs",
            "ISendEndpoint.cs",
            "ISendEndpointProvider.cs",
            "MessageHeaders.cs",
            "MessageLimits.cs",
            "PublishOptions.cs",
            "RequestOptions.cs",
            "ScheduleOptions.cs",
            "SendOptions.cs",
            "ViciOne.ServiceBus.Abstractions.csproj",
            "ViciOne.ServiceBus.Abstractions.csproj.DotSettings",
            "packages.lock.json",
        ];

        Assert.Equal(
            expectedRootFiles,
            Directory.EnumerateFiles(abstractionsRoot, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Order(StringComparer.Ordinal));

        string[] applicationContracts = expectedRootFiles
            .Where(static file => file.EndsWith(".cs", StringComparison.Ordinal) && file != "GlobalUsings.cs")
            .ToArray();
        Assert.All(applicationContracts, file => Assert.Equal(
            ["ViciOne.ServiceBus"],
            ReadNamespaces(Path.Combine(abstractionsRoot, file), TestContext.Current.CancellationToken)));

        SourceFileInspection outgoingContract = Inspect(Path.Combine(abstractionsRoot, "IOutgoingMessages.cs"));
        Assert.Equal(["public interface IOutgoingMessages`0"], outgoingContract.TypeIdentities);

        string implementationPath = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus",
            "Context",
            "Consumption",
            "ConsumeContextOutgoingMessages.cs");
        Assert.True(File.Exists(implementationPath), RepositoryLayout.RelativeToRoot(implementationPath));
        Assert.Equal(["ViciOne.ServiceBus.Context"], ReadNamespaces(implementationPath, TestContext.Current.CancellationToken));
        Assert.Equal(["internal class ConsumeContextOutgoingMessages`0"], Inspect(implementationPath).TypeIdentities);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "context-infrastructure-has-explicit-api-and-runtime-owners")]
    public void ContextInfrastructure_HasExplicitApiAndRuntimeOwners()
    {
        string advancedContexts = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus.Abstractions",
            "Advanced",
            "Contexts");
        string[] advancedProxyFiles = ["PublishContextProxy.cs", "SendContextProxy.cs", "SendContextScope.cs"];
        Assert.All(advancedProxyFiles, file =>
        {
            string path = Path.Combine(advancedContexts, file);
            Assert.True(File.Exists(path), RepositoryLayout.RelativeToRoot(path));
            Assert.Equal(["ViciOne.ServiceBus.Advanced"], ReadNamespaces(path, TestContext.Current.CancellationToken));
        });

        (string Directory, string Namespace, string[] Files)[] internalGroups =
        [
            (
                "src/ViciOne.ServiceBus.Abstractions/Internals/Dispatching",
                "ViciOne.ServiceBus.Internals.Dispatching",
                ["PublishEndpointDispatcher.cs", "ResponseEndpointDispatcher.cs", "SendEndpointDispatcher.cs"]),
            (
                "src/ViciOne.ServiceBus.Abstractions/Internals/Outgoing",
                "ViciOne.ServiceBus.Internals.Outgoing",
                ["OutgoingOptionsPipe.cs", "OutgoingOptionsSnapshot.cs", "PublishOptionsPipe.cs", "ScheduleOptionsPipe.cs",
                    "SendOptionsPipe.cs"]),
        ];

        foreach ((string relativeDirectory, string expectedNamespace, string[] expectedFiles) in internalGroups)
        {
            string directory = Path.Combine(RepositoryLayout.Root, relativeDirectory);
            Assert.True(Directory.Exists(directory), relativeDirectory);
            Assert.Equal(
                expectedFiles,
                Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .OfType<string>()
                    .Order(StringComparer.Ordinal));
            Assert.All(expectedFiles, file => Assert.Equal(
                [expectedNamespace],
                ReadNamespaces(Path.Combine(directory, file), TestContext.Current.CancellationToken)));
        }

        string pendingFaultPath = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus",
            "RetryPolicies",
            "PendingFaultCollection.cs");
        Assert.True(File.Exists(pendingFaultPath), RepositoryLayout.RelativeToRoot(pendingFaultPath));
        Assert.Equal(["ViciOne.ServiceBus.RetryPolicies"], ReadNamespaces(pendingFaultPath, TestContext.Current.CancellationToken));
        Assert.Equal(["internal class PendingFaultCollection`0"], Inspect(pendingFaultPath).TypeIdentities);

        Assert.False(Directory.Exists(Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus.Abstractions",
            "Context")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "client-files-have-explicit-responsibility-owners")]
    public void ClientFiles_AreGroupedByPublicFactoryContextsEndpointsAndRequestMechanics()
    {
        string clientsRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Clients");
        Assert.Equal(
            ["ClientFactory.cs", "ScopedClientFactory.cs"],
            Directory.EnumerateFiles(clientsRoot, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
        Assert.All(
            Directory.EnumerateFiles(clientsRoot, "*.cs", SearchOption.TopDirectoryOnly),
            file => Assert.Equal(
                ["ViciOne.ServiceBus.Clients"],
                ReadNamespaces(file, TestContext.Current.CancellationToken)));

        (string Directory, string Namespace, string[] Files)[] groups =
        [
            ("Contexts", "ViciOne.ServiceBus.Clients.Contexts",
                ["BusClientFactoryContext.cs", "HostReceiveEndpointClientFactoryContext.cs", "ReceiveEndpointClientFactoryContext.cs"]),
            ("Endpoints", "ViciOne.ServiceBus.Clients.Endpoints",
                ["PublishRequestSendEndpoint.cs", "ReceiveEndpointPublishRequestSendEndpoint.cs",
                    "ReceiveEndpointSendRequestSendEndpoint.cs", "RequestSendEndpoint.cs", "SendRequestSendEndpoint.cs"]),
            ("Requests", "ViciOne.ServiceBus.Clients.Requests",
                ["ClientRequestHandle.Completion.cs", "ClientRequestHandle.Responses.cs", "ClientRequestHandle.cs",
                    "HandlerConnectHandle.cs", "MessageResponse.cs", "RequestClient.cs", "ResponseHandlerConnectHandle.cs"]),
        ];

        foreach ((string directoryName, string expectedNamespace, string[] expectedFiles) in groups)
        {
            string directory = Path.Combine(clientsRoot, directoryName);
            string[] actualFiles = Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedFiles.Order(StringComparer.Ordinal), actualFiles);
            Assert.All(actualFiles, file => Assert.Equal(
                [expectedNamespace],
                ReadNamespaces(Path.Combine(directory, file), TestContext.Current.CancellationToken)));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "lifecycle-spi-files-live-under-advanced-middleware")]
    public void LifecycleSpiFiles_LiveUnderTheirAdvancedMiddlewareNamespace()
    {
        string[] abstractionFiles =
        [
            "Agent.cs",
            "AgentExtensions.cs",
            "IAgent.cs",
            "ISupervisor.cs",
            "StopContext.cs",
            "StopSupervisorContext.cs",
            "Supervisor.cs",
        ];
        string[] coreFiles =
        [
            "ActivePipeContext.cs",
            "ActivePipeContextAgent.cs",
            "AsyncPipeContextAgent.cs",
            "AsyncPipeContextFilter.cs",
            "AsyncPipeContextHandle.cs",
            "AsyncPipeContextPipe.cs",
            "ConstantPipeContextHandle.cs",
            "IActivePipeContextAgent.cs",
            "IActivePipeContextHandle.cs",
            "IAsyncPipeContextAgent.cs",
            "IAsyncPipeContextHandle.cs",
            "IPipeContextAgent.cs",
            "IPipeContextFactory.cs",
            "IPipeContextHandle.cs",
            "PipeContextAgent.cs",
            "PipeContextSupervisor.cs",
            "SupervisorExtensions.cs",
        ];

        string abstractionDirectory = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus.Abstractions",
            "Advanced",
            "Middleware");
        string coreDirectory = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus",
            "Advanced",
            "Middleware");
        string[] paths = abstractionFiles.Select(file => Path.Combine(abstractionDirectory, file))
            .Concat(coreFiles.Select(file => Path.Combine(coreDirectory, file)))
            .ToArray();

        Assert.All(paths, path =>
        {
            Assert.True(File.Exists(path), RepositoryLayout.RelativeToRoot(path));
            string[] namespaces = ReadNamespaces(path, TestContext.Current.CancellationToken);
            Assert.Equal(["ViciOne.ServiceBus.Advanced.Middleware"], namespaces);
        });

        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Agents")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "tests", "ViciOne.ServiceBus.Tests", "Agents")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "flow-control-files-separate-public-spi-configuration-and-runtime-mechanics")]
    public void FlowControlFiles_AreGroupedByApiLayerAndResponsibility()
    {
        (string Directory, string Namespace, string[] Files)[] groups =
        [
            (
                "src/ViciOne.ServiceBus/Advanced/Middleware/Partitioning",
                "ViciOne.ServiceBus.Advanced.Middleware",
                ["IPartitionedTaskExecutor.cs", "IPartitionHashGenerator.cs", "IPartitioner.cs", "Murmur3PartitionHashGenerator.cs",
                    "PartitionedTaskExecutor.cs", "PartitionKeyProvider.cs", "PipePartitioner.cs"]),
            (
                "src/ViciOne.ServiceBus/Configuration/CircuitBreaker",
                "ViciOne.ServiceBus.Configuration",
                ["CircuitBreakerConfigurationExtensions.cs", "CircuitBreakerOptions.cs", "CircuitBreakerPipeSpecification.cs"]),
            (
                "src/ViciOne.ServiceBus/Configuration/ConcurrencyLimit",
                "ViciOne.ServiceBus.Configuration",
                ["ConcurrencyLimitConfigurationExtensions.cs", "ConcurrencyLimitConfigurationObserver.cs",
                    "ConcurrencyLimitConsumePipeSpecification.cs", "ConcurrencyLimitConsumerConfigurationObserver.cs",
                    "ConcurrencyLimitHandlerConfigurationObserver.cs", "ConcurrencyLimitPipeSpecification.cs",
                    "ConsumerConcurrencyLimitConfigurationExtensions.cs"]),
            (
                "src/ViciOne.ServiceBus/Configuration/Partition",
                "ViciOne.ServiceBus.Configuration",
                ["PartitionerConfigurationExtensions.cs", "PartitionerPipeSpecification.cs", "PartitionMessageConfigurationObserver.cs",
                    "PartitionMessageSpecification.cs"]),
            (
                "src/ViciOne.ServiceBus/Configuration/RateLimiting",
                "ViciOne.ServiceBus.Configuration",
                ["RateLimitConfigurationExtensions.cs", "RateLimitPipeSpecification.cs"]),
            (
                "src/ViciOne.ServiceBus/Middleware/CircuitBreaker",
                "ViciOne.ServiceBus.Middleware.CircuitBreaker",
                ["CircuitBreakerFilter.cs", "CircuitBreakerSettings.cs", "CircuitBreakerStateMachine.cs", "CircuitBreakerTelemetry.cs"]),
            (
                "src/ViciOne.ServiceBus/Middleware/ConcurrencyLimiting",
                "ViciOne.ServiceBus.Middleware.ConcurrencyLimiting",
                ["ConcurrencyLimitFilter.cs", "ConcurrencyLimiter.cs", "ConsumeConcurrencyLimitFilter.cs", "IConcurrencyLimiter.cs"]),
            (
                "src/ViciOne.ServiceBus/Middleware/Partitioning",
                "ViciOne.ServiceBus.Middleware.Partitioning",
                ["Partition.cs", "PartitionCoordinator.cs", "PartitionFilter.cs"]),
            (
                "src/ViciOne.ServiceBus/Middleware/RateLimiting",
                "ViciOne.ServiceBus.Middleware.RateLimiting",
                ["RateLimitFilter.cs"]),
        ];

        foreach ((string relativeDirectory, string expectedNamespace, string[] expectedFiles) in groups)
        {
            string directory = Path.Combine(RepositoryLayout.Root, relativeDirectory);
            string[] actualFiles = Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Order(StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(expectedFiles.Order(StringComparer.Ordinal), actualFiles);
            Assert.All(actualFiles, file => Assert.Equal(
                [expectedNamespace],
                ReadNamespaces(Path.Combine(directory, file), TestContext.Current.CancellationToken)));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "saga-project-folders-mirror-top-level-namespaces")]
    public void SagaProjectSourceFolders_MirrorTheirTopLevelNamespace()
    {
        const string namespaceRoot = "ViciOne.ServiceBus";
        string projectRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.Sagas");
        string[] violations = FindTopLevelNamespaceFolderViolations(projectRoot, namespaceRoot);

        Assert.Empty(violations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "initializers-project-folders-mirror-public-namespaces")]
    public void InitializersProjectSourceFolders_MirrorTheirTopLevelNamespace()
    {
        const string namespaceRoot = "ViciOne.ServiceBus";
        string projectRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.Initializers");
        string projectPath = Path.Combine(projectRoot, "ViciOne.ServiceBus.Initializers.csproj");
        List<string> violations = FindTopLevelNamespaceFolderViolations(projectRoot, namespaceRoot).ToList();
        string rootNamespace = MsBuildEvaluation.PropertyOf(projectPath, "RootNamespace");
        if (!StringComparer.Ordinal.Equals(namespaceRoot, rootNamespace))
            violations.Add($"{RepositoryLayout.RelativeToRoot(projectPath)}: RootNamespace is {rootNamespace}, expected {namespaceRoot}");

        Assert.Empty(violations.Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "futures-project-folders-mirror-public-namespaces")]
    public void FuturesProjectSourceFolders_MirrorTheirPublicNamespaces()
    {
        const string namespaceRoot = "ViciOne.ServiceBus";
        string projectRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.Futures");
        string projectPath = Path.Combine(projectRoot, "ViciOne.ServiceBus.Futures.csproj");
        List<string> violations = FindNamespaceFolderViolations(projectRoot, namespaceRoot).ToList();
        string rootNamespace = MsBuildEvaluation.PropertyOf(projectPath, "RootNamespace");
        if (!StringComparer.Ordinal.Equals(namespaceRoot, rootNamespace))
            violations.Add($"{RepositoryLayout.RelativeToRoot(projectPath)}: RootNamespace is {rootNamespace}, expected {namespaceRoot}");

        Assert.Empty(violations.Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "job-service-project-folders-mirror-public-namespaces")]
    public void JobServiceProjectSourceFolders_MirrorTheirPublicNamespaces()
    {
        const string namespaceRoot = "ViciOne.ServiceBus";
        string projectRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.JobService");
        string projectPath = Path.Combine(projectRoot, "ViciOne.ServiceBus.JobService.csproj");
        List<string> violations = FindNamespaceFolderViolations(projectRoot, namespaceRoot).ToList();
        string rootNamespace = MsBuildEvaluation.PropertyOf(projectPath, "RootNamespace");
        if (!StringComparer.Ordinal.Equals(namespaceRoot, rootNamespace))
            violations.Add($"{RepositoryLayout.RelativeToRoot(projectPath)}: RootNamespace is {rootNamespace}, expected {namespaceRoot}");

        Assert.Empty(violations.Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "entity-framework-source-is-grouped-by-current-responsibility")]
    public void EntityFrameworkProject_UsesOnlyCurrentResponsibilityFolders()
    {
        string projectRoot = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "Persistence",
            "ViciOne.ServiceBus.EntityFrameworkCore");
        string[] expectedDirectories =
        [
            "Configuration",
            "Infrastructure",
            "Locking",
            "MessageJournal",
            "Outbox",
            "ReliableMessaging",
            "Serialization",
        ];
        string[] expectedRootFiles =
        [
            "ApiSurfaceGlobalUsings.cs",
            "GlobalUsings.cs",
            "ViciOne.ServiceBus.EntityFrameworkCore.csproj",
            "ViciOne.ServiceBus.EntityFrameworkCore.csproj.DotSettings",
            "packages.lock.json",
        ];

        string[] actualDirectories = Directory.EnumerateDirectories(projectRoot)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] actualRootFiles = Directory.EnumerateFiles(projectRoot)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedDirectories, actualDirectories);
        Assert.Equal(expectedRootFiles, actualRootFiles);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "job-service-root-contains-only-entry-points-and-infrastructure")]
    public void JobServiceProjectRoot_ContainsOnlyEntryPointsAndInfrastructure()
    {
        string jobServiceRoot = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.JobService");
        string[] allowedFiles =
        [
            "GlobalUsings.cs",
            "ViciOne.ServiceBus.JobService.csproj",
            "packages.lock.json",
        ];

        string[] actualFiles = Directory.EnumerateFiles(jobServiceRoot, "*", SearchOption.TopDirectoryOnly)
            .Select(static path => Path.GetFileName(path)
                ?? throw new InvalidOperationException($"Source path '{path}' has no file name."))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "terminal-partitioners-have-explicit-lifetime-owners")]
    public void TerminalPipePartitioners_AreCreatedOnlyByComponentsWithATerminalLifetimeOwner()
    {
        string[] expectedOwners =
        [
            "src/Scheduling/ViciOne.ServiceBus.Quartz/Configuration/QuartzEndpointDefinition.cs",
            "src/Scheduling/ViciOne.ServiceBus.Quartz/QuartzSchedulingExtensions.cs",
        ];
        string[] actualOwners = RepositoryLayout.ProductProjects
            .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath"))
            .Select(Path.GetFullPath)
            .Where(IsRepositorySource)
            .Where(path => CreatedTypeNames(path).Contains(nameof(PipePartitioner), StringComparer.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedOwners, actualOwners);

        string dedicatedSpecification = Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus",
            "Configuration",
            "Partition",
            "PartitionerPipeSpecification.cs");
        string[] dedicatedCreatedTypes = CreatedTypeNames(dedicatedSpecification);
        Assert.Contains("PartitionCoordinator", dedicatedCreatedTypes);
        Assert.DoesNotContain(nameof(PipePartitioner), dedicatedCreatedTypes);
    }

    private static SourceFileInspection Inspect(string source)
    {
        string path = RepositoryLayout.RelativeToRoot(source);
        string fileName = Path.GetFileNameWithoutExtension(source);
        string sourceText = File.ReadAllText(source);
        CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(sourceText)
            .GetCompilationUnitRoot();
        MemberDeclarationSyntax[] declarations = syntax.DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .Where(IsTopLevelType)
            .ToArray();
        string[] typeNames = declarations.Select(TypeName).ToArray();
        string[] typeIdentities = declarations.Select(TypeIdentity).ToArray();
        bool reviewedPartialPath = ReviewedPartialFragments.TryGetValue(path, out IReadOnlySet<string>? expectedPartialTypes);
        bool reviewedPartial = reviewedPartialPath
            && declarations.Length > 0
            && declarations.All(IsPartial)
            && expectedPartialTypes!.SetEquals(typeIdentities);
        bool conventionPartial = declarations.Length > 0
            && declarations.All(declaration => IsPartial(declaration)
                && fileName.StartsWith(TypeName(declaration) + ".", StringComparison.Ordinal));
        bool primaryType = typeNames.Contains(fileName, StringComparer.Ordinal);
        bool publicTypesMatch = path.StartsWith("src/", StringComparison.Ordinal)
            ? declarations
                .Where(IsPublic)
                .All(declaration => StringComparer.Ordinal.Equals(fileName, TypeName(declaration)))
            : declarations.All(declaration =>
                StringComparer.Ordinal.Equals(fileName, TypeName(declaration)) || !ContainsTests(declaration));
        bool generatedFile = (fileName.EndsWith(".generated", StringComparison.Ordinal)
                || fileName.EndsWith(".g", StringComparison.Ordinal))
            && syntax.GetLeadingTrivia().ToFullString().Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
        bool infrastructureFile = declarations.Length == 0
            && (fileName.EndsWith("GlobalUsings", StringComparison.Ordinal)
                || fileName.EndsWith("AssemblyInfo", StringComparison.Ordinal)
                || fileName.EndsWith("AssemblyAttributes", StringComparison.Ordinal));
        bool reviewedPath = ReviewedCohesiveMultiTypeFiles.TryGetValue(path, out IReadOnlySet<string>? expectedTypes);
        bool reviewedGrouping = reviewedPath && expectedTypes!.SetEquals(typeIdentities);

        return new SourceFileInspection(
            path,
            typeIdentities,
            infrastructureFile || generatedFile || reviewedPartial || conventionPartial || (primaryType && publicTypesMatch),
            reviewedPath,
            reviewedGrouping,
            reviewedPartialPath,
            reviewedPartial);
    }

    private static bool IsRepositorySource(string path) =>
        path.StartsWith(RepositoryLayout.Root + Path.DirectorySeparatorChar, RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison);

    private static string[] ReadNamespaces(string path, CancellationToken cancellationToken) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(path), cancellationToken: cancellationToken)
            .GetCompilationUnitRoot(cancellationToken)
            .DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(static declaration => declaration.Name.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string[] FindTopLevelNamespaceFolderViolations(string projectRoot, string namespaceRoot) =>
        Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                RepositoryLayout.PathComparison))
            .Where(path => Path.GetFileName(path) != "GlobalUsings.cs")
            .Select(path =>
            {
                string relativePath = Path.GetRelativePath(projectRoot, path);
                string? actualFolder = Path.GetDirectoryName(relativePath)?
                    .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                string[] namespaces = ReadNamespaces(path, CancellationToken.None);

                if (namespaces.Length != 1)
                    return $"{RepositoryLayout.RelativeToRoot(path)}: expected one namespace, found {namespaces.Length}";

                string namespaceName = namespaces[0];
                if (namespaceName != namespaceRoot
                    && !namespaceName.StartsWith(namespaceRoot + ".", StringComparison.Ordinal))
                {
                    return $"{RepositoryLayout.RelativeToRoot(path)}: namespace {namespaceName} is outside {namespaceRoot}";
                }

                string? expectedFolder = namespaceName == namespaceRoot
                    ? null
                    : namespaceName[(namespaceRoot.Length + 1)..].Split('.')[0];
                return StringComparer.Ordinal.Equals(actualFolder, expectedFolder)
                    ? null
                    : $"{RepositoryLayout.RelativeToRoot(path)}: expected top-level folder {expectedFolder ?? "<root>"} for {namespaceName}";
            })
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] FindNamespaceFolderViolations(string projectRoot, string namespaceRoot) =>
        Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                RepositoryLayout.PathComparison))
            .Where(path => Path.GetFileName(path) != "GlobalUsings.cs")
            .Select(path =>
            {
                string relativePath = Path.GetRelativePath(projectRoot, path);
                string actualFolder = Path.GetDirectoryName(relativePath) ?? string.Empty;
                string[] namespaces = ReadNamespaces(path, CancellationToken.None);

                if (namespaces.Length != 1)
                    return $"{RepositoryLayout.RelativeToRoot(path)}: expected one namespace, found {namespaces.Length}";

                string namespaceName = namespaces[0];
                if (namespaceName != namespaceRoot
                    && !namespaceName.StartsWith(namespaceRoot + ".", StringComparison.Ordinal))
                {
                    return $"{RepositoryLayout.RelativeToRoot(path)}: namespace {namespaceName} is outside {namespaceRoot}";
                }

                string expectedFolder = namespaceName == namespaceRoot
                    ? string.Empty
                    : namespaceName[(namespaceRoot.Length + 1)..].Replace('.', Path.DirectorySeparatorChar);
                return StringComparer.Ordinal.Equals(actualFolder, expectedFolder)
                    ? null
                    : $"{RepositoryLayout.RelativeToRoot(path)}: expected folder {expectedFolder} for {namespaceName}";
            })
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] CreatedTypeNames(string path) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(path))
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Select(static creation => creation.Type switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.ValueText,
                _ => creation.Type.ToString(),
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static bool IsTopLevelType(MemberDeclarationSyntax declaration) =>
        declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax
        && declaration.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax;

    private static string TypeName(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
        _ => throw new InvalidOperationException($"Unsupported declaration kind {declaration.Kind()}.")
    };

    private static bool IsPartial(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Modifiers.Any(SyntaxKind.PartialKeyword),
        DelegateDeclarationSyntax @delegate => @delegate.Modifiers.Any(SyntaxKind.PartialKeyword),
        _ => false,
    };

    private static bool IsPublic(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Modifiers.Any(SyntaxKind.PublicKeyword),
        DelegateDeclarationSyntax @delegate => @delegate.Modifiers.Any(SyntaxKind.PublicKeyword),
        _ => false,
    };

    private static string TypeIdentity(MemberDeclarationSyntax declaration)
    {
        SyntaxTokenList modifiers = declaration switch
        {
            BaseTypeDeclarationSyntax type => type.Modifiers,
            DelegateDeclarationSyntax @delegate => @delegate.Modifiers,
            _ => default,
        };
        string visibility = modifiers.Any(SyntaxKind.PublicKeyword) ? "public"
            : modifiers.Any(SyntaxKind.FileKeyword) ? "file"
            : "internal";
        string kind = declaration switch
        {
            InterfaceDeclarationSyntax => "interface",
            EnumDeclarationSyntax => "enum",
            RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
            RecordDeclarationSyntax => "record",
            StructDeclarationSyntax => "struct",
            ClassDeclarationSyntax => "class",
            DelegateDeclarationSyntax => "delegate",
            _ => throw new InvalidOperationException($"Unsupported declaration kind {declaration.Kind()}.")
        };
        int arity = declaration switch
        {
            TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Count ?? 0,
            DelegateDeclarationSyntax @delegate => @delegate.TypeParameterList?.Parameters.Count ?? 0,
            _ => 0,
        };

        return $"{visibility} {kind} {TypeName(declaration)}`{arity}";
    }

    private static bool ContainsTests(MemberDeclarationSyntax declaration) =>
        TypeName(declaration).EndsWith("Tests", StringComparison.Ordinal)
        || declaration.DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .SelectMany(static method => method.AttributeLists)
        .SelectMany(static list => list.Attributes)
        .SelectMany(static attribute => attribute.Name.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        .Any(static identifier => identifier.Identifier.ValueText is "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute");

    private static IReadOnlySet<string> Types(params string[] identities) =>
        new HashSet<string>(identities, StringComparer.Ordinal);

    private sealed record SourceFileInspection(
        string Path,
        IReadOnlyList<string> TypeIdentities,
        bool IsConforming,
        bool IsReviewedPath,
        bool IsReviewedGrouping,
        bool IsReviewedPartialPath,
        bool IsReviewedPartial);
}
