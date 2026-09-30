using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class InboundMessageLimitsArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "rabbitmq")]
    public void RabbitMqReceivePath_RejectsOversizedWireBodiesBeforeDeserialization() =>
        AssertGuardedTransportBody("src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/RabbitMqReceiveContext.cs");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "activemq")]
    public void ActiveMqReceivePath_RejectsOversizedWireBodiesBeforeDeserialization() =>
        AssertGuardedTransportBody("src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/ActiveMqReceiveContext.cs");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "amazon-sqs")]
    public void AmazonSqsReceivePath_RejectsOversizedWireBodiesBeforeDeserialization() =>
        AssertGuardedTransportBody("src/Transports/ViciOne.ServiceBus.AmazonSqs/AmazonSqsTransport/AmazonSqsReceiveContext.cs");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "azure-service-bus")]
    public void AzureServiceBusReceivePath_RejectsOversizedWireBodiesBeforeDeserialization() =>
        AssertGuardedTransportBody(
            "src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/Contexts/ServiceBusReceiveContext.cs");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "azure-event-hubs")]
    public void EventHubsReceivePath_RejectsOversizedWireBodiesBeforeDeserialization() =>
        AssertGuardedTransportBody("src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/EventHubReceiveContext.cs");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "postgresql")]
    public void PostgreSqlReceivePath_UsesTheGuardedSharedSqlEnvelope() =>
        AssertGuardedSqlProvider(
            "src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/ViciOne.ServiceBus.SqlTransport.PostgreSql.csproj",
            "src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/Runtime/PostgreSqlClientContext.cs",
            2);

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "sql-server")]
    public void SqlServerReceivePath_UsesTheGuardedSharedSqlEnvelope() =>
        AssertGuardedSqlProvider(
            "src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/ViciOne.ServiceBus.SqlTransport.SqlServer.csproj",
            "src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/Runtime/SqlServerClientContext.cs",
            1);

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "in-memory")]
    public void InMemoryReceivePath_RejectsOversizedWireBodiesBeforeDeserialization() =>
        AssertGuardedTransportBody("src/ViciOne.ServiceBus/InMemoryTransport/Runtime/InMemoryReceiveContext.cs", "_body.Value");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "mediator")]
    public void MediatorReceivePath_RejectsOversizedBodiesBeforeDispatch()
    {
        string sendEndpoint = Read("src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorSendEndpoint.cs");
        int serialize = sendEndpoint.IndexOf("MediatorMessageBodySerializer.SerializeAsync", StringComparison.Ordinal);
        int dispatch = sendEndpoint.IndexOf("_dispatcher.DispatchAsync", StringComparison.Ordinal);
        Assert.True(serialize >= 0 && dispatch > serialize,
            "Mediator body materialization and admission must complete before the receive dispatcher can observe the message.");

        string serializer = Read("src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorMessageBodySerializer.cs");
        Assert.Contains("new MessageTooLargeException(actualBytes, _maximumBytes, _endpointAddress)", serializer, StringComparison.Ordinal);
        Assert.DoesNotContain(".ToArray()", serializer, StringComparison.Ordinal);
    }

    static void AssertGuardedSqlProvider(string providerProject, string clientContextPath, int expectedStorageSelections)
    {
        string project = Read(providerProject);
        Assert.Contains("ViciOne.ServiceBus.SqlTransport.csproj", project, StringComparison.Ordinal);
        AssertGuardedTransportBody("src/Transports/ViciOne.ServiceBus.SqlTransport/SqlReceiveContext.cs");

        string clientContext = Read(clientContextPath);
        Assert.Equal(expectedStorageSelections,
            clientContext.Split("SqlMessageBodyStorage.Create(context)", StringSplitOptions.None).Length - 1);
        Assert.Contains("bodyStorage.Text", clientContext, StringComparison.Ordinal);
        Assert.Contains("bodyStorage.Binary", clientContext, StringComparison.Ordinal);
    }

    static void AssertGuardedTransportBody(string receiveContextPath, string bodyExpression = "_body")
    {
        string receiveContext = Read(receiveContextPath);
        Assert.Contains($"MessageBody Body => EnforceMessageLimits({bodyExpression})", receiveContext, StringComparison.Ordinal);

        string deserializeFilter = Read("src/ViciOne.ServiceBus/Middleware/DeserializeFilter.cs");
        Assert.Contains("MessageBody transportBody = context.Body", deserializeFilter, StringComparison.Ordinal);
        int bodyAdmission = deserializeFilter.IndexOf("transportBody.Length", StringComparison.Ordinal);
        int textNormalization = deserializeFilter.IndexOf("TransportTextMessageBodyNormalizer.Normalize", StringComparison.Ordinal);
        int deserialize = deserializeFilter.IndexOf(".Deserialize(context)", StringComparison.Ordinal);
        Assert.True(bodyAdmission >= 0 && textNormalization > bodyAdmission && deserialize > textNormalization,
            "The common receive pipeline must admit the native body before normalizing text or invoking a deserializer.");
    }

    static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));
}
