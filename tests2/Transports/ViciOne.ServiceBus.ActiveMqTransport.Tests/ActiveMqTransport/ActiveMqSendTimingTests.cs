using System.Reflection;
using Apache.NMS;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Serialization;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.ActiveMqTransport;

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

        long delivery = ActiveMqSendTransportContext.GetArtemisScheduledDelivery(context);

        Assert.Equal(now.AddMinutes(3).ToUnixTimeMilliseconds(), delivery);
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
                if (_properties.TryGetValue(propertyName, out object? value))
                    return value;

                return targetMethod.ReturnType.IsValueType
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
