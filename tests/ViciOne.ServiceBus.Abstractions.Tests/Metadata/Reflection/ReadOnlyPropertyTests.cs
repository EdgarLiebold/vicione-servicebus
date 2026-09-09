using System.Reflection;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata.Reflection;

public sealed class ReadOnlyPropertyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-READ", "public-untyped-value-boxing")]
    public void UntypedAccessor_ReadsPublicValueProperty()
    {
        var accessor = new ReadOnlyProperty(Property<AccessTarget>(nameof(AccessTarget.Count)));

        object? value = accessor.Get(new AccessTarget { Count = 42 });

        Assert.IsType<int>(value);
        Assert.Equal(42, value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-READ", "boxed-value-type-reflection-path")]
    public void UntypedAccessor_ReadsAPropertyFromABoxedValueType()
    {
        var accessor = new ReadOnlyProperty(Property<ValueTarget>(nameof(ValueTarget.Count)));
        object target = new ValueTarget { Count = 73 };

        object? value = accessor.Get(target);

        Assert.IsType<int>(value);
        Assert.Equal(73, value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-READ", "inherited-typed-accessor")]
    public void TypedAccessor_ReadsAnInheritedProperty()
    {
        var accessor = new ReadOnlyProperty<DerivedTarget, int>(Property<BaseTarget>(nameof(BaseTarget.Count)));

        int value = accessor.Get(new DerivedTarget { Count = 19 });

        Assert.Equal(19, value);
        Assert.Equal(typeof(BaseTarget), accessor.Property.DeclaringType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-POLICY", "public-only-rejects-private-getter")]
    public void PublicOnly_PrivateGetterFailsWithAnExactAccessorError()
    {
        var accessor = new ReadOnlyProperty<AccessTarget>(Property<AccessTarget>(nameof(AccessTarget.PrivateGetter)));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => accessor.Get(new AccessTarget()));

        Assert.Equal("No eligible getter is available for AccessTarget.PrivateGetter.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-POLICY", "include-non-public-private-getter")]
    public void IncludeNonPublic_PrivateGetterReturnsItsValue()
    {
        var accessor = new ReadOnlyProperty<AccessTarget>(
            Property<AccessTarget>(nameof(AccessTarget.PrivateGetter)),
            PropertyAccessPolicy.IncludeNonPublic);

        object? value = accessor.Get(new AccessTarget());

        Assert.Equal("private-getter", value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-FALLBACK", "private-getter-preserves-exception")]
    public void ReflectionFallback_PrivateGetterPreservesTheOriginalException()
    {
        var failure = new IntentionalAccessorException("getter");
        var target = new AccessTarget(failure, new IntentionalAccessorException("unused"));
        var accessor = new ReadOnlyProperty<AccessTarget>(
            Property<AccessTarget>(nameof(AccessTarget.ThrowingPrivateGetter)),
            PropertyAccessPolicy.IncludeNonPublic);

        IntentionalAccessorException exception = Assert.Throws<IntentionalAccessorException>(() => accessor.Get(target));

        Assert.Same(failure, exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-VALIDATION", "property-type-mismatch")]
    public void TypedAccessor_RejectsAPropertyTypeMismatchAtConstruction()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ReadOnlyProperty<AccessTarget, string>(Property<AccessTarget>(nameof(AccessTarget.Count))));

        Assert.Equal("property", exception.ParamName);
        Assert.Contains(typeof(int).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(string).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-VALIDATION", "unrelated-target-type")]
    public void TypedAccessor_RejectsAnUnrelatedTargetTypeAtConstruction()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ReadOnlyProperty<UnrelatedTarget>(Property<AccessTarget>(nameof(AccessTarget.Count))));

        Assert.Equal("property", exception.ParamName);
        Assert.Contains(typeof(UnrelatedTarget).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-VALIDATION", "indexed-property")]
    public void Accessor_RejectsAnIndexedPropertyAtConstruction()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ReadOnlyProperty(Property<IndexedTarget>("Item")));

        Assert.Equal("property", exception.ParamName);
        Assert.Contains("Indexed property", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-VALIDATION", "static-property")]
    public void Accessor_RejectsAStaticPropertyAtConstruction()
    {
        PropertyInfo property = typeof(StaticTarget).GetProperty(nameof(StaticTarget.Count), BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("The static test property must exist.");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ReadOnlyProperty(property));

        Assert.Equal("property", exception.ParamName);
        Assert.Contains("Static property", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-VALIDATION", "unknown-access-policy")]
    public void Accessor_RejectsAnUnknownAccessPolicyAtConstruction()
    {
        var policy = (PropertyAccessPolicy)42;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReadOnlyProperty<AccessTarget>(Property<AccessTarget>(nameof(AccessTarget.Count)), policy));

        Assert.Equal("accessPolicy", exception.ParamName);
        Assert.Equal(policy, exception.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-SURFACE", "method-owned-delegates")]
    public void AccessorSurface_DoesNotExposeItsImplementationDelegatesAsPublicFields()
    {
        Type[] accessorTypes =
        [
            typeof(ReadOnlyProperty),
            typeof(ReadOnlyProperty<>),
            typeof(ReadOnlyProperty<,>),
            typeof(ReadWriteProperty),
            typeof(ReadWriteProperty<>),
            typeof(ReadWriteProperty<,>),
        ];

        Assert.All(accessorTypes, type => Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)));
    }

    static PropertyInfo Property<T>(string name)
    {
        return typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The test property {typeof(T).Name}.{name} must exist.");
    }

    private class BaseTarget
    {
        public int Count { get; set; }
    }

    private sealed class DerivedTarget : BaseTarget
    {
    }

    private sealed class AccessTarget
    {
        private readonly Exception _getterFailure;
        private readonly Exception _setterFailure;

        public AccessTarget()
            : this(new IntentionalAccessorException("unused"), new IntentionalAccessorException("unused"))
        {
        }

        public AccessTarget(Exception getterFailure, Exception setterFailure)
        {
            _getterFailure = getterFailure;
            _setterFailure = setterFailure;
        }

        public int Count { get; set; }

        public string PrivateGetter { private get; set; } = "private-getter";

        public string ThrowingPrivateGetter
        {
            private get => throw _getterFailure;
            set { }
        }

        public string ThrowingPrivateSetter
        {
            get => string.Empty;
            private set => throw _setterFailure;
        }
    }

    private struct ValueTarget
    {
        public int Count { get; set; }
    }

    private sealed class UnrelatedTarget
    {
    }

    private sealed class IndexedTarget
    {
        public string this[int index] => index.ToString();
    }

    private static class StaticTarget
    {
        public static int Count { get; set; }
    }

    internal sealed class IntentionalAccessorException(string message) : Exception(message);
}
