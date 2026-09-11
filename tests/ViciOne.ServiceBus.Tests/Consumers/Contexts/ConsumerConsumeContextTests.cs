using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Contexts;

public sealed class ConsumerConsumeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONTEXT", "proxy-preserves-message-consumer-and-source-payloads")]
    public void Proxy_ExposesTheConsumerAndPreservesTheSourceContext()
    {
        ConsumeContext<ConsumerMessage> source = CreateContext();
        var inheritedPayload = new InheritedPayload("source");
        source.AddOrUpdatePayload(() => inheritedPayload, _ => inheritedPayload);
        var consumer = new TestConsumer();

        var context = new ConsumerConsumeContextProxy<TestConsumer, ConsumerMessage>(source, consumer);

        Assert.Same(consumer, context.Consumer);
        Assert.Same(source.Message, context.Message);
        Assert.True(context.TryGetPayload(out InheritedPayload? selected));
        Assert.Same(inheritedPayload, selected);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new ConsumerConsumeContextProxy<TestConsumer, ConsumerMessage>(null!, consumer)).ParamName);
        Assert.Equal(
            "consumer",
            Assert.Throws<ArgumentNullException>(() => new ConsumerConsumeContextProxy<TestConsumer, ConsumerMessage>(source, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONTEXT", "scope-isolates-local-payload-and-validates-inputs")]
    public void Scope_AddsLocalPayloadsWithoutMutatingTheSourceContext()
    {
        ConsumeContext<ConsumerMessage> source = CreateContext();
        var consumer = new TestConsumer();
        var localPayload = new LocalPayload("local");

        var context = new ConsumerConsumeContextScope<TestConsumer, ConsumerMessage>(source, consumer, localPayload);

        Assert.Same(consumer, context.Consumer);
        Assert.Same(source.Message, context.Message);
        Assert.True(context.TryGetPayload(out LocalPayload? selected));
        Assert.Same(localPayload, selected);
        Assert.False(source.TryGetPayload(out LocalPayload? missing));
        Assert.Null(missing);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new ConsumerConsumeContextScope<TestConsumer, ConsumerMessage>(null!, consumer)).ParamName);
        Assert.Equal(
            "consumer",
            Assert.Throws<ArgumentNullException>(() => new ConsumerConsumeContextScope<TestConsumer, ConsumerMessage>(source, null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentNullException>(() =>
                new ConsumerConsumeContextScope<TestConsumer, ConsumerMessage>(source, consumer, null!)).ParamName);
    }

    private static ConsumeContext<ConsumerMessage> CreateContext() =>
        InMemoryOutboxTestContextFactory.Create(new ConsumerMessage("value"));

    public sealed record ConsumerMessage(string Value);

    private sealed class TestConsumer
    {
    }

    private sealed record InheritedPayload(string Value);

    private sealed record LocalPayload(string Value);
}
