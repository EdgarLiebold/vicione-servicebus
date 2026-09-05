using System.Reflection;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Diagnostics;
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
            typeof(IDurableSenderOperations<>),
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
            typeof(IDurableSendStore<>),
            typeof(IDurableSendDispatcher<>),
            typeof(IDurableSendAdmission<>),
            typeof(IDurableSendConsumerCompletion),
            typeof(BusPersistenceIdentity<>),
            typeof(IDurableSenderProviderConfigurator),
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
        Assert.Same(ProductAssemblyFacts.Core, typeof(DurableSenderOptions<>).Assembly);
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
            "ViciOne.ServiceBus.Providers.Persistence.InMemoryDurableSendStore`1",
            "ViciOne.ServiceBus.Providers.Persistence.DurableSendAdmission`1",
            "ViciOne.ServiceBus.Providers.Persistence.TypedDurableSender`1",
            "ViciOne.ServiceBus.Providers.Persistence.DurableSenderDeliveryService`1",
            "ViciOne.ServiceBus.Operations.DurableSenderOperations`1",
            "ViciOne.ServiceBus.Providers.Persistence.DurableSendConsumerCompletion`1",
            "ViciOne.ServiceBus.Configuration.DurableSenderConfigurator`1",
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
        string sender = Source("src/ViciOne.ServiceBus/DurableSend/DurableSender.cs");
        int catalogLookup = sender.IndexOf("_contractCatalog.TryGetMessageType", StringComparison.Ordinal);
        int storeAdmission = sender.IndexOf("_store\n                .AdmitAsync", StringComparison.Ordinal);
        Assert.True(catalogLookup >= 0);
        Assert.True(storeAdmission > catalogLookup);

        string efStore = Source(
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/DurableSend/EntityFrameworkDurableSendStore.cs");
        int preflight = efStore.IndexOf("_commitDurabilityValidator.ValidateAsync", StringComparison.Ordinal);
        int capacityLookup = efStore.IndexOf("Set<DurableSendCapacityState>()", preflight, StringComparison.Ordinal);
        Assert.True(preflight >= 0);
        Assert.True(capacityLookup > preflight);

        string validator = Source(
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/DurableSend/EntityFrameworkDurableSendCommitDurabilityValidator.cs");
        Assert.Contains("Microsoft.EntityFrameworkCore.SqlServer", validator, StringComparison.Ordinal);
        Assert.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", validator, StringComparison.Ordinal);
        Assert.Contains("Microsoft.EntityFrameworkCore.Sqlite", validator, StringComparison.Ordinal);
        Assert.Contains("has no built-in Durable Sender synchronous-commit validator", validator, StringComparison.Ordinal);
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));

}
