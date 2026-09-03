using System.Net.WebSockets;
using Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusSendFailureClassifierTests
{
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
