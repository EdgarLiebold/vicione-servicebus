using System.Reflection;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.DurableSend;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class DurableSenderArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ARCHITECTURE", "public-spi-is-provider-neutral-abstraction")]
    public void PublicStateAndPersistenceSpi_StayInAbstractionsWithoutClrTypePersistence()
    {
        Type[] contracts =
        [
            typeof(DurableSendId),
            typeof(SerializedDurableSend),
            typeof(DurableSendDelivery),
            typeof(DurableSendStoreSnapshot),
            typeof(IDurableSender<>),
            typeof(IDurableSendStore<>),
            typeof(IDurableSendDispatcher<>),
            typeof(IDurableSendConsumerCompletion),
            typeof(IDurableSenderOperations<>),
        ];

        Assert.All(contracts, contract =>
        {
            Assert.True(contract.IsPublic);
            Assert.Same(ProductAssemblyFacts.Abstractions, contract.Assembly);
        });
        Assert.Same(ProductAssemblyFacts.Core, typeof(DurableSenderOptions<>).Assembly);
        Assert.DoesNotContain(typeof(SerializedDurableSend).GetProperties(), property => property.PropertyType == typeof(Type));
        Assert.DoesNotContain("AssemblyQualifiedName", Source(
            "src/ViciOne.ServiceBus.Abstractions/DurableSend/SerializedDurableSend.cs"),
            StringComparison.Ordinal);
        Assert.DoesNotContain(ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Abstractions),
            name => name.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ARCHITECTURE", "runtime-state-machines-remain-internal")]
    public void RuntimeStateMachines_AreInternalSealedAndReachedOnlyThroughPublicContracts()
    {
        string[] implementationNames =
        [
            "ViciOne.ServiceBus.DurableSend.InMemoryDurableSendStore`1",
            "ViciOne.ServiceBus.DurableSend.DurableSender`1",
            "ViciOne.ServiceBus.DurableSend.DurableSenderDeliveryService`1",
            "ViciOne.ServiceBus.DurableSend.DurableSenderOperations`1",
            "ViciOne.ServiceBus.DurableSend.DurableSendConsumerCompletion`1",
            "ViciOne.ServiceBus.InMemoryTransport.InMemoryDurableSendDispatcher`1",
            "ViciOne.ServiceBus.InMemoryTransport.InMemoryDurableSendCompletionFilter",
            "ViciOne.ServiceBus.InMemoryTransport.InMemoryDurableSendContext",
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
        int pipeline = filter.IndexOf("await next.Send(context)", StringComparison.Ordinal);
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
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/DurableSend/EntityFrameworkDurableSendStore.cs");
        int preflight = efStore.IndexOf("_commitDurabilityValidator.ValidateAsync", StringComparison.Ordinal);
        int capacityLookup = efStore.IndexOf("Set<DurableSendCapacityState>()", preflight, StringComparison.Ordinal);
        Assert.True(preflight >= 0);
        Assert.True(capacityLookup > preflight);

        string validator = Source(
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/DurableSend/EntityFrameworkDurableSendCommitDurabilityValidator.cs");
        Assert.Contains("Microsoft.EntityFrameworkCore.SqlServer", validator, StringComparison.Ordinal);
        Assert.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", validator, StringComparison.Ordinal);
        Assert.Contains("Microsoft.EntityFrameworkCore.Sqlite", validator, StringComparison.Ordinal);
        Assert.Contains("has no built-in Durable Sender synchronous-commit validator", validator, StringComparison.Ordinal);
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));
}
