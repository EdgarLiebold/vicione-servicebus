using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class MessageFabricTopologyBuilderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "consume-builder-forwards-complete-topology")]
    public async Task ConsumeBuilder_ForwardsEveryTopologyOperationToItsFabricAsync()
    {
        var fabric = new MessageFabric<BuilderMessage>();

        try
        {
            var builder = new MessageFabricConsumeTopologyBuilder<BuilderMessage>(fabric, "endpoint", "endpoint");
            builder.ExchangeDeclare(builder.Exchange, InMemoryExchangeType.FanOut);
            builder.QueueDeclare(builder.Queue);
            builder.ExchangeDeclare("source", InMemoryExchangeType.Direct);
            builder.ExchangeBind("source", builder.Exchange, "tenant-a");
            builder.QueueBind(builder.Exchange, builder.Queue);

            IMessageExchange<BuilderMessage> source = fabric.GetExchange("source", InMemoryExchangeType.Direct);
            IMessageExchange<BuilderMessage> endpoint = fabric.GetExchange("endpoint", InMemoryExchangeType.FanOut);

            Assert.Equal("endpoint", builder.Exchange);
            Assert.Equal("endpoint", builder.Queue);
            Assert.Same(endpoint, Assert.Single(source.Sinks));
            Assert.Same(fabric.GetQueue("endpoint"), Assert.Single(endpoint.Sinks));
            Assert.Equal("fabric", Assert.Throws<ArgumentNullException>(() =>
                new MessageFabricConsumeTopologyBuilder<BuilderMessage>(null!, "exchange", "queue")).ParamName);
            Assert.Equal("exchange", Assert.Throws<ArgumentException>(() =>
                new MessageFabricConsumeTopologyBuilder<BuilderMessage>(fabric, " ", "queue")).ParamName);
            Assert.Equal("queue", Assert.Throws<ArgumentException>(() =>
                new MessageFabricConsumeTopologyBuilder<BuilderMessage>(fabric, "exchange", " ")).ParamName);
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "publish-builder-composes-implemented-contracts")]
    public async Task PublishBuilder_ComposesNestedImplementedContractTopologyAsync()
    {
        var fabric = new MessageFabric<BuilderMessage>();

        try
        {
            var builder = new MessageFabricPublishTopologyBuilder<BuilderMessage>(fabric);
            builder.ExchangeDeclare("contract", InMemoryExchangeType.Topic);
            builder.ExchangeName = "contract";
            builder.ExchangeType = InMemoryExchangeType.Topic;

            IMessageFabricPublishTopologyBuilder implemented = builder.CreateImplementedBuilder();
            implemented.ExchangeDeclare("implemented", InMemoryExchangeType.Direct);
            implemented.ExchangeName = "implemented";
            implemented.ExchangeType = InMemoryExchangeType.Direct;
            implemented.QueueDeclare("implemented-queue");
            implemented.QueueBind("implemented", "implemented-queue");

            IMessageFabricPublishTopologyBuilder nested = implemented.CreateImplementedBuilder();
            nested.ExchangeDeclare("nested", InMemoryExchangeType.FanOut);
            nested.ExchangeName = "nested";
            nested.ExchangeBind("nested", "nested-destination", null);
            nested.QueueDeclare("nested-queue");
            nested.QueueBind("nested", "nested-queue");

            IMessageExchange<BuilderMessage> contract = fabric.GetExchange("contract", InMemoryExchangeType.Topic);
            IMessageExchange<BuilderMessage> implementedExchange = fabric.GetExchange("implemented", InMemoryExchangeType.Direct);
            IMessageExchange<BuilderMessage> nestedExchange = fabric.GetExchange("nested", InMemoryExchangeType.FanOut);

            Assert.Equal("contract", builder.ExchangeName);
            Assert.Equal(InMemoryExchangeType.Topic, builder.ExchangeType);
            Assert.Contains(implementedExchange, contract.Sinks);
            Assert.Contains(nestedExchange, implementedExchange.Sinks);
            Assert.Equal(2, nestedExchange.Sinks.Count());
            Assert.Equal("messageFabric", Assert.Throws<ArgumentNullException>(() =>
                new MessageFabricPublishTopologyBuilder<BuilderMessage>(null!)).ParamName);
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None);
        }
    }

    private sealed record BuilderMessage;
}
