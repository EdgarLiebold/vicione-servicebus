using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class SingleThreadedDictionaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-UTILITY-DICTIONARY", "copy-on-write-comparer-snapshots-and-factory-failure")]
    public void Mutations_PreserveComparerSnapshotsAndFailureAtomicity()
    {
        var dictionary = new SingleThreadedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var alpha = new object();
        var beta = new object();
        var gamma = new object();
        Assert.True(dictionary.TryAdd("Alpha", _ => alpha));
        Assert.True(dictionary.TryAdd("Beta", _ => beta));
        using IEnumerator<KeyValuePair<string, object>> originalEnumerator = dictionary.GetEnumerator();
        IEnumerable<string> originalKeys = dictionary.Keys;
        IEnumerable<object> originalValues = dictionary.Values;
        int duplicateFactoryCalls = 0;
        Assert.False(dictionary.TryAdd("ALPHA", _ =>
        {
            duplicateFactoryCalls++;
            throw new InvalidOperationException("duplicate factory must not run");
        }));
        Assert.Equal(0, duplicateFactoryCalls);
        var failure = new InvalidOperationException("factory failed");
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            dictionary.TryAdd("Gamma", _ => throw failure)));
        Assert.Equal(2, dictionary.Count);
        Assert.False(dictionary.ContainsKey("Gamma"));
        Assert.Same(alpha, dictionary["ALPHA"]);
        Assert.Same(beta, dictionary["BETA"]);

        Assert.True(dictionary.TryAdd("Gamma", _ => gamma));
        Assert.True(dictionary.TryRemove("BETA", out object? removed));
        Assert.Same(beta, removed);
        Assert.False(dictionary.TryRemove("missing", out object? absent));
        Assert.Null(absent);
        Assert.Equal(2, dictionary.Count);
        Assert.Same(alpha, dictionary["alpha"]);
        Assert.Same(gamma, dictionary["GAMMA"]);
        Assert.False(dictionary.ContainsKey("Beta"));

        var enumerated = new List<KeyValuePair<string, object>>();
        while (originalEnumerator.MoveNext())
            enumerated.Add(originalEnumerator.Current);
        Assert.Equal(new[] { "Alpha", "Beta" }, enumerated.Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal));
        Assert.Same(alpha, Assert.Single(enumerated, pair => pair.Key == "Alpha").Value);
        Assert.Same(beta, Assert.Single(enumerated, pair => pair.Key == "Beta").Value);
        Assert.Equal(new[] { "Alpha", "Beta" }, originalKeys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(2, originalValues.Count());
        Assert.Contains(originalValues, value => ReferenceEquals(value, alpha));
        Assert.Contains(originalValues, value => ReferenceEquals(value, beta));

        IEnumerable<string> newerKeys = dictionary.Keys;
        IEnumerable<object> newerValues = dictionary.Values;
        dictionary.Clear();
        Assert.Empty(dictionary);
        Assert.Equal(new[] { "Alpha", "Gamma" }, newerKeys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(2, newerValues.Count());
        Assert.Contains(newerValues, value => ReferenceEquals(value, alpha));
        Assert.Contains(newerValues, value => ReferenceEquals(value, gamma));
        Assert.Equal(new[] { "Alpha", "Beta" }, originalKeys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.True(dictionary.TryAdd("Alpha", _ => alpha));
        Assert.Same(alpha, dictionary["ALPHA"]);
        dictionary.Clear();
        dictionary.Clear();
        Assert.Empty(dictionary);
    }
}
