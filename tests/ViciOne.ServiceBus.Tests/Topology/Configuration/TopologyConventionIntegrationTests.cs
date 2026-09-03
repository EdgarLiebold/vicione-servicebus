using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class TopologyConventionIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TOPOLOGY", "correlation-selector-syntax")]
    public async Task CorrelationSelectors_ApplyToInterfacePropertyAndBusOwnedMessageContracts()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CreateHarness(timeout, "correlation");
        HandlerTestHarness<NewUserEvent> interfaceHandler = harness.Handler<NewUserEvent>();
        HandlerTestHarness<OtherMessage> propertyHandler = harness.Handler<OtherMessage>();
        HandlerTestHarness<ExplicitCorrelationMessage> globalHandler = harness.Handler<ExplicitCorrelationMessage>();
        harness.OnConfigureInMemoryBus += configurator =>
        {
            configurator.Send<NewUserEvent>(topology =>
                topology.UseCorrelationId(message => message.TransactionId));
            configurator.Send<ExplicitCorrelationMessage>(topology =>
                topology.UseCorrelationId(message => message.TransactionId));
        };
        Guid interfaceId = NewId.NextGuid();
        Guid propertyId = NewId.NextGuid();
        Guid globalId = NewId.NextGuid();

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                    new NewUserEvent(interfaceId),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(
                    new OtherMessage(propertyId),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(
                    new ExplicitCorrelationMessage(globalId),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            ConsumeContext<NewUserEvent> interfaceContext =
                (await interfaceHandler.Consumed.SelectAsync(cancellationToken).First()).Context;
            ConsumeContext<OtherMessage> propertyContext =
                (await propertyHandler.Consumed.SelectAsync(cancellationToken).First()).Context;
            ConsumeContext<ExplicitCorrelationMessage> globalContext =
                (await globalHandler.Consumed.SelectAsync(cancellationToken).First()).Context;

            Assert.Equal(interfaceId, interfaceContext.CorrelationId);
            Assert.Equal(propertyId, propertyContext.CorrelationId);
            Assert.Equal(globalId, globalContext.CorrelationId);
            Assert.Equal(interfaceId, interfaceContext.Message.TransactionId);
            Assert.Equal(propertyId, propertyContext.Message.CorrelationId);
            Assert.Equal(globalId, globalContext.Message.TransactionId);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TOPOLOGY", "message-specific-serializer")]
    public async Task MessageSerializerConvention_UsesRawJsonForTheConfiguredContract()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CreateHarness(timeout, "serializer");
        HandlerTestHarness<JsonMessage> handler = harness.Handler<JsonMessage>();
        harness.OnConfigureInMemoryBus += configurator =>
        {
            configurator.AddRawJsonSerializer();
            configurator.Send<JsonMessage>(topology =>
                topology.UseSerializer(SystemTextJsonRawMessageSerializer.JsonContentType));
        };
        var message = new JsonMessage("Frank");

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.Bus.Publish(message, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            ConsumeContext<JsonMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).First()).Context;

            Assert.Equal("Frank", context.Message.Value);
            Assert.Equal(SystemTextJsonRawMessageSerializer.JsonContentType, context.ReceiveContext.ContentType);
            Assert.Equal("application/json", context.ReceiveContext.ContentType.MediaType);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout, string purpose) =>
        new($"topology-{purpose}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    public sealed record NewUserEvent(Guid TransactionId);

    public sealed record OtherMessage(Guid CorrelationId);

    public sealed record ExplicitCorrelationMessage(Guid TransactionId);

    public sealed record JsonMessage(string Value);
}
