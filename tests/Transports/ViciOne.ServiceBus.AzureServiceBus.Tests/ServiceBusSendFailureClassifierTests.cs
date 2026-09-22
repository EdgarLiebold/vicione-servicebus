using System.Net.WebSockets;
using Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusSendFailureClassifierTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "missing-http-response-is-transient")]
    public void RequestWithoutHttpResponse_IsTransientEvenWhenWrapped()
    {
        var classifier = new ServiceBusSendFailureClassifier();
        var noResponse = new RequestFailedException(0, "no response");

        AssertKind(classifier, noResponse, TransportSendFailureKind.Transient);
        AssertKind(classifier, new ServiceBusConnectionException("endpoint", noResponse),
            TransportSendFailureKind.Transient);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "aggregate-permanent-sibling-has-precedence")]
    public void AggregateFailures_PermanentSiblingOverridesRecoverableFirstChild()
    {
        var classifier = new ServiceBusSendFailureClassifier();
        var failure = new AggregateException(
            new TimeoutException("request timed out"),
            new RequestFailedException(403, "forbidden"));

        AssertKind(classifier, failure, TransportSendFailureKind.Permanent);
        AssertKind(classifier, new ServiceBusConnectionException("endpoint", failure),
            TransportSendFailureKind.Permanent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "aggregate-later-transient-sibling-is-visible")]
    public void AggregateFailures_LaterRecoverableSiblingRemainsTransient()
    {
        var classifier = new ServiceBusSendFailureClassifier();
        var failure = new AggregateException(
            new IOException("cleanup failed"),
            new RequestFailedException(503, "service unavailable"));

        AssertKind(classifier, failure, TransportSendFailureKind.Transient);
    }

    [Theory]
    [InlineData(ServiceBusFailureReason.GeneralError, false, TransportSendFailureKind.Permanent)]
    [InlineData(ServiceBusFailureReason.GeneralError, true, TransportSendFailureKind.Transient)]
    [InlineData(ServiceBusFailureReason.ServiceTimeout, false, TransportSendFailureKind.Permanent)]
    [InlineData(ServiceBusFailureReason.MessageNotFound, false, TransportSendFailureKind.Permanent)]
    [InlineData(ServiceBusFailureReason.MessagingEntityDisabled, false, TransportSendFailureKind.Permanent)]
    [InlineData(ServiceBusFailureReason.MessagingEntityNotFound, false, TransportSendFailureKind.Transient)]
    [InlineData(ServiceBusFailureReason.MessagingEntityAlreadyExists, false, TransportSendFailureKind.Transient)]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "broker-reason-direct-and-wrapped-send-taxonomy")]
    public void BrokerFailures_KeepTheirSendEligibilityInsideConnectionWrappers(
        ServiceBusFailureReason reason, bool isTransient, TransportSendFailureKind expected)
    {
        var classifier = new ServiceBusSendFailureClassifier();
        var brokerFailure = new ServiceBusException(isTransient, "broker failure", "queue", reason, null);

        AssertKind(classifier, new ServiceBusConnectionException("endpoint", brokerFailure), expected);
        AssertKind(classifier, brokerFailure, expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "foreign-connection-failure-is-unclassified")]
    public void ForeignConnectionFailure_IsNotClaimedByAzureClassifier(bool isTransient)
    {
        var classifier = new ServiceBusSendFailureClassifier();
        var failure = new ConnectionException("another transport", new ArgumentException("invalid endpoint"), isTransient);

        Assert.False(classifier.TryClassify(failure, out TransportSendFailureKind kind));
        Assert.Equal(TransportSendFailureKind.Unclassified, kind);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "reason-status-chain-and-permanent-precedence")]
    public void TypedFailures_UseReasonsStatusesAndTheCompleteChainWithPermanentPrecedence()
    {
        var classifier = new ServiceBusSendFailureClassifier();

        AssertKind(classifier, new ServiceBusException("text has no policy", ServiceBusFailureReason.ServiceTimeout),
            TransportSendFailureKind.Transient);
        AssertKind(classifier, new ServiceBusException("temporary text", ServiceBusFailureReason.MessageSizeExceeded),
            TransportSendFailureKind.Permanent);
        AssertKind(classifier, new TimeoutException("typed timeout"), TransportSendFailureKind.Transient);
        AssertKind(classifier, new WebSocketException("typed socket failure"), TransportSendFailureKind.Transient);
        AssertKind(classifier, new ServiceBusConnectionException("outer", new IOException()), TransportSendFailureKind.Transient);
        AssertKind(classifier, new ServiceBusConnectionException("outer", new UnauthorizedAccessException()),
            TransportSendFailureKind.Permanent);
        AssertKind(classifier, new RequestFailedException(503, "permanent text"), TransportSendFailureKind.Transient);
        AssertKind(classifier, new RequestFailedException(500, "permanent text"), TransportSendFailureKind.Transient);
        AssertKind(classifier, new RequestFailedException(429, "permanent text"), TransportSendFailureKind.Transient);
        AssertKind(classifier, new RequestFailedException(408, "permanent text"), TransportSendFailureKind.Transient);
        AssertKind(classifier, new RequestFailedException(403, "temporary text"), TransportSendFailureKind.Permanent);
        AssertKind(classifier, new RequestFailedException(499, "temporary text"), TransportSendFailureKind.Permanent);
        AssertKind(classifier, new TimeoutException("outer", new UnauthorizedAccessException("inner")),
            TransportSendFailureKind.Permanent);
        AssertKind(classifier, new UnauthorizedAccessException("direct terminal failure"), TransportSendFailureKind.Permanent);
        Assert.False(classifier.TryClassify(new IOException("503 words do not matter"), out TransportSendFailureKind unknown));
        Assert.Equal(TransportSendFailureKind.Unclassified, unknown);
        Assert.False(classifier.TryClassify(new RequestFailedException(399, "not a failure status"),
            out TransportSendFailureKind nonFailureStatus));
        Assert.Equal(TransportSendFailureKind.Unclassified, nonFailureStatus);
        Assert.Throws<ArgumentNullException>(() => classifier.TryClassify(null!, out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "registration-is-singleton-and-idempotent")]
    public void BusRegistration_AddsExactlyOneClassifierDescriptor()
    {
        var defaultServices = new ServiceCollection();
        defaultServices.AddViciOneServiceBus(configuration => configuration.UsingAzureServiceBus());
        AssertRegistration(defaultServices);

        var typedServices = new ServiceCollection();
        typedServices.AddViciOneServiceBus<ISecondBus>(configuration => configuration.UsingAzureServiceBus());
        AssertRegistration(typedServices);

        var services = new ServiceCollection();
        services
            .AddViciOneServiceBus(configuration => configuration.UsingAzureServiceBus())
            .AddViciOneServiceBus<ISecondBus>(configuration => configuration.UsingAzureServiceBus());

        AssertRegistration(services);
    }

    static void AssertRegistration(IServiceCollection services)
    {
        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ITransportSendFailureClassifier));
        Assert.Equal(typeof(ServiceBusSendFailureClassifier), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    static void AssertKind(ITransportSendFailureClassifier classifier, Exception exception, TransportSendFailureKind expected)
    {
        Assert.True(classifier.TryClassify(exception, out TransportSendFailureKind actual));
        Assert.Equal(expected, actual);
    }

    public interface ISecondBus : IBus;
}
