using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureSubscriptionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "subscription-value-semantics")]
    public void Equality_UsesBothEndpointAndRequestIdentity()
    {
        var address = new Uri("loopback://localhost/future-result");
        var requestId = Guid.Parse("4f2d7f0e-e020-4c89-aaf7-d67a8911af58");
        var subscription = new FutureSubscription(address, requestId);
        var equivalent = new FutureSubscription(address, requestId);
        var differentRequest = new FutureSubscription(address, Guid.Empty);
        var differentAddress = new FutureSubscription(new Uri("loopback://localhost/other-result"), requestId);

        Assert.Equal(subscription, equivalent);
        Assert.True(subscription == equivalent);
        Assert.False(subscription != equivalent);
        Assert.Equal(subscription.GetHashCode(), equivalent.GetHashCode());
        Assert.True(FutureSubscription.Comparer.Equals(subscription, equivalent));
        Assert.Equal(FutureSubscription.Comparer.GetHashCode(subscription), FutureSubscription.Comparer.GetHashCode(equivalent));
        Assert.NotEqual(subscription, differentRequest);
        Assert.NotEqual(subscription, differentAddress);
        Assert.False(subscription.Equals(null));
        Assert.False(subscription.Equals(new object()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "subscription-required-address")]
    public void ConstructorAndComparer_RejectMissingRequiredValues()
    {
        var constructorException = Assert.Throws<ArgumentNullException>(() => new FutureSubscription(null!));
        var comparerException = Assert.Throws<ArgumentNullException>(() => FutureSubscription.Comparer.GetHashCode(null!));

        Assert.Equal("address", constructorException.ParamName);
        Assert.Equal("obj", comparerException.ParamName);
    }
}
