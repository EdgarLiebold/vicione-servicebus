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
        AssertGuardedSqlProvider("src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/ViciOne.ServiceBus.SqlTransport.PostgreSql.csproj");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "sql-server")]
    public void SqlServerReceivePath_UsesTheGuardedSharedSqlEnvelope() =>
        AssertGuardedSqlProvider("src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/ViciOne.ServiceBus.SqlTransport.SqlServer.csproj");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "in-memory")]
    public void InMemoryReceivePath_RejectsOversizedWireBodiesBeforeDeserialization() =>
        AssertGuardedTransportBody("src/ViciOne.ServiceBus/InMemoryTransport/InMemoryReceiveContext.cs");

    [Fact]
    [RequirementCoverage("REQ-VSB-INBOUND-MESSAGE-LIMITS", "mediator")]
    public void MediatorReceivePath_RejectsOversizedBodiesBeforeDispatch()
    {
        string sendEndpoint = Read("src/ViciOne.ServiceBus.Mediator/Contexts/MediatorSendEndpoint.cs");
        int measure = sendEndpoint.IndexOf("MediatorMessageBodySizer.MeasureAsync", StringComparison.Ordinal);
        int dispatch = sendEndpoint.IndexOf("_dispatcher.DispatchAsync", StringComparison.Ordinal);
        Assert.True(measure >= 0 && dispatch > measure,
            "Mediator body measurement must complete before the receive dispatcher can observe the message.");

        string sizer = Read("src/ViciOne.ServiceBus.Mediator/Contexts/MediatorMessageBodySizer.cs");
        Assert.Contains("new MessageTooLargeException(actualBytes, _maximumBytes, _inputAddress)", sizer, StringComparison.Ordinal);
    }

    static void AssertGuardedSqlProvider(string providerProject)
    {
        string project = Read(providerProject);
        Assert.Contains("ViciOne.ServiceBus.SqlTransport.csproj", project, StringComparison.Ordinal);
        AssertGuardedTransportBody("src/Transports/ViciOne.ServiceBus.SqlTransport/SqlReceiveContext.cs");
    }

    static void AssertGuardedTransportBody(string receiveContextPath)
    {
        string receiveContext = Read(receiveContextPath);
        Assert.Contains("MessageBody Body => EnforceMessageLimits(_body)", receiveContext, StringComparison.Ordinal);

        string deserializeFilter = Read("src/ViciOne.ServiceBus/Middleware/DeserializeFilter.cs");
        int bodyAdmission = deserializeFilter.IndexOf("context.Body.Length", StringComparison.Ordinal);
        int deserialize = deserializeFilter.IndexOf(".Deserialize(context)", StringComparison.Ordinal);
        Assert.True(bodyAdmission >= 0 && deserialize > bodyAdmission,
            "The common receive pipeline must inspect the guarded body before invoking a deserializer.");
    }

    static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));
}
