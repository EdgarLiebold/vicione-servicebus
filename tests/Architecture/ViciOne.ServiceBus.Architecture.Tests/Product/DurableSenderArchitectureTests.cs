using System.Reflection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class DurableSenderArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ARCHITECTURE", "application-api-is-visible-and-provider-spi-is-hidden")]
    public void ApplicationApi_IsVisibleWhileProviderSpiStaysHiddenAndProviderNeutral()
    {
        Type[] applicationApi =
        [
            typeof(DurableSendId),
            typeof(DurableSendOptions),
            typeof(DurableSendReceipt),
            typeof(IDurableSender<>),
        ];
        Assert.All(applicationApi, type => Assert.Equal("ViciOne.ServiceBus", type.Namespace));

        Type[] operationsApi =
        [
            typeof(DurableSendQuarantineQuery),
            typeof(DurableSendQuarantinePage),
            typeof(DurableSendQuarantineEntry),
            typeof(DurableSendOperationOutcome),
            typeof(DurableSendOperationResult),
            typeof(IReliableMessagingOperations<>),
        ];
        Assert.All(operationsApi, type => Assert.Equal("ViciOne.ServiceBus.Operations", type.Namespace));

        Type[] providerSpi =
        [
            typeof(SerializedDurableSend),
            typeof(DurableSendAdmissionDisposition),
            typeof(DurableSendAdmissionResult),
            typeof(DurableSendDelivery),
            typeof(DurableSendDispatchContext),
            typeof(DurableSendDispatchResult),
            typeof(DurableSendCompletionMode),
            typeof(DurableSendLease),
            typeof(DurableSendStoreLimits),
            typeof(DurableSendStoreSnapshot),
            typeof(IOutboxStore<>),
            typeof(IDurableSendDispatcher<>),
            typeof(IDurableSendAdmission<>),
            typeof(IDurableSendConsumerCompletion),
            typeof(BusPersistenceIdentity<>),
            typeof(IReliableMessagingProviderConfigurator),
        ];

        Assert.All(providerSpi, contract =>
        {
            Assert.True(contract.IsPublic);
            Assert.Equal("ViciOne.ServiceBus.Providers.Persistence", contract.Namespace);
        });

        Type[] advancedSpi =
        [
            typeof(IConsumerConcurrencyGate<>),
            typeof(ConsumerConcurrencyGate<>),
            typeof(PartitionedConsumerConcurrencyGate<,>),
            typeof(IPayloadAdmissionEvaluator<>),
            typeof(PayloadAdmissionEvaluator<>),
            typeof(IPayloadSerializationBuffer),
            typeof(PayloadAdmissionPolicy),
            typeof(IMessageSensitivityInspector),
            typeof(MessageSensitivityInspector),
            typeof(IMessageDiagnosticRedactor),
            typeof(MessageDiagnosticRedactor),
            typeof(EndpointQosDeclaration),
            typeof(EndpointQosTopologyValidator),
        ];
        Assert.All(advancedSpi, contract =>
            Assert.StartsWith("ViciOne.ServiceBus.Advanced", contract.Namespace, StringComparison.Ordinal));
        Assert.Same(ProductAssemblyFacts.Abstractions, typeof(SerializedDurableSend).Assembly);
        Assert.Same(ProductAssemblyFacts.Core, typeof(ReliableMessagingOptions<>).Assembly);
        Assert.DoesNotContain(typeof(SerializedDurableSend).GetProperties(), property => property.PropertyType == typeof(Type));
        Assert.DoesNotContain("AssemblyQualifiedName", Source(
            "src/ViciOne.ServiceBus.Abstractions/DurableSend/SerializedDurableSend.cs"),
            StringComparison.Ordinal);
        string[] forbiddenProviderReferences =
        [
            "Amazon",
            "Apache.NMS",
            "Azure.Messaging",
            "EntityFramework",
            "Npgsql",
            "RabbitMQ",
            "SqlClient",
        ];
        Assert.DoesNotContain(ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Abstractions),
            name => forbiddenProviderReferences.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ARCHITECTURE", "runtime-state-machines-remain-internal")]
    public void RuntimeStateMachines_AreInternalSealedAndReachedOnlyThroughPublicContracts()
    {
        string[] implementationNames =
        [
            "ViciOne.ServiceBus.Providers.Persistence.InMemoryReliableStore`1",
            "ViciOne.ServiceBus.Providers.Persistence.DurableSendAdmission`1",
            "ViciOne.ServiceBus.Providers.Persistence.TypedDurableSender`1",
            "ViciOne.ServiceBus.Providers.Persistence.ReliableMessagingDeliveryService`1",
            "ViciOne.ServiceBus.Operations.ReliableMessagingOperations`1",
            "ViciOne.ServiceBus.Providers.Persistence.DurableSendConsumerCompletion`1",
            "ViciOne.ServiceBus.Configuration.ReliableMessagingConfigurator`1",
            "ViciOne.ServiceBus.Configuration.BusCompositionStartupValidator`1",
            "ViciOne.ServiceBus.Providers.Transports.InMemoryDurableSendDispatcher`1",
            "ViciOne.ServiceBus.Providers.Transports.InMemoryDurableSendCompletionFilter",
            "ViciOne.ServiceBus.Providers.Transports.InMemoryDurableSendContext",
        ];

        foreach (string name in implementationNames)
        {
            Type implementation = ProductAssemblyFacts.Core.GetType(name, throwOnError: true)!;
            Assert.False(implementation.IsPublic);
            Assert.True(implementation.IsSealed);
        }

        Type transportMessage = ProductAssemblyFacts.Core.GetType(
            "ViciOne.ServiceBus.InMemoryTransport.InMemoryTransportMessage",
            throwOnError: true)!;
        PropertyInfo capability = transportMessage.GetProperty(
            "DurableSendContext",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.False(capability.GetMethod!.IsPublic);
        Assert.False(capability.PropertyType.IsPublic);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-OWNERSHIP", "one-delivery-loop-and-one-quarantine-operations-api")]
    public void ReliableMessaging_HasOneDeliveryLoopAndOneQuarantineOperationsApi()
    {
        string sourceRoot = Path.Combine(RepositoryLayout.Root, "src");
        string[] deliveryLoops = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return source.Contains("class ReliableMessagingDeliveryService<", StringComparison.Ordinal)
                    || source.Contains("class DurableSenderDeliveryService<", StringComparison.Ordinal)
                    || source.Contains("class BusOutboxDeliveryService<", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(RepositoryLayout.Root, path))
            .ToArray();
        Assert.Equal(
            ["src/ViciOne.ServiceBus/DurableSend/ReliableMessagingDeliveryService.cs"],
            deliveryLoops);

        string[] retiredEntryPoints = ["UseInMemoryOutbox", "AddEntityFrameworkOutbox", "UseBusOutbox"];
        string[] applicationRoots =
        [
            sourceRoot,
            Path.Combine(RepositoryLayout.Root, "samples"),
        ];
        var retiredOccurrences = applicationRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(entry => retiredEntryPoints.Any(token => entry.Source.Contains(token, StringComparison.Ordinal)))
            .Select(entry => Path.GetRelativePath(RepositoryLayout.Root, entry.Path))
            .ToArray();
        Assert.Empty(retiredOccurrences);

        Type[] quarantineApis = ProductAssemblyFacts.Abstractions.GetExportedTypes()
            .Where(type => type.GetMethods().Any(method =>
                method.Name is "GetOutboxQuarantineAsync" or "GetInboxQuarantineAsync"))
            .ToArray();
        Type operations = Assert.Single(quarantineApis);
        Assert.Equal(typeof(IReliableMessagingOperations<>), operations);
        Assert.Equal(
            ["AbandonAsync", "DiscardAsync", "GetInboxQuarantineAsync", "GetOutboxQuarantineAsync", "GetSnapshotAsync", "RequeueAsync"],
            operations.GetMethods().Select(static method => method.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "persistent-mandatory-confirmed-before-accepted")]
    public void RabbitMqDispatcher_ReportsAcceptanceOnlyAfterPersistentMandatoryConfirmedPublish()
    {
        string dispatcher = Source(
            "src/Transports/ViciOne.ServiceBus.RabbitMq/DurableSend/RabbitMqDurableSendDispatcher.cs");

        Assert.Contains("context.Durable = true;", dispatcher, StringComparison.Ordinal);
        Assert.Contains("rabbitMqContext.Mandatory = true;", dispatcher, StringComparison.Ordinal);
        Assert.Contains("rabbitMqContext.AwaitAck = true;", dispatcher, StringComparison.Ordinal);
        Assert.Contains("RabbitMqTransportAcceptanceRequirement", dispatcher, StringComparison.Ordinal);

        string transport = Source(
            "src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/RabbitMqSendTransportContext.cs");
        int confirmationPreflight = transport.IndexOf(
            "transportContext.ConnectionContext.PublisherConfirmation",
            StringComparison.Ordinal);
        int topology = transport.IndexOf("_configureTopologyFilter.ConfigureAsync", StringComparison.Ordinal);
        int publish = transport.IndexOf("transportContext.BasicPublishAsync", StringComparison.Ordinal);
        Assert.True(confirmationPreflight >= 0);
        Assert.True(topology > confirmationPreflight);
        Assert.True(publish > topology);

        int send = dispatcher.IndexOf("await endpoint.SendAsync(", StringComparison.Ordinal);
        int accepted = dispatcher.IndexOf(
            "return DurableSendDispatchResult.TransportAccepted;",
            StringComparison.Ordinal);
        Assert.True(send >= 0);
        Assert.True(accepted > send);
        Assert.Equal(
            1,
            dispatcher.Split(
                "return DurableSendDispatchResult.TransportAccepted;",
                StringSplitOptions.None).Length - 1);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-ARCHITECTURE", "completion-after-full-pipeline-and-never-wire")]
    public void InMemoryAdapter_CompletesAfterReceiveOwnedWorkAndNeverClaimsDurableTransportAcceptance()
    {
        string filter = Source(
            "src/ViciOne.ServiceBus/InMemoryTransport/DurableSend/InMemoryDurableSendCompletionFilter.cs");
        int pipeline = filter.IndexOf("await next.SendAsync(context)", StringComparison.Ordinal);
        int receiveCompleted = filter.IndexOf("await context.ReceiveCompleted", StringComparison.Ordinal);
        int completion = filter.IndexOf("ConsumerCompletion.CompleteAsync", StringComparison.Ordinal);
        Assert.True(pipeline >= 0);
        Assert.True(receiveCompleted > pipeline);
        Assert.True(completion > receiveCompleted);

        string dispatcher = Source(
            "src/ViciOne.ServiceBus/InMemoryTransport/DurableSend/InMemoryDurableSendDispatcher.cs");
        Assert.Contains("GetOrAddPayload", dispatcher, StringComparison.Ordinal);
        Assert.Contains("AwaitConsumerCompletion", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("TransportAccepted", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Headers.Set", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("ConsumerCompletion.ToString", dispatcher, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-ARCHITECTURE", "catalog-and-commit-preflights-precede-persistence")]
    public void AdmissionPreconditions_RunBeforeAnyPersistentMutation()
    {
        string sender = Source("src/ViciOne.ServiceBus/DurableSend/DurableSendAdmission.cs");
        int catalogLookup = sender.IndexOf("_contractCatalog.TryGetMessageType", StringComparison.Ordinal);
        int storeAdmission = sender.IndexOf("_store\n                .AdmitAsync", StringComparison.Ordinal);
        Assert.True(catalogLookup >= 0);
        Assert.True(storeAdmission > catalogLookup);

        string efStore = Source(
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableStore.cs");
        int preflight = efStore.IndexOf("_commitDurabilityValidator.ValidateAsync", StringComparison.Ordinal);
        int capacityLookup = efStore.IndexOf("Set<DurableSendCapacityState>()", preflight, StringComparison.Ordinal);
        Assert.True(preflight >= 0);
        Assert.True(capacityLookup > preflight);

        string validator = Source(
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkDurableSendCommitDurabilityValidator.cs");
        Assert.Contains("Microsoft.EntityFrameworkCore.SqlServer", validator, StringComparison.Ordinal);
        Assert.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", validator, StringComparison.Ordinal);
        Assert.Contains("Microsoft.EntityFrameworkCore.Sqlite", validator, StringComparison.Ordinal);
        Assert.Contains("has no built-in Durable Sender synchronous-commit validator", validator, StringComparison.Ordinal);
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));

}
