using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Internals;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Internals.Reflection;

public sealed class ReadWritePropertyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "public-getter-first-call")]
    public void ReadProperty_PublicGetterReturnsTheValueOnItsFirstCall()
    {
        var accessor = new ReadPropertyTestDriver<IRuntimeTarget, string>(Property<IRuntimeTarget>(nameof(IRuntimeTarget.Value)));
        IRuntimeTarget target = new RuntimeTarget { Value = "first-call" };

        string value = accessor.Get(target);

        Assert.Equal("first-call", value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "implementation-setter-first-call")]
    public void WriteProperty_ImplementationSetterWritesTheValueOnItsFirstCall()
    {
        var accessor = new WritePropertyTestDriver<IRuntimeTarget, string>(
            typeof(RuntimeTarget),
            Property<RuntimeTarget>(nameof(RuntimeTarget.Value)));
        IRuntimeTarget target = new RuntimeTarget();

        accessor.Set(target, "first-call");

        Assert.Equal("first-call", target.Value);
        Assert.Equal(typeof(RuntimeTarget), accessor.TargetType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "private-getter-reflection-exception")]
    public void ReadProperty_PrivateGetterPreservesTheOriginalException()
    {
        var failure = new IntentionalAccessorException("read");
        var accessor = new ReadPropertyTestDriver<RuntimeTarget, string>(Property<RuntimeTarget>(nameof(RuntimeTarget.ThrowingPrivateGetter)));
        var target = new RuntimeTarget(failure, new IntentionalAccessorException("unused"));

        IntentionalAccessorException exception = Assert.Throws<IntentionalAccessorException>(() => accessor.Get(target));

        Assert.Same(failure, exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "private-setter-reflection-exception")]
    public void WriteProperty_PrivateSetterPreservesTheOriginalException()
    {
        var failure = new IntentionalAccessorException("write");
        var accessor = new WritePropertyTestDriver<RuntimeTarget, string>(
            typeof(RuntimeTarget),
            Property<RuntimeTarget>(nameof(RuntimeTarget.ThrowingPrivateSetter)));
        var target = new RuntimeTarget(new IntentionalAccessorException("unused"), failure);

        IntentionalAccessorException exception = Assert.Throws<IntentionalAccessorException>(() => accessor.Set(target, "value"));

        Assert.Same(failure, exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "property-type-validation")]
    public void ReadProperty_RejectsAPropertyTypeMismatchAtConstruction()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ReadPropertyTestDriver<IRuntimeTarget, int>(Property<IRuntimeTarget>(nameof(IRuntimeTarget.Value))));

        Assert.Equal("propertyInfo", exception.ParamName);
        Assert.Contains(typeof(string).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(int).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "implementation-type-validation")]
    public void WriteProperty_RejectsAnUnrelatedImplementationTypeAtConstruction()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new WritePropertyTestDriver<IRuntimeTarget, string>(
                typeof(UnrelatedTarget),
                Property<RuntimeTarget>(nameof(RuntimeTarget.Value))));

        Assert.Equal("implementationType", exception.ParamName);
        Assert.Contains(typeof(UnrelatedTarget).ToString(), exception.Message, StringComparison.Ordinal);
    }

    static PropertyInfo Property<T>(string name)
    {
        return typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The test property {typeof(T).Name}.{name} must exist.");
    }

    private interface IRuntimeTarget
    {
        string Value { get; set; }
    }

    private sealed class RuntimeTarget : IRuntimeTarget
    {
        private readonly Exception _getterFailure;
        private readonly Exception _setterFailure;

        public RuntimeTarget()
            : this(new IntentionalAccessorException("unused"), new IntentionalAccessorException("unused"))
        {
        }

        public RuntimeTarget(Exception getterFailure, Exception setterFailure)
        {
            _getterFailure = getterFailure;
            _setterFailure = setterFailure;
        }

        public string Value { get; set; } = string.Empty;

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

    private sealed class UnrelatedTarget
    {
    }

    private sealed class IntentionalAccessorException(string message) : Exception(message);
}
