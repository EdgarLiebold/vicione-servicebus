using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqStreamConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "subsecond-max-age-removes-invalid-argument")]
    public void MaxAgeBelowBrokerGranularity_RemovesAnExistingQueueArgument()
    {
        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-max-age", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.MaxAge = TimeSpan.FromDays(14);
                stream.MaxAge = TimeSpan.FromMilliseconds(999);
            });
            settings = Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(
                Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
        }));

        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings actual =
            Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(settings);
        Assert.False(actual.QueueArguments.ContainsKey("x-max-age"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "stream-settings-project-before-provider-start")]
    public void Stream_ProjectsTheExactQueueAndConsumerArgumentsBeforeTheBrokerStarts()
    {
        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-settings", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.MaxAge = TimeSpan.FromDays(14);
                stream.FromFirst();
            });
            settings = Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(
                Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
        }));

        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings actual =
            Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(settings);
        Assert.Equal("stream", actual.QueueArguments["x-queue-type"]);
        Assert.Equal("14D", actual.QueueArguments["x-max-age"]);
        Assert.Equal("first", actual.ConsumeArguments["x-stream-offset"]);
        Assert.Equal("native-reader", actual.ConsumerTag);
    }
}
