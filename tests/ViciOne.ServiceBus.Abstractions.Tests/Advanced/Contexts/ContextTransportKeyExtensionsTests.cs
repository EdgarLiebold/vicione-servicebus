using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Contexts;

public sealed class ContextTransportKeyExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-CONTEXT-KEYS", "consume-and-send-capability-lookup")]
    public void Getters_ReturnTheExactTransportCapabilityValuesOrNull()
    {
        var consumePartition = new PartitionCapability("consume-partition");
        var consumeRouting = new RoutingCapability("consume-route");
        ConsumeContext consumeContext = CreateContext<ConsumeContext>(consumePartition, consumeRouting);
        var sendPartition = new PartitionCapability("send-partition");
        var sendRouting = new RoutingCapability("send-route");
        SendContext sendContext = CreateContext<SendContext>(sendPartition, sendRouting);

        Assert.Equal("consume-partition", consumeContext.GetPartitionKey());
        Assert.Equal("consume-route", consumeContext.GetRoutingKey());
        Assert.Equal("send-partition", sendContext.GetPartitionKey());
        Assert.Equal("send-route", sendContext.GetRoutingKey());
        Assert.Null(CreateContext<ConsumeContext>().GetPartitionKey());
        Assert.Null(CreateContext<ConsumeContext>().GetRoutingKey());
        Assert.Null(CreateContext<SendContext>().GetPartitionKey());
        Assert.Null(CreateContext<SendContext>().GetRoutingKey());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-CONTEXT-KEYS", "send-capability-mutation-and-absence")]
    public void Setters_MutateOnlyTheMatchingCapabilityAndReportAbsencePrecisely()
    {
        var partition = new PartitionCapability("original-partition");
        var routing = new RoutingCapability("original-route");
        SendContext context = CreateContext<SendContext>(partition, routing);

        context.SetPartitionKey("updated-partition");
        context.SetRoutingKey("updated-route");

        Assert.Equal("updated-partition", partition.PartitionKey);
        Assert.Equal("updated-route", routing.RoutingKey);
        Assert.True(context.TrySetPartitionKey(null));
        Assert.True(context.TrySetRoutingKey(null));
        Assert.Null(partition.PartitionKey);
        Assert.Null(routing.RoutingKey);

        SendContext unsupported = CreateContext<SendContext>();
        Assert.False(unsupported.TrySetPartitionKey("ignored"));
        Assert.False(unsupported.TrySetRoutingKey("ignored"));
        Assert.Throws<NotSupportedException>(() => unsupported.SetPartitionKey("rejected"));
        Assert.Throws<NotSupportedException>(() => unsupported.SetRoutingKey("rejected"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-CONTEXT-KEYS", "receiver-boundaries-and-method-names")]
    public void Contract_RejectsNullReceiversAndUsesVerbShapedGetterNames()
    {
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => PartitionKeyExtensions.GetPartitionKey((ConsumeContext)null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => PartitionKeyExtensions.GetPartitionKey((SendContext)null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => RoutingKeyExtensions.GetRoutingKey((ConsumeContext)null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => RoutingKeyExtensions.GetRoutingKey((SendContext)null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => PartitionKeyExtensions.SetPartitionKey(null!, "value")).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => PartitionKeyExtensions.TrySetPartitionKey(null!, "value")).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => RoutingKeyExtensions.SetRoutingKey(null!, "value")).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => RoutingKeyExtensions.TrySetRoutingKey(null!, "value")).ParamName);

        Assert.DoesNotContain(typeof(PartitionKeyExtensions).GetMethods(), method => method.Name == "PartitionKey");
        Assert.DoesNotContain(typeof(RoutingKeyExtensions).GetMethods(), method => method.Name == "RoutingKey");
    }

    private static TContext CreateContext<TContext>(params object[] payloads)
        where TContext : class
    {
        TContext context = DispatchProxy.Create<TContext, PayloadContextProxy>();
        ((PayloadContextProxy)(object)context).SetPayloads(payloads);
        return context;
    }

    private sealed class PartitionCapability(string? value) :
        PartitionKeyConsumeContext,
        PartitionKeySendContext
    {
        public string? PartitionKey { get; set; } = value;
    }

    private sealed class RoutingCapability(string? value) :
        RoutingKeyConsumeContext,
        RoutingKeySendContext
    {
        public string? RoutingKey { get; set; } = value;
    }

    private class PayloadContextProxy : DispatchProxy
    {
        private object[] _payloads = [];

        public void SetPayloads(object[] payloads) => _payloads = payloads;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "TryGetPayload")
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                object? payload = _payloads.SingleOrDefault(payloadType.IsInstanceOfType);
                args![0] = payload;
                return payload is not null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
