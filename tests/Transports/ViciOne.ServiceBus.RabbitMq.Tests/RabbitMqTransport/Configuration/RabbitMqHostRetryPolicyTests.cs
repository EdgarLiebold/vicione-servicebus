using System.IO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqHostRetryPolicyTests
{
    [Theory]
    [InlineData(200, false, false)]
    [InlineData(299, false, false)]
    [InlineData(300, true, true)]
    [InlineData(404, true, true)]
    [InlineData(405, true, false)]
    [InlineData(406, true, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "amqp-channel-close-and-retry-boundaries")]
    public void BrokerReplyCode_DeterminesChannelClosureAndReceiveRetry(ushort replyCode, bool closesChannel, bool retries)
    {
        var interrupted = Interrupted(replyCode, "broker reply");
        var host = CreateHost();

        Assert.Equal(closesChannel, interrupted.ChannelShouldBeClosed());
        Assert.Equal(retries, host.ReceiveTransportRetryPolicy.IsHandled(interrupted));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "nested-exclusive-conflict-remains-terminal")]
    public void NestedResourceLock_StopsRetriesAcrossConnectionAndClosedChannelCarriers()
    {
        var host = CreateHost();
        var locked = Interrupted(405, "queue owned by another connection");
        Exception nested = new InvalidOperationException("outer", locked);

        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(new RabbitMqConnectionException("declare", nested)));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(new AlreadyClosedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 405, "queue owned by another connection"))));
        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(new RabbitMqConnectionException(
            "declare", Interrupted(406, "precondition failed"))));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "broker-reply-code-overrides-reply-text")]
    public void ReplyText_CannotMakeResourceLockRetryableOrARecoverableReplyTerminal()
    {
        var host = CreateHost();

        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(
            Interrupted(405, "temporary error; retry later")));
        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(
            Interrupted(406, "RESOURCE_LOCKED")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "terminal-authentication-and-configuration")]
    public void AuthenticationAndConfigurationFailures_DoNotRetryEvenWhenWrappingAHandledCause()
    {
        var host = CreateHost();

        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(
            new AuthenticationFailureException("credentials refused")));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(
            new RabbitMqConnectionException("connect", new AuthenticationFailureException("credentials refused"))));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(
            new ConfigurationException("invalid queue", Interrupted(406, "precondition failed"))));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "stream-drop-and-selective-pipelining-retry")]
    public void StreamDropAndPipeliningConflict_RetryWithoutRetryingUnrelatedUnsupportedFeatures()
    {
        var host = CreateHost();

        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(new EndOfStreamException("broker disconnected")));
        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(
            new NotSupportedException("Pipelining of requests forbidden")));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(
            new NotSupportedException("unsupported exchange type")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "connection-transience-controls-host-retry")]
    public void ConnectionFailures_RetryOnlyWhenExplicitlyTransientAndWithoutResourceLock()
    {
        var host = CreateHost();

        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(new ConnectionException("permanent configuration")));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(new RabbitMqConnectionException("permanent provider failure")));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(new ConnectionException(
            "permanent configuration", new EndOfStreamException("secondary socket closure"), false)));
        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(new ConnectionException("socket lost", true)));
        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(new RabbitMqConnectionException(
            "broker unavailable", new EndOfStreamException("stream lost"))));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(new RabbitMqConnectionException(
            "exclusive queue", Interrupted(405, "RESOURCE_LOCKED"))));
        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(new ConnectionException(
            "exclusive queue", Interrupted(405, "RESOURCE_LOCKED"), true)));
    }

    private static IRabbitMqHostConfiguration CreateHost()
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        return new RabbitMqBusConfiguration(topology).HostConfiguration;
    }

    private static OperationInterruptedException Interrupted(ushort replyCode, string text) =>
        new(new ShutdownEventArgs(ShutdownInitiator.Peer, replyCode, text));
}
