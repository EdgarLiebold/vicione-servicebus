using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsSendFailureClassifierTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-SEND-FAILURE", "status-typed-chain-and-permanent-precedence")]
    public void TypedFailures_UseStatusCodesAndTheCompleteChainWithPermanentPrecedence()
    {
        var classifier = new AmazonSqsSendFailureClassifier();

        AssertKind(classifier, ServiceFailure(HttpStatusCode.RequestTimeout), TransportSendFailureKind.Transient);
        AssertKind(classifier, ServiceFailure(HttpStatusCode.TooManyRequests), TransportSendFailureKind.Transient);
        AssertKind(classifier, ServiceFailure(HttpStatusCode.InternalServerError), TransportSendFailureKind.Transient);
        AssertKind(classifier, ServiceFailure(HttpStatusCode.ServiceUnavailable), TransportSendFailureKind.Transient);
        AssertKind(classifier, ServiceFailure(HttpStatusCode.BadRequest), TransportSendFailureKind.Permanent);
        AssertKind(classifier, ServiceFailure(HttpStatusCode.Forbidden), TransportSendFailureKind.Permanent);
        AssertKind(classifier, ServiceFailure((HttpStatusCode)499), TransportSendFailureKind.Permanent);
        AssertKind(classifier, new InvalidMessageContentsException("temporary text"), TransportSendFailureKind.Permanent);
        AssertKind(classifier, new AmazonSqsConnectionException("outer", new IOException()), TransportSendFailureKind.Transient);
        AssertKind(classifier, new AmazonSqsConnectionException("temporary text"), TransportSendFailureKind.Permanent);
        AssertKind(
            classifier,
            new AmazonSqsConnectionException("outer", ServiceFailure(HttpStatusCode.Forbidden)),
            TransportSendFailureKind.Permanent);
        Assert.False(classifier.TryClassify(new IOException("503 words do not matter"), out TransportSendFailureKind unknown));
        Assert.Equal(TransportSendFailureKind.Unclassified, unknown);
        Assert.False(classifier.TryClassify(ServiceFailure((HttpStatusCode)399), out TransportSendFailureKind nonFailureStatus));
        Assert.Equal(TransportSendFailureKind.Unclassified, nonFailureStatus);
        Assert.Throws<ArgumentNullException>(() => classifier.TryClassify(null!, out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-SEND-FAILURE", "registration-is-singleton-and-idempotent")]
    public void BusRegistration_AddsExactlyOneClassifierDescriptor()
    {
        var services = new ServiceCollection();

        services
            .AddViciOneServiceBus(configuration => configuration.UsingAmazonSqs())
            .AddViciOneServiceBus<ISecondBus>(configuration => configuration.UsingAmazonSqs());

        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ITransportSendFailureClassifier));
        Assert.Equal(typeof(AmazonSqsSendFailureClassifier), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    static AmazonSQSException ServiceFailure(HttpStatusCode statusCode) =>
        new("message text deliberately carries no policy") { StatusCode = statusCode };

    static void AssertKind(ITransportSendFailureClassifier classifier, Exception exception, TransportSendFailureKind expected)
    {
        Assert.True(classifier.TryClassify(exception, out TransportSendFailureKind actual));
        Assert.Equal(expected, actual);
    }

    public interface ISecondBus : IBus;
}
