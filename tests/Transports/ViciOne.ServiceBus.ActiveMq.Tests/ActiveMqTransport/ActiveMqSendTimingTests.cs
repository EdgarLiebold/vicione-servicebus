using System.Reflection;
using Apache.NMS;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Serialization;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqSendTimingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "artemis-delay-uses-context-time-provider")]
    public void ArtemisScheduledDelivery_UsesTheContextTimeProvider()
    {
        DateTimeOffset now = new(2031, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        TransportActiveMqSendContext<Message> context = CreateContext();
        context.SetTimeProvider(timeProvider);
        context.Delay = TimeSpan.FromMinutes(3);
        IMessage message = DispatchProxy.Create<IMessage, MessageProxy>();

        ActiveMqSendTransportContext.ApplyDeliveryDelay(message, context, isArtemis: true);

        Assert.Equal(now.AddMinutes(3).UtcDateTime, message.NMSDeliveryTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "classic-delay-uses-provider-property")]
    public void ClassicScheduledDelivery_UsesTheExactProviderDelayProperty()
    {
        TransportActiveMqSendContext<Message> context = CreateContext();
        context.Delay = TimeSpan.FromMilliseconds(2750);
        IMessage message = DispatchProxy.Create<IMessage, MessageProxy>();

        ActiveMqSendTransportContext.ApplyDeliveryDelay(message, context, isArtemis: false);

        Assert.Equal(2750L, message.Properties["AMQ_SCHEDULED_DELAY"]);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "nonpositive-delay-does-not-write-provider-state")]
    public void AbsentOrNonPositiveDelay_DoesNotWriteProviderState(int? milliseconds, bool isArtemis)
    {
        DateTime sentinel = new(2040, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        const long classicSentinel = 73;
        TransportActiveMqSendContext<Message> context = CreateContext();
        context.Delay = milliseconds.HasValue ? TimeSpan.FromMilliseconds(milliseconds.Value) : null;
        IMessage message = DispatchProxy.Create<IMessage, MessageProxy>();
        message.NMSDeliveryTime = sentinel;
        message.Properties["AMQ_SCHEDULED_DELAY"] = classicSentinel;

        ActiveMqSendTransportContext.ApplyDeliveryDelay(message, context, isArtemis);

        Assert.Equal(sentinel, message.NMSDeliveryTime);
        Assert.Equal(classicSentinel, message.Properties["AMQ_SCHEDULED_DELAY"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "positive-ttl-preserved-exactly")]
    public void PositiveTimeToLive_IsAppliedWithoutClamping()
    {
        TransportActiveMqSendContext<Message> context = CreateContext();
        context.TimeToLive = TimeSpan.FromMilliseconds(2750);
        IMessage message = DispatchProxy.Create<IMessage, MessageProxy>();

        ActiveMqSendTransportContext.ApplyTimeToLive(message, context, useOpenWireDefault: true);

        Assert.Equal(TimeSpan.FromMilliseconds(2750), message.NMSTimeToLive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "absent-ttl-retains-openwire-default")]
    public void MissingTimeToLive_UsesOnlyTheOpenWireNoExpiryDefault()
    {
        TransportActiveMqSendContext<Message> context = CreateContext();
        IMessage openWireMessage = DispatchProxy.Create<IMessage, MessageProxy>();
        IMessage amqpMessage = DispatchProxy.Create<IMessage, MessageProxy>();
        amqpMessage.NMSTimeToLive = TimeSpan.FromSeconds(9);

        ActiveMqSendTransportContext.ApplyTimeToLive(openWireMessage, context, useOpenWireDefault: true);
        ActiveMqSendTransportContext.ApplyTimeToLive(amqpMessage, context, useOpenWireDefault: false);

        Assert.Equal(NMSConstants.defaultTimeToLive, openWireMessage.NMSTimeToLive);
        Assert.Equal(TimeSpan.FromSeconds(9), amqpMessage.NMSTimeToLive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "expired-ttl-cannot-be-serialized")]
    public void ExpiredTimeToLive_CannotReachTransportSerialization(int milliseconds)
    {
        TransportActiveMqSendContext<Message> context = CreateContext();
        context.TimeToLive = TimeSpan.FromMilliseconds(milliseconds);
        IMessage message = DispatchProxy.Create<IMessage, MessageProxy>();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ActiveMqSendTransportContext.ApplyTimeToLive(message, context, useOpenWireDefault: true));

        Assert.Contains("discarded", exception.Message, StringComparison.Ordinal);
        Assert.Equal(default, message.NMSTimeToLive);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "generic-expired-send-marked-before-transport")]
    public void GenericTimeToLive_IsMarkedExpiredBeforeTransport(int milliseconds, bool expectedExpired)
    {
        TransportActiveMqSendContext<Message> context = CreateContext();
        context.TimeToLive = TimeSpan.FromMilliseconds(milliseconds);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2031, 4, 5, 6, 7, 8, TimeSpan.Zero));

        bool expired = ForwardingExpirationTestDriver.MarkIfExpired(context, inheritedExpirationTime: null, timeProvider);

        Assert.Equal(expectedExpired, expired);
        Assert.Equal(expectedExpired, ForwardingExpirationTestDriver.IsMarkedExpired(context));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TIME", "inherited-expiry-uses-explicit-time-provider")]
    public void InheritedExpiration_UsesTheExplicitTimeProvider(int offsetSeconds, bool expectedExpired)
    {
        DateTimeOffset now = new(2031, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        TransportActiveMqSendContext<Message> context = CreateContext();

        bool expired = ForwardingExpirationTestDriver.MarkIfExpired(
            context,
            now.AddSeconds(offsetSeconds).UtcDateTime,
            timeProvider);

        Assert.Equal(expectedExpired, expired);
        Assert.Equal(expectedExpired, ForwardingExpirationTestDriver.IsMarkedExpired(context));
    }

    private static TransportActiveMqSendContext<Message> CreateContext() =>
        new(new Message(), TestContext.Current.CancellationToken);

    private sealed record Message;

    private class MessageProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _properties = new(StringComparer.Ordinal);
        private readonly IPrimitiveMap _primitiveMap = DispatchProxy.Create<IPrimitiveMap, PrimitiveMapProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                _properties[targetMethod.Name[4..]] = args![0];
                return null;
            }

            if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal))
            {
                string propertyName = targetMethod.Name[4..];
                if (propertyName == nameof(IMessage.Properties))
                    return _primitiveMap;
                if (_properties.TryGetValue(propertyName, out object? value))
                    return value;

                return targetMethod.ReturnType.IsValueType
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class PrimitiveMapProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "set_Item")
            {
                _values[Assert.IsType<string>(args![0])] = args[1];
                return null;
            }

            if (targetMethod.Name == "get_Item")
                return _values[Assert.IsType<string>(args![0])];

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
