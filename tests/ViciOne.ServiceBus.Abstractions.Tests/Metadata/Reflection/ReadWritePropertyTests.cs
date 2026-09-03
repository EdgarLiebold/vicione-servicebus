namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata.Reflection;

using System.Reflection;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;


public sealed class ReadWritePropertyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "public-typed-read-write")]
    public void TypedAccessor_WritesAndReadsAPublicProperty()
    {
        var accessor = new ReadWriteProperty<AccessTarget, int>(Property<AccessTarget>(nameof(AccessTarget.Count)));
        var target = new AccessTarget();

        accessor.Set(target, 37);

        Assert.Equal(37, target.Count);
        Assert.Equal(37, accessor.Get(target));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-POLICY", "public-only-rejects-private-setter")]
    public void PublicOnly_PrivateSetterFailsWithoutChangingTheTarget()
    {
        var accessor = new ReadWriteProperty<AccessTarget>(Property<AccessTarget>(nameof(AccessTarget.PrivateSetter)));
        var target = new AccessTarget();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => accessor.Set(target, "changed"));

        Assert.Equal("No eligible setter is available for AccessTarget.PrivateSetter.", exception.Message);
        Assert.Equal("initial", target.PrivateSetter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-POLICY", "include-non-public-private-setter")]
    public void IncludeNonPublic_PrivateSetterWritesAndReadsItsValue()
    {
        var accessor = new ReadWriteProperty<AccessTarget>(
            Property<AccessTarget>(nameof(AccessTarget.PrivateSetter)),
            PropertyAccessPolicy.IncludeNonPublic);
        var target = new AccessTarget();

        accessor.Set(target, "changed");

        Assert.Equal("changed", target.PrivateSetter);
        Assert.Equal("changed", accessor.Get(target));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-FALLBACK", "private-setter-preserves-exception")]
    public void ReflectionFallback_PrivateSetterPreservesTheOriginalException()
    {
        var failure = new IntentionalAccessorException("setter");
        var target = new AccessTarget(failure);
        var accessor = new ReadWriteProperty<AccessTarget>(
            Property<AccessTarget>(nameof(AccessTarget.ThrowingPrivateSetter)),
            PropertyAccessPolicy.IncludeNonPublic);

        IntentionalAccessorException exception = Assert.Throws<IntentionalAccessorException>(() => accessor.Set(target, "value"));

        Assert.Same(failure, exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "boxed-value-type-reflection-path")]
    public void UntypedAccessor_MutatesABoxedValueType()
    {
        var accessor = new ReadWriteProperty(Property<ValueTarget>(nameof(ValueTarget.Count)));
        object target = new ValueTarget { Count = 3 };

        accessor.Set(target, 41);

        Assert.Equal(41, accessor.Get(target));
        Assert.Equal(41, ((ValueTarget)target).Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "nullable-reference-value")]
    public void UntypedAccessor_AcceptsNullForANullableReferenceProperty()
    {
        var accessor = new ReadWriteProperty(Property<AccessTarget>(nameof(AccessTarget.OptionalText)));
        var target = new AccessTarget { OptionalText = "present" };

        accessor.Set(target, null);

        Assert.Null(target.OptionalText);
        Assert.Null(accessor.Get(target));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "null-value-type-rejected")]
    public void UntypedAccessor_RejectsNullForANonNullableValueProperty()
    {
        var accessor = new ReadWriteProperty(Property<AccessTarget>(nameof(AccessTarget.Count)));
        var target = new AccessTarget { Count = 17 };

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => accessor.Set(target, null));

        Assert.Equal("value", exception.ParamName);
        Assert.Contains("AccessTarget.Count", exception.Message, StringComparison.Ordinal);
        Assert.Equal(17, target.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "incompatible-value-type-rejected")]
    public void UntypedAccessor_RejectsAConvertibleButIncompatibleValueType()
    {
        var accessor = new ReadWriteProperty(Property<AccessTarget>(nameof(AccessTarget.Count)));
        var target = new AccessTarget { Count = 23 };

        ArgumentException exception = Assert.Throws<ArgumentException>(() => accessor.Set(target, 24L));

        Assert.Equal("value", exception.ParamName);
        Assert.Contains(typeof(long).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(int).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal(23, target.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-FALLBACK", "private-value-setter-validates-null")]
    public void ReflectionFallback_RejectsNullBeforeInvokingAPrivateValueSetter()
    {
        var accessor = new ReadWriteProperty<AccessTarget>(
            Property<AccessTarget>(nameof(AccessTarget.PrivateCount)),
            PropertyAccessPolicy.IncludeNonPublic);
        var target = new AccessTarget();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => accessor.Set(target, null));

        Assert.Equal("value", exception.ParamName);
        Assert.Equal(5, target.PrivateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "missing-setter")]
    public void ReadOnlyProperty_SetFailsWithAnExactAccessorError()
    {
        var accessor = new ReadWriteProperty<AccessTarget>(Property<AccessTarget>(nameof(AccessTarget.ReadOnly)));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => accessor.Set(new AccessTarget(), "value"));

        Assert.Equal("No eligible setter is available for AccessTarget.ReadOnly.", exception.Message);
    }

    static PropertyInfo Property<T>(string name)
    {
        return typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The test property {typeof(T).Name}.{name} must exist.");
    }

    private sealed class AccessTarget
    {
        private readonly Exception _setterFailure;

        public AccessTarget()
            : this(new IntentionalAccessorException("unused"))
        {
        }

        public AccessTarget(Exception setterFailure)
        {
            _setterFailure = setterFailure;
        }

        public int Count { get; set; }

        public int PrivateCount { get; private set; } = 5;

        public string PrivateSetter { get; private set; } = "initial";

        public string? OptionalText { get; set; }

        public string ReadOnly => "read-only";

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

    private sealed class IntentionalAccessorException(string message) : Exception(message);
}
