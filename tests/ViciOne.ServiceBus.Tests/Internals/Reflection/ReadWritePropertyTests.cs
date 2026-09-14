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

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "required-constructor-metadata")]
    public void AccessorConstructors_RejectMissingMetadataAtTheirBoundary()
    {
        ArgumentNullException readException = Assert.Throws<ArgumentNullException>(() =>
            new ReadPropertyTestDriver<RuntimeTarget, string>(null!));
        ArgumentNullException implementationException = Assert.Throws<ArgumentNullException>(() =>
            new WritePropertyTestDriver<RuntimeTarget, string>(null!, Property<RuntimeTarget>(nameof(RuntimeTarget.Value))));
        ArgumentNullException writeException = Assert.Throws<ArgumentNullException>(() =>
            new WritePropertyTestDriver<RuntimeTarget, string>(typeof(RuntimeTarget), null!));

        Assert.Equal("propertyInfo", readException.ParamName);
        Assert.Equal("implementationType", implementationException.ParamName);
        Assert.Equal("propertyInfo", writeException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "instance-property-shape")]
    public void Accessors_RejectUnrelatedStaticAndIndexedPropertiesAtConstruction()
    {
        PropertyInfo unrelated = Property<UnrelatedTarget>(nameof(UnrelatedTarget.Value));
        PropertyInfo staticProperty = Property<RuntimeTarget>(nameof(RuntimeTarget.StaticValue));
        PropertyInfo indexer = Property<RuntimeTarget>("Item");

        ArgumentException unrelatedRead = Assert.Throws<ArgumentException>(() =>
            new ReadPropertyTestDriver<RuntimeTarget, string>(unrelated));
        ArgumentException unrelatedWrite = Assert.Throws<ArgumentException>(() =>
            new WritePropertyTestDriver<RuntimeTarget, string>(typeof(RuntimeTarget), unrelated));
        ArgumentException staticRead = Assert.Throws<ArgumentException>(() =>
            new ReadPropertyTestDriver<RuntimeTarget, string>(staticProperty));
        ArgumentException staticWrite = Assert.Throws<ArgumentException>(() =>
            new WritePropertyTestDriver<RuntimeTarget, string>(typeof(RuntimeTarget), staticProperty));
        ArgumentException indexedRead = Assert.Throws<ArgumentException>(() =>
            new ReadPropertyTestDriver<RuntimeTarget, string>(indexer));
        ArgumentException indexedWrite = Assert.Throws<ArgumentException>(() =>
            new WritePropertyTestDriver<RuntimeTarget, string>(typeof(RuntimeTarget), indexer));

        Assert.All(
            [unrelatedRead, unrelatedWrite, staticRead, staticWrite, indexedRead, indexedWrite],
            exception => Assert.Equal("propertyInfo", exception.ParamName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "required-instance")]
    public void Accessors_RejectANullRuntimeInstanceBeforeInvocation()
    {
        var read = new ReadPropertyTestDriver<RuntimeTarget, string>(Property<RuntimeTarget>(nameof(RuntimeTarget.Value)));
        var write = new WritePropertyTestDriver<RuntimeTarget, string>(
            typeof(RuntimeTarget),
            Property<RuntimeTarget>(nameof(RuntimeTarget.Value)));

        ArgumentNullException readException = Assert.Throws<ArgumentNullException>(() => read.Get(null!));
        ArgumentNullException writeException = Assert.Throws<ArgumentNullException>(() => write.Set(null!, "value"));

        Assert.Equal("content", readException.ParamName);
        Assert.Equal("content", writeException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "required-accessor-shape")]
    public void Accessors_RejectPropertiesWithoutTheirRequiredAccessor()
    {
        ArgumentException readException = Assert.Throws<ArgumentException>(() =>
            new ReadPropertyTestDriver<RuntimeTarget, string>(Property<RuntimeTarget>(nameof(RuntimeTarget.WriteOnly))));
        ArgumentException writeException = Assert.Throws<ArgumentException>(() =>
            new WritePropertyTestDriver<RuntimeTarget, string>(
                typeof(RuntimeTarget),
                Property<RuntimeTarget>(nameof(RuntimeTarget.ReadOnly))));

        Assert.Equal("propertyInfo", readException.ParamName);
        Assert.Equal("propertyInfo", writeException.ParamName);
        Assert.Contains("getter", readException.Message, StringComparison.Ordinal);
        Assert.Contains("setter", writeException.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "nonpublic-accessor-success")]
    public void NonPublicAccessors_ReadAndWriteWithoutChangingTheValue()
    {
        var read = new ReadPropertyTestDriver<RuntimeTarget, string>(
            Property<RuntimeTarget>(nameof(RuntimeTarget.PrivateGetterValue)));
        var write = new WritePropertyTestDriver<RuntimeTarget, string>(
            typeof(RuntimeTarget),
            Property<RuntimeTarget>(nameof(RuntimeTarget.PrivateSetterValue)));
        var target = new RuntimeTarget();
        target.SetPrivateGetterValue("read-value");

        write.Set(target, "write-value");

        Assert.Equal("read-value", read.Get(target));
        Assert.Equal("write-value", target.PrivateSetterValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-ACCESSOR", "write-property-type-validation")]
    public void WriteProperty_RejectsAPropertyTypeMismatchAtConstruction()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new WritePropertyTestDriver<RuntimeTarget, int>(
                typeof(RuntimeTarget),
                Property<RuntimeTarget>(nameof(RuntimeTarget.Value))));

        Assert.Equal("propertyInfo", exception.ParamName);
        Assert.Contains(typeof(string).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(int).ToString(), exception.Message, StringComparison.Ordinal);
    }

    static PropertyInfo Property<T>(string name)
    {
        return typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
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
        private string _privateGetterValue = string.Empty;

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

        public static string StaticValue { get; set; } = string.Empty;

        public string this[int index]
        {
            get => index.ToString();
            set { }
        }

        public string ReadOnly => string.Empty;

        public string WriteOnly
        {
            set { }
        }

        public string PrivateGetterValue
        {
            private get => _privateGetterValue;
            set => _privateGetterValue = value;
        }

        public string PrivateSetterValue { get; private set; } = string.Empty;

        public void SetPrivateGetterValue(string value) => PrivateGetterValue = value;

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
        public string Value { get; set; } = string.Empty;
    }

    private sealed class IntentionalAccessorException(string message) : Exception(message);
}
