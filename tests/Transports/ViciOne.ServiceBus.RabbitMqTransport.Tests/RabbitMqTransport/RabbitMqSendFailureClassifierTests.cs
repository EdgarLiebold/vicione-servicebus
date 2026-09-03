using System.Net.Sockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport;

public sealed class RabbitMqSendFailureClassifierTests
{
    [Theory]
    [InlineData(311)]
    [InlineData(403)]
    [InlineData(405)]
    [InlineData(530)]
    [InlineData(540)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-FAILURE", "permanent-amqp-reply-codes")]
    public void PermanentReplyCodes_AreClassifiedFromStructuredBrokerData(ushort replyCode)
    {
        var classifier = new RabbitMqSendFailureClassifier();

        bool classified = classifier.TryClassify(
            Interrupted(replyCode, "text deliberately carries no policy"),
            out TransportSendFailureKind kind);

        Assert.True(classified);
        Assert.Equal(TransportSendFailureKind.Permanent, kind);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-FAILURE", "transient-typed-provider-failures")]
    public void RecoverableTypedFailures_AreTransientIncludingNestedFailures()
    {
        var classifier = new RabbitMqSendFailureClassifier();
        Exception[] failures =
        [
            Interrupted(404, "NOT_FOUND"),
            Interrupted(406, "PRECONDITION_FAILED"),
            new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "connection forced")),
            new BrokerUnreachableException(new SocketException(10061)),
            new InvalidOperationException("outer", Interrupted(541, "internal error")),
        ];

        foreach (Exception failure in failures)
        {
            Assert.True(classifier.TryClassify(failure, out TransportSendFailureKind kind));
            Assert.Equal(TransportSendFailureKind.Transient, kind);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-FAILURE", "reply-text-is-never-policy")]
    public void ReplyText_CannotChangeTheReplyCodeVerdict()
    {
        var classifier = new RabbitMqSendFailureClassifier();

        Assert.True(classifier.TryClassify(
            Interrupted(404, "ACCESS_REFUSED RESOURCE_LOCKED NOT_ALLOWED"),
            out TransportSendFailureKind transient));
        Assert.True(classifier.TryClassify(
            Interrupted(403, "temporary connection interruption; please retry"),
            out TransportSendFailureKind permanent));

        Assert.Equal(TransportSendFailureKind.Transient, transient);
        Assert.Equal(TransportSendFailureKind.Permanent, permanent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-FAILURE", "authentication-and-unknown-boundaries")]
    public void AuthenticationIsPermanent_WhileUnknownTypesRemainUnclassified()
    {
        var classifier = new RabbitMqSendFailureClassifier();
        var authentication = new BrokerUnreachableException(
            new InvalidOperationException("outer", new AuthenticationFailureException("denied")));

        Assert.True(classifier.TryClassify(authentication, out TransportSendFailureKind authenticationKind));
        Assert.Equal(TransportSendFailureKind.Permanent, authenticationKind);
        Assert.False(classifier.TryClassify(new IOException("unowned failure"), out TransportSendFailureKind unknownKind));
        Assert.Equal(TransportSendFailureKind.Unclassified, unknownKind);
        Assert.Throws<ArgumentNullException>(() => classifier.TryClassify(null!, out _));
    }

    private static OperationInterruptedException Interrupted(ushort replyCode, string replyText) =>
        new(new ShutdownEventArgs(ShutdownInitiator.Peer, replyCode, replyText));
}
