using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ReceiveMessageLimitsTests
{
    private static readonly Uri InputAddress = new("loopback://receive-limits/input");

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-MESSAGE-LIMITS", "shared-transport-envelope-boundary")]
    public void SharedTransportBoundary_RejectsMaximumPlusOneBeforeBodyAccess()
    {
        MessageLimits limits = Limits(maximumEnvelopeBytes: 8);
        var endpointContext = new ReceiveMessageLimitsTestContext(limits, InputAddress);
        using var receiveContext = new ReceiveEndpointDispatcherReceiveContext(
            endpointContext,
            new byte[9],
            new Dictionary<string, object>());

        MessageTooLargeException exception = Assert.Throws<MessageTooLargeException>(() => receiveContext.Body);

        Assert.Equal(9, exception.ActualBytes);
        Assert.Equal(8, exception.MaximumBytes);
        Assert.Equal(InputAddress, exception.EndpointAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-MESSAGE-LIMITS", "inclusive-boundary-and-stable-body")]
    public void SharedTransportBoundary_AcceptsExactMaximumAndReturnsTheSameBody()
    {
        MessageLimits limits = Limits(maximumEnvelopeBytes: 8);
        var endpointContext = new ReceiveMessageLimitsTestContext(limits, InputAddress);
        using var receiveContext = new ReceiveEndpointDispatcherReceiveContext(
            endpointContext,
            new byte[8],
            new Dictionary<string, object>());

        MessageBody first = receiveContext.Body;
        MessageBody second = receiveContext.Body;

        Assert.Same(first, second);
        Assert.Equal(8, first.Length);
        Assert.Equal(new byte[8], first.ToArray());
    }

    static MessageLimits Limits(int maximumEnvelopeBytes) => new()
    {
        MaxBodyBytes = maximumEnvelopeBytes,
        MaxEnvelopeBytes = maximumEnvelopeBytes,
        MaxJsonDepth = 32,
    };
}
