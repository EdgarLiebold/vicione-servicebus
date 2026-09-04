using Apache.NMS;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests;

public sealed class ActiveMqSendFailureClassifierTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-SEND-FAILURE", "typed-chain-and-permanent-precedence")]
    public void TypedFailures_UseTheCompleteExceptionChainWithPermanentPrecedence()
    {
        var classifier = new ActiveMqSendFailureClassifier();

        AssertKind(classifier, new ActiveMqConnectionException("outer", new IOException()), TransportSendFailureKind.Transient);
        AssertKind(classifier, new NMSConnectionException("typed provider connection failure"), TransportSendFailureKind.Transient);
        AssertKind(classifier, new ActiveMqConnectionException("temporary text"), TransportSendFailureKind.Permanent);
        AssertKind(classifier, new ActiveMqTransportConfigurationException("temporary text"), TransportSendFailureKind.Permanent);
        AssertKind(
            classifier,
            new ActiveMqConnectionException("outer is transient", new UnauthorizedAccessException("inner is terminal")),
            TransportSendFailureKind.Permanent);
        Assert.False(classifier.TryClassify(new IOException("connection words do not matter"), out TransportSendFailureKind unknown));
        Assert.Equal(TransportSendFailureKind.Unclassified, unknown);
        Assert.Throws<ArgumentNullException>(() => classifier.TryClassify(null!, out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-SEND-FAILURE", "registration-is-singleton-and-idempotent")]
    public void BusRegistration_AddsExactlyOneClassifierDescriptor()
    {
        var services = new ServiceCollection();

        services
            .AddViciOneServiceBus(configuration => configuration.UsingActiveMq())
            .AddViciOneServiceBus<ISecondBus>(configuration => configuration.UsingActiveMq());

        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ITransportSendFailureClassifier));
        Assert.Equal(typeof(ActiveMqSendFailureClassifier), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    static void AssertKind(ITransportSendFailureClassifier classifier, Exception exception, TransportSendFailureKind expected)
    {
        Assert.True(classifier.TryClassify(exception, out TransportSendFailureKind actual));
        Assert.Equal(expected, actual);
    }

    public interface ISecondBus : IBus;
}
