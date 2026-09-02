using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class GreenCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-DIRECT-ADD", "index-identity-and-factory-bypass")]
    public async Task Add_IndexesTheExactValueWithoutInvokingTheFallbackFactory()
    {
        var cache = new GreenCache<Endpoint>(CreateSettings());
        IIndex<Uri, Endpoint> index = cache.AddIndex("address", endpoint => endpoint.Address);
        var address = new Uri("rabbitmq://localhost/vhost/input-queue");
        var expected = new Endpoint(address);
        var factoryCalls = 0;

        cache.Add(expected);

        Endpoint actual = await index.Get(address, key =>
        {
            Interlocked.Increment(ref factoryCalls);
            return Task.FromResult(new Endpoint(key));
        });
        Endpoint visible = await Assert.Single(cache.GetAll());

        Assert.Same(expected, actual);
        Assert.Same(expected, visible);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.TotalCount);
        Assert.Equal(1, cache.Statistics.Hits);
        Assert.Equal(0, cache.Statistics.Misses);
        Assert.Equal(0, cache.Statistics.CreateFaults);
    }

    private static CacheSettings CreateSettings() => new(
        capacity: 100,
        minAge: TimeSpan.FromMinutes(1),
        maxAge: TimeSpan.FromMinutes(30),
        nowProvider: () => DateTime.UnixEpoch);

    private sealed record Endpoint(Uri Address);
}
