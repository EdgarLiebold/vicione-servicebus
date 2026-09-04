using System.Net.Sockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests;

public sealed class RabbitMqConnectionExceptionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "broker-failure-classification")]
    public void BrokerFailures_AreRetryableOnlyWhenWaitingCanChangeTheOutcome()
    {
        var exclusive = new RabbitMqConnectionException(
            "declare failed",
            ChannelClosed(405, "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue"));
        var wrappedExclusive = new RabbitMqConnectionException(
            "wrapped declare failed",
            new InvalidOperationException(
                "pipe failed",
                ChannelClosed(405, "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue")));
        var alreadyClosedExclusive = new RabbitMqConnectionException(
            "channel already closed",
            new AlreadyClosedException(new ShutdownEventArgs(
                ShutdownInitiator.Peer,
                405,
                "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue")));
        var precondition = new RabbitMqConnectionException(
            "channel closed",
            ChannelClosed(406, "PRECONDITION_FAILED - delivery acknowledgement timed out"));
        var unavailable = new RabbitMqConnectionException(
            "broker unreachable",
            new BrokerUnreachableException(new SocketException(10061)));
        var refusedCredential = new RabbitMqConnectionException(
            "broker unreachable",
            new BrokerUnreachableException(new AuthenticationFailureException("ACCESS_REFUSED")));
        var dropped = new RabbitMqConnectionException("connection dropped", new EndOfStreamException());
        var stopping = RabbitMqConnectionException.Stopping("rabbitmq://broker/test");
        var publicMessage = new RabbitMqConnectionException(
            "The connection is stopping and cannot be used: rabbitmq://broker/test");

        Assert.False(exclusive.IsTransient);
        Assert.False(wrappedExclusive.IsTransient);
        Assert.False(alreadyClosedExclusive.IsTransient);
        Assert.True(precondition.IsTransient);
        Assert.True(unavailable.IsTransient);
        Assert.False(refusedCredential.IsTransient);
        Assert.True(dropped.IsTransient);
        Assert.True(stopping.IsTransient);
        Assert.False(publicMessage.IsTransient);
    }

    private static OperationInterruptedException ChannelClosed(ushort replyCode, string replyText) =>
        new(new ShutdownEventArgs(ShutdownInitiator.Peer, replyCode, replyText));
}
