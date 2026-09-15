using System.Collections;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class NamedEntityCollectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-COLLECTION", "named-identity-idempotency-and-conflicts")]
    public void GetOrAdd_EnforcesNameDefinitionAndIdentifierIdentityIndependently()
    {
        var collection = new NamedEntityCollection<TestEntity, EntityHandle>(
            TestEntityComparer.Instance,
            TestEntityNameComparer.Instance);
        var orders = new TestEntity(1, "orders", "durable");

        Assert.Equal("entity", Assert.Throws<ArgumentNullException>(() => collection.GetOrAdd(null!)).ParamName);
        Assert.Same(orders, collection.GetOrAdd(orders));
        Assert.Same(orders, collection.GetOrAdd(new TestEntity(2, "orders", "durable")));
        Assert.Equal("entity", Assert.Throws<ArgumentException>(
            () => collection.GetOrAdd(new TestEntity(2, "orders", "temporary"))).ParamName);
        Assert.Equal("entity", Assert.Throws<ArgumentException>(
            () => collection.GetOrAdd(new TestEntity(1, "events", "durable"))).ParamName);

        var events = new TestEntity(2, "events", "temporary");
        Assert.Same(events, collection.GetOrAdd(events));
        Assert.Equal([orders, events], collection.ToArray());
        Assert.Equal([orders, events], ((IEnumerable)collection).Cast<TestEntity>().ToArray());
    }

    private sealed record TestEntity(long Id, string Name, string Settings) : EntityHandle;

    private sealed class TestEntityComparer : IEqualityComparer<TestEntity>
    {
        internal static readonly TestEntityComparer Instance = new();

        public bool Equals(TestEntity? x, TestEntity? y) =>
            StringComparer.Ordinal.Equals(x?.Name, y?.Name)
            && StringComparer.Ordinal.Equals(x?.Settings, y?.Settings);

        public int GetHashCode(TestEntity obj) => HashCode.Combine(obj.Name, obj.Settings);
    }

    private sealed class TestEntityNameComparer : IEqualityComparer<TestEntity>
    {
        internal static readonly TestEntityNameComparer Instance = new();

        public bool Equals(TestEntity? x, TestEntity? y) => StringComparer.Ordinal.Equals(x?.Name, y?.Name);

        public int GetHashCode(TestEntity obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
    }
}
