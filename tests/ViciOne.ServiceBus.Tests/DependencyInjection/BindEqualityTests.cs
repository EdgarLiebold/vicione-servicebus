using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class BindEqualityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BIND-IDENTITY", "owner-type-and-bound-value-control-equality-and-lookup")]
    public void BindEquality_PreservesOwnerAndValueIdentityInKeyedLookup()
    {
        var first = Bind<IBus>.Create(new BoundService("orders"));
        var equivalent = Bind<IBus>.Create(new BoundService("orders"));
        var differentValue = Bind<IBus>.Create(new BoundService("billing"));
        var differentOwner = Bind<IOtherBus>.Create(new BoundService("orders"));
        var derived = new DerivedBind(new BoundService("orders"));
        var lookup = new Dictionary<Bind<IBus, BoundService>, string>
        {
            [first] = "registered service",
        };

        Assert.True(first.Equals(first));
        Assert.True(first.Equals((object)first));
        Assert.True(first.Equals(equivalent));
        Assert.True(first.Equals((object)equivalent));
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal("registered service", lookup[equivalent]);

        Assert.False(first.Equals((Bind<IBus, BoundService>?)null));
        Assert.False(first.Equals((object?)null));
        Assert.False(first.Equals(differentValue));
        Assert.False(first.Equals((object)differentOwner));
        Assert.False(first.Equals("orders"));
        Assert.False(lookup.ContainsKey(differentValue));
        Assert.False(first.Equals((Bind<IBus, BoundService>)derived));
        Assert.False(first.Equals((object)derived));
        Assert.False(derived.Equals(first));
        Assert.False(lookup.ContainsKey(derived));
    }

    private sealed record BoundService(string Name);

    private sealed class DerivedBind(BoundService value) : Bind<IBus, BoundService>(value);

    private interface IOtherBus : IBus;
}
