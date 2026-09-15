using System.Reflection;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga.InMemoryRepository;

public sealed class IndexedSagaPropertyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-removal-uses-captured-registration-key")]
    public void MutablePropertyValue_DoesNotPreventRemovalOfItsRegisteredKey()
    {
        var index = CreateIndex();
        var instance = new SagaInstance<PropertyState>(new PropertyState { Group = "before" });
        index.Add(instance);
        instance.Instance.Group = "after";
        index.Remove(instance);

        Assert.Equal(0, index.Count);
        Assert.Null(index["before"]);
        Assert.Null(index["after"]);
        Assert.Empty(index.Where(_ => true));
        Assert.False(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "equal-property-members-remain-independent-wrapper-references")]
    public void EqualStateWrappers_RemainIndependentWithinTheSamePropertyBucket()
    {
        var index = CreateIndex();
        var first = new SagaInstance<PropertyState>(new PropertyState());
        var second = new SagaInstance<PropertyState>(new PropertyState());
        Assert.Equal(first, second);
        index.Add(first);
        index.Add(second);

        Assert.Equal(1, index.Count);
        Assert.Equal(2, index.Where(_ => true).Count());
        Assert.Equal(2, index.Where("group", _ => true).Count());
        Assert.Throws<InvalidOperationException>(() => { _ = index["group"]; });
        index.Remove(first);
        Assert.Same(second, index["group"]);
        Assert.Single(index.Where(_ => true));
        Assert.False(first.IsRemoved);
        Assert.False(second.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-state-hash-mutation-does-not-strand-reference-membership")]
    public void MutableStateHash_DoesNotStrandARegisteredPropertyMember()
    {
        var index = CreateIndex();
        var instance = new SagaInstance<PropertyState>(new PropertyState { Value = 3 });
        index.Add(instance);
        instance.Instance.Value = 17;
        index.Remove(instance);

        Assert.Equal(0, index.Count);
        Assert.Empty(index.Where(_ => true));
        Assert.Null(index["group"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-unregistered-equal-reference-cannot-evict-real-member")]
    public void EqualUnregisteredWrapper_DoesNotEvictTheRegisteredPropertyMember()
    {
        var index = CreateIndex();
        var retained = new SagaInstance<PropertyState>(new PropertyState());
        var unrelated = new SagaInstance<PropertyState>(new PropertyState());
        index.Add(retained);
        index.Remove(unrelated);

        Assert.Equal(1, index.Count);
        Assert.Same(retained, index["group"]);
        Assert.False(retained.IsRemoved);
        Assert.False(unrelated.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-null-key-is-a-supported-distinct-bucket")]
    public void NullPropertyKey_IsSupportedForLookupFilteringCountAndRemoval()
    {
        var index = CreateIndex();
        var instance = new SagaInstance<PropertyState>(new PropertyState { Group = null });
        index.Add(instance);

        Assert.Equal(1, index.Count);
        Assert.Same(instance, index[null!]);
        Assert.Same(instance, Assert.Single(index.Where(null!, state => state.Group == null)));
        index.Remove(instance);
        Assert.Equal(0, index.Count);
        Assert.Null(index[null!]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-count-preserves-distinct-key-semantics")]
    public void Count_ReportsDistinctRegisteredKeysRatherThanWrapperCount()
    {
        var index = CreateIndex();
        var first = new SagaInstance<PropertyState>(new PropertyState { Group = "first", Value = 1 });
        var second = new SagaInstance<PropertyState>(new PropertyState { Group = "first", Value = 2 });
        var third = new SagaInstance<PropertyState>(new PropertyState { Group = "third", Value = 3 });
        index.Add(first);
        index.Add(second);
        index.Add(third);

        Assert.Equal(2, index.Count);
        Assert.Equal(3, index.Where(_ => true).Count());
        index.Remove(first);
        Assert.Equal(2, index.Count);
        Assert.Same(second, index["first"]);
        index.Remove(second);
        Assert.Equal(1, index.Count);
        Assert.Same(third, index["third"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "mutable-key-object-equality-hash-does-not-break-membership")]
    public void MutableKeyObject_DoesNotBreakLookupCountOrRemoval()
    {
        var index = new IndexedSagaProperty<KeyState, MutableKey>(typeof(KeyState).GetProperty(nameof(KeyState.Key))!);
        var key = new MutableKey { Value = 7 };
        var instance = new SagaInstance<KeyState>(new KeyState { Key = key });
        index.Add(instance);
        key.Value = 11;

        Assert.Same(instance, index[new MutableKey { Value = 11 }]);
        Assert.Same(instance, Assert.Single(index.Where(new MutableKey { Value = 11 }, _ => true)));
        Assert.Equal(1, index.Count);
        index.Remove(instance);
        Assert.Equal(0, index.Count);
        Assert.Empty(index.Where(_ => true));
    }

    [Theory]
    [InlineData("Add", "instance")]
    [InlineData("Remove", "instance")]
    [InlineData("Where", "filter")]
    [InlineData("WhereKey", "filter")]
    [InlineData("Select", "transformer")]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-required-inputs-rejected-before-effects")]
    public void RequiredInput_IsRejectedWithoutChangingThePropertyIndex(string operation, string parameter)
    {
        var index = CreateIndex();
        var instance = new SagaInstance<PropertyState>(new PropertyState());
        index.Add(instance);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            switch (operation)
            {
                case "Add": index.Add(null!); break;
                case "Remove": index.Remove(null!); break;
                case "Where": _ = index.Where(null!); break;
                case "WhereKey": _ = index.Where("group", null!); break;
                case "Select": _ = index.Select<PropertyState>(null!); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal(parameter, exception.ParamName);
        Assert.Equal(1, index.Count);
        Assert.Same(instance, index["group"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-wrong-key-type-has-exact-argument-diagnostic")]
    public void WrongKeyType_IsRejectedWithTheExactParameterName(bool filter)
    {
        var index = CreateIndex();

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
        {
            if (filter)
                _ = index.Where(17, _ => true);
            else
                _ = index[17];
        });

        Assert.Equal("key", exception.ParamName);
        Assert.Equal(0, index.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-callbacks-cannot-corrupt-original-membership-snapshot")]
    public void CallbackMutation_DoesNotChangeItsOriginalMembershipSnapshot(bool transform)
    {
        var index = CreateIndex();
        var first = new SagaInstance<PropertyState>(new PropertyState { Group = "first", Value = 1 });
        var added = new SagaInstance<PropertyState>(new PropertyState { Group = "added", Value = 2 });
        index.Add(first);
        int calls = 0;
        if (transform)
            Assert.Same(first.Instance, Assert.Single(index.Select(state => { calls++; index.Add(added); return state; })));
        else
            Assert.Same(first, Assert.Single(index.Where(state => { calls++; index.Add(added); return true; })));

        Assert.Equal(1, calls);
        Assert.Equal(2, index.Count);
        Assert.Same(added, index["added"]);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("wrong-type")]
    [InlineData("static")]
    [InlineData("indexer")]
    [InlineData("write-only")]
    [InlineData("foreign-type")]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-construction-validates-required-readable-instance-metadata")]
    public void InvalidPropertyMetadata_IsRejectedBeforeIndexCreation(string kind)
    {
        PropertyInfo? property = kind switch
        {
            "null" => null,
            "wrong-type" => typeof(PropertyState).GetProperty(nameof(PropertyState.Value)),
            "static" => typeof(PropertyState).GetProperty(nameof(PropertyState.StaticGroup)),
            "indexer" => typeof(PropertyState).GetProperty("Item"),
            "write-only" => typeof(PropertyState).GetProperty(nameof(PropertyState.WriteOnlyGroup)),
            "foreign-type" => typeof(BasePropertyState).GetProperty(nameof(BasePropertyState.Group)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() => new IndexedSagaProperty<PropertyState, string>(property!));

        Assert.Equal("propertyInfo", exception.ParamName);
        if (kind == "null")
            Assert.IsType<ArgumentNullException>(exception);
        else
            Assert.IsType<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "nonnullable-value-key-rejects-null-with-exact-key-diagnostic")]
    public void NullNonNullableValueKey_IsRejectedWithItsExactParameterName(bool filter)
    {
        var index = new IndexedSagaProperty<PropertyState, int>(typeof(PropertyState).GetProperty(nameof(PropertyState.Value))!);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            if (filter)
                _ = index.Where(null!, _ => true);
            else
                _ = index[null!];
        });

        Assert.Equal("key", exception.ParamName);
        Assert.Equal(0, index.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "nullable-value-key-null-and-present-membership-round-trip")]
    public void NullableValueKey_SupportsNullAndPresentValues(bool present)
    {
        var index = new IndexedSagaProperty<PropertyState, int?>(typeof(PropertyState).GetProperty(nameof(PropertyState.OptionalValue))!);
        var instance = new SagaInstance<PropertyState>(new PropertyState { OptionalValue = present ? 7 : null });
        index.Add(instance);
        object? key = instance.Instance.OptionalValue;

        Assert.Equal(1, index.Count);
        Assert.Same(instance, index[key!]);
        Assert.Same(instance, Assert.Single(index.Where(key!, _ => true)));
        index.Remove(instance);
        Assert.Equal(0, index.Count);
        Assert.Null(index[key!]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "reentrant-property-registration-fails-before-repeating-getter-and-is-reusable")]
    public void ReentrantRegistration_IsRejectedBeforeRepeatingItsGetterAndCanBeRetried()
    {
        var index = CreateIndex();
        var state = new PropertyState();
        var instance = new SagaInstance<PropertyState>(state);
        int reads = 0;
        state.OnReadGroup = () =>
        {
            if (++reads > 1)
                throw new InvalidOperationException("fixture prevented recursive getter overflow");
            index.Add(instance);
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => index.Add(instance));

        Assert.Equal("A saga index registration is already in progress for this wrapper.", exception.Message);
        Assert.Equal(1, reads);
        Assert.Equal(0, index.Count);
        state.OnReadGroup = null;
        index.Add(instance);
        Assert.Same(instance, index["group"]);
        Assert.Equal(1, index.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "direct-index-reads-exact-base-or-interface-metadata-through-the-implementation")]
    public void DirectInheritedPropertyMetadata_ReadsReferencedStateWithoutRepeatingRemovalGetters(bool interfaceMetadata)
    {
        PropertyInfo property = interfaceMetadata
            ? typeof(IPropertyState).GetProperty(nameof(IPropertyState.Group))!
            : typeof(BasePropertyState).GetProperty(nameof(BasePropertyState.Group))!;
        var index = new IndexedSagaProperty<DerivedPropertyState, string>(property);
        var state = new DerivedPropertyState { Group = "registered" };
        var instance = new SagaInstance<DerivedPropertyState>(state);
        index.Add(instance);
        state.Group = "later";

        Assert.Equal(1, state.GroupReads);
        Assert.Same(instance, index["registered"]);
        Assert.Null(index["later"]);
        index.Remove(instance);
        Assert.Equal(1, state.GroupReads);
        Assert.Equal(0, index.Count);
        Assert.False(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "hidden-same-name-properties-retain-their-exact-declaring-member-and-key-type")]
    public void HiddenSameNameProperties_ReadTheirExactDeclaringMemberAndKeyType()
    {
        var numberIndex = new IndexedSagaProperty<HiddenPropertyState, int>(typeof(HiddenPropertyBase).GetProperty(nameof(HiddenPropertyBase.Value))!);
        var textIndex = new IndexedSagaProperty<HiddenPropertyState, string>(typeof(HiddenPropertyState).GetProperty(nameof(HiddenPropertyState.Value), typeof(string))!);
        var state = new HiddenPropertyState { Value = "text" };
        ((HiddenPropertyBase)state).Value = 7;
        var instance = new SagaInstance<HiddenPropertyState>(state);
        numberIndex.Add(instance);
        textIndex.Add(instance);

        Assert.Same(instance, numberIndex[7]);
        Assert.Same(instance, textIndex["text"]);
        Assert.Null(numberIndex[9]);
        Assert.Null(textIndex["other"]);
        numberIndex.Remove(instance);
        textIndex.Remove(instance);
        Assert.Equal(0, numberIndex.Count);
        Assert.Equal(0, textIndex.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "keyed-property-query-honors-positive-and-negative-filter-results")]
    public void KeyedQuery_FiltersActualStatesAndRejectsNonmatchingBucketMembers()
    {
        var index = CreateIndex();
        var first = new SagaInstance<PropertyState>(new PropertyState { Group = "group", Value = 1 });
        var second = new SagaInstance<PropertyState>(new PropertyState { Group = "group", Value = 2 });
        index.Add(first);
        index.Add(second);

        Assert.Same(second, Assert.Single(index.Where("group", state => state.Value == 2)));
        Assert.Empty(index.Where("group", _ => false));
        Assert.Equal(1, index.Count);
        Assert.Equal(2, index.Where(_ => true).Count());
        Assert.Same(first, Assert.Single(index.Where("group", state => state.Value == 1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "property-select-materializes-original-membership-and-transformed-values")]
    public void Select_MaterializesOriginalMembershipAndTransformedValues()
    {
        var index = CreateIndex();
        var first = new SagaInstance<PropertyState>(new PropertyState { Group = "first", Value = 5 });
        index.Add(first);
        IEnumerable<int> selected = index.Select(state => state.Value);
        first.Instance.Value = 13;
        var added = new SagaInstance<PropertyState>(new PropertyState { Group = "added", Value = 17 });
        index.Add(added);

        Assert.Equal(5, Assert.Single(selected));
        Assert.Equal(2, index.Count);
        Assert.Same(first, index["first"]);
        Assert.Same(added, index["added"]);
    }

    private interface IPropertyState
    {
        string? Group { get; }
    }

    private class BasePropertyState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public virtual string? Group { get; set; }
    }

    private sealed class DerivedPropertyState : BasePropertyState, IPropertyState
    {
        public int GroupReads { get; private set; }
        public override string? Group
        {
            get { GroupReads++; return base.Group; }
            set => base.Group = value;
        }
    }

    private class HiddenPropertyBase : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public int Value { get; set; }
    }

    private sealed class HiddenPropertyState : HiddenPropertyBase
    {
        public new string Value { get; set; } = "text";
    }

    private static IndexedSagaProperty<PropertyState, string> CreateIndex() =>
        new(typeof(PropertyState).GetProperty(nameof(PropertyState.Group))!);

    private sealed class MutableKey
    {
        public int Value { get; set; }
        public override bool Equals(object? other) => other is MutableKey key && key.Value == Value;
        public override int GetHashCode() => Value;
    }

    private sealed class KeyState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public MutableKey Key { get; set; } = new();
    }

    private sealed class PropertyState : ISaga
    {
        private string? _group = "group";
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public string? Group { get { OnReadGroup?.Invoke(); return _group; } set => _group = value; }
        public Action? OnReadGroup { get; set; }
        public int Value { get; set; }
        public int? OptionalValue { get; set; }
        public static string StaticGroup => "static";
        public string this[int index] => "indexed";
        public string WriteOnlyGroup { set => Group = value; }
        public override bool Equals(object? other) => other is PropertyState state && state.Value == Value;
        public override int GetHashCode() => Value;
    }
}
