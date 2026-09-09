using ViciOne.ServiceBus.Payloads;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware;

public sealed class ListPayloadCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "required-inputs-and-non-null-results")]
    public void Cache_RejectsMissingInputsAndNullFactoryResults()
    {
        var cache = new ListPayloadCache();

        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentNullException>(() => new ListPayloadCache(null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentException>(() => new ListPayloadCache([new object(), null!])).ParamName);
        Assert.Equal(
            "payloadType",
            Assert.Throws<ArgumentNullException>(() => cache.HasPayloadType(null!)).ParamName);
        Assert.Equal(
            "payloadFactory",
            Assert.Throws<ArgumentNullException>(() => cache.GetOrAddPayload<TestPayload>(null!)).ParamName);
        Assert.Equal(
            "addFactory",
            Assert.Throws<ArgumentNullException>(() => cache.AddOrUpdatePayload<TestPayload>(null!, existing => existing)).ParamName);
        Assert.Equal(
            "updateFactory",
            Assert.Throws<ArgumentNullException>(() => cache.AddOrUpdatePayload(() => new TestPayload("value"), null!)).ParamName);
        Assert.Throws<InvalidOperationException>(() => cache.GetOrAddPayload<TestPayload>(() => null!));
        Assert.Throws<InvalidOperationException>(() => cache.AddOrUpdatePayload<TestPayload>(() => null!, existing => existing));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "latest-assignable-payload")]
    public void Cache_ResolvesAndUpdatesTheMostRecentAssignablePayload()
    {
        var first = new TestPayload("first");
        var second = new DerivedPayload("second");
        var cache = new ListPayloadCache([first, second]);

        Assert.True(cache.HasPayloadType(typeof(TestPayload)));
        Assert.True(cache.TryGetPayload(out TestPayload? resolved));
        Assert.Same(second, resolved);

        TestPayload updated = cache.AddOrUpdatePayload(
            () => new TestPayload("unused"),
            existing => new TestPayload(existing.Value + "-updated"));

        Assert.Equal("second-updated", updated.Value);
        Assert.Same(updated, cache.GetOrAddPayload(() => new TestPayload("unused")));
    }

    private record TestPayload(string Value);

    private sealed record DerivedPayload(string DerivedValue) : TestPayload(DerivedValue);
}
