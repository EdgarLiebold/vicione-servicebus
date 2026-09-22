using Azure;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusHostRetryPolicyTests
{
    [Theory]
    [InlineData(ServiceBusFailureReason.MessagingEntityDisabled, false, true, false)]
    [InlineData(ServiceBusFailureReason.MessagingEntityNotFound, false, false, true)]
    [InlineData(ServiceBusFailureReason.MessagingEntityAlreadyExists, false, false, true)]
    [InlineData(ServiceBusFailureReason.MessageNotFound, false, false, false)]
    [InlineData(ServiceBusFailureReason.MessageSizeExceeded, true, false, false)]
    [InlineData(ServiceBusFailureReason.ServiceCommunicationProblem, true, true, true)]
    [InlineData(ServiceBusFailureReason.ServiceCommunicationProblem, false, false, false)]
    [InlineData(ServiceBusFailureReason.ServiceBusy, true, true, true)]
    [InlineData(ServiceBusFailureReason.ServiceBusy, false, false, false)]
    [InlineData(ServiceBusFailureReason.ServiceTimeout, true, true, true)]
    [InlineData(ServiceBusFailureReason.ServiceTimeout, false, false, false)]
    [InlineData(ServiceBusFailureReason.GeneralError, true, true, true)]
    [InlineData(ServiceBusFailureReason.GeneralError, false, false, false)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "broker-reason-and-transience-matrix")]
    public void BrokerReasons_HaveDistinctReceiveAndSendRetryEligibility(
        ServiceBusFailureReason reason, bool isTransient, bool expectedReceive, bool expectedSend)
    {
        IServiceBusHostConfiguration host = CreateHost();
        var failure = new ServiceBusException(isTransient, "broker failure", "queue", reason, null);

        Assert.Equal(expectedReceive, host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.Equal(expectedSend, host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(400, false)]
    [InlineData(403, false)]
    [InlineData(408, true)]
    [InlineData(429, true)]
    [InlineData(499, false)]
    [InlineData(500, true)]
    [InlineData(503, true)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "http-status-transient-boundary")]
    public void RequestFailedStatuses_RetryOnlyRecoverableHttpFailures(int status, bool expected)
    {
        IServiceBusHostConfiguration host = CreateHost();
        var failure = new RequestFailedException(status, "broker request failed");

        Assert.Equal(expected, host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.Equal(expected, host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "connection-exception-transience-boundary")]
    public void ConnectionFailures_RespectTheirExplicitTransientFlag(bool isTransient)
    {
        IServiceBusHostConfiguration host = CreateHost();
        var failure = new ConnectionException("namespace connection", isTransient);

        Assert.Equal(isTransient, host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.Equal(isTransient, host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(403, false)]
    [InlineData(408, true)]
    [InlineData(503, true)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "wrapped-http-failure-boundary")]
    public void ConnectionWrapper_PreservesHttpFailureEligibility(int status, bool expected)
    {
        IServiceBusHostConfiguration host = CreateHost();
        var failure = new ServiceBusConnectionException("receive endpoint", new RequestFailedException(status, "broker request failed"));

        Assert.Equal(expected, host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.Equal(expected, host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Theory]
    [InlineData(ServiceBusFailureReason.MessageSizeExceeded, true, false, false)]
    [InlineData(ServiceBusFailureReason.MessagingEntityNotFound, false, false, true)]
    [InlineData(ServiceBusFailureReason.ServiceTimeout, true, true, true)]
    [InlineData(ServiceBusFailureReason.GeneralError, true, true, true)]
    [InlineData(ServiceBusFailureReason.GeneralError, false, false, false)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "wrapped-broker-reason-boundary")]
    public void ConnectionWrapper_PreservesBrokerReasonEligibility(
        ServiceBusFailureReason reason, bool isTransient, bool expectedReceive, bool expectedSend)
    {
        IServiceBusHostConfiguration host = CreateHost();
        var brokerFailure = new ServiceBusException(isTransient, "broker failure", "queue", reason, null);
        var failure = new ServiceBusConnectionException("receive endpoint", brokerFailure);

        Assert.Equal(expectedReceive, host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.Equal(expectedSend, host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "nested-permanent-provider-cause-vetoes-transient-inner")]
    public void ConnectionWrapper_RejectsPermanentBrokerCauseEvenWithNestedTimeout()
    {
        IServiceBusHostConfiguration host = CreateHost();
        var brokerFailure = new ServiceBusException(true, "message too large", "queue",
            ServiceBusFailureReason.MessageSizeExceeded, new TimeoutException("request timed out"));
        var failure = new ServiceBusConnectionException("receive endpoint", brokerFailure);

        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.False(host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "aggregate-permanent-sibling-vetoes-transient")]
    public void AggregateFailures_RejectPermanentSiblingAfterRecoverableFailure()
    {
        IServiceBusHostConfiguration host = CreateHost();
        var timeout = new ServiceBusException(true, "broker timeout", "queue",
            ServiceBusFailureReason.ServiceTimeout, null);
        var forbidden = new RequestFailedException(403, "forbidden");
        var failure = new ServiceBusConnectionException("receive endpoint", new AggregateException(timeout, forbidden));

        Assert.False(host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.False(host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "aggregate-later-transient-sibling-is-visible")]
    public void AggregateFailures_RetryWhenLaterSiblingIsRecoverable()
    {
        IServiceBusHostConfiguration host = CreateHost();
        var unknown = new IOException("cleanup failed");
        var unavailable = new RequestFailedException(503, "unavailable");
        var aggregate = new AggregateException(unknown, unavailable);
        var failure = new ServiceBusConnectionException("receive endpoint", aggregate);

        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(aggregate));
        Assert.True(host.SendTransportRetryPolicy.IsHandled(aggregate));
        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.True(host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "intermediate-recoverable-provider-cause-is-visible")]
    public void NestedWrapper_RetriesRecoverableBrokerCauseBetweenUnknownExceptions()
    {
        IServiceBusHostConfiguration host = CreateHost();
        var timeout = new ServiceBusException(true, "broker timeout", "queue",
            ServiceBusFailureReason.ServiceTimeout, new IOException("socket closed"));
        var failure = new Exception("middleware wrapper", timeout);

        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.True(host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "missing-http-response-inside-timeout-remains-retryable")]
    public void TimeoutWrapper_DoesNotTurnMissingHttpResponseIntoAPermanentFailure()
    {
        IServiceBusHostConfiguration host = CreateHost();
        var failure = new TimeoutException("request timed out", new RequestFailedException(0, "no response"));

        Assert.True(host.ReceiveTransportRetryPolicy.IsHandled(failure));
        Assert.True(host.SendTransportRetryPolicy.IsHandled(failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "policy-context-schedules-timeout-but-stops-authorization")]
    public void RetryContext_SchedulesARecoverableTimeoutButStopsAuthorization()
    {
        IServiceBusHostConfiguration host = CreateHost();
        var timeout = new ServiceBusException(true, "broker timeout", "queue",
            ServiceBusFailureReason.ServiceTimeout, null);
        var forbidden = new RequestFailedException(403, "forbidden");
        using var retryable = host.SendTransportRetryPolicy.CreatePolicyContext(new TestPipeContext());
        using var terminal = host.SendTransportRetryPolicy.CreatePolicyContext(new TestPipeContext());

        Assert.True(retryable.CanRetry(timeout, out RetryContext<TestPipeContext> retry));
        Assert.NotNull(retry.Delay);
        Assert.InRange(retry.Delay.Value, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30));
        Assert.False(terminal.CanRetry(forbidden, out RetryContext<TestPipeContext> stopped));
        Assert.Equal(0, stopped.RetryCount);
    }

    static IServiceBusHostConfiguration CreateHost()
    {
        var topology = new ServiceBusTopologyConfiguration(AzureBusFactory.CreateMessageTopology());
        return new ServiceBusBusConfiguration(topology).HostConfiguration;
    }

    sealed class TestPipeContext : BasePipeContext;
}
