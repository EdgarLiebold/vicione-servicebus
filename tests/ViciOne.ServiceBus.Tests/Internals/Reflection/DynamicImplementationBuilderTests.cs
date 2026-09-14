using System.Reflection;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Internals;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Internals.Reflection;

public sealed class DynamicImplementationBuilderTests
{
    private const TypeAttributes SerializableFlag = (TypeAttributes)0x00002000;

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "stable-concrete-collectible-type")]
    public void ValidInterface_ProducesOnePublicSealedCollectibleImplementation()
    {
        var builder = new DynamicImplementationBuilderTestDriver();

        Type first = builder.GetImplementationType(typeof(AttributedContract));
        Type second = builder.GetImplementationType(typeof(AttributedContract));
        object instance = Activator.CreateInstance(first)!;

        Assert.Same(first, second);
        Assert.False(first.IsInterface);
        Assert.True(first.IsPublic);
        Assert.True(first.IsSealed);
        Assert.True(first.Assembly.IsCollectible);
        Assert.True(typeof(AttributedContract).IsAssignableFrom(first));
        Assert.StartsWith("ViciOne.ServiceBus.DynamicInternal.", first.FullName, StringComparison.Ordinal);
        Assert.True(typeof(SerializableFlagProbe).Attributes.HasFlag(SerializableFlag));
        Assert.False(first.Attributes.HasFlag(SerializableFlag));
        Assert.IsAssignableFrom<AttributedContract>(instance);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "inherited-properties-init-and-attributes")]
    public void InheritedProperties_PreserveValuesInitMetadataAndCompleteAttributes()
    {
        var builder = new DynamicImplementationBuilderTestDriver();
        Type implementation = builder.GetImplementationType(typeof(AttributedContract));
        object instance = Activator.CreateInstance(implementation)!;
        PropertyInfo name = implementation.GetProperty(nameof(AttributedContract.Name))!;
        PropertyInfo count = implementation.GetProperty(nameof(AttributedContract.Count))!;
        PropertyInfo address = implementation.GetProperty(nameof(AttributedContract.Address))!;

        name.SetValue(instance, "A+");
        count.SetValue(instance, 27);
        address.SetValue(instance, new Uri("https://example.test/contracts/27"));
        var contract = Assert.IsAssignableFrom<AttributedContract>(instance);
        ContractMarkerAttribute marker = Assert.Single(name.GetCustomAttributes<ContractMarkerAttribute>());

        Assert.Equal("A+", contract.Name);
        Assert.Equal(27, contract.Count);
        Assert.Equal(new Uri("https://example.test/contracts/27"), contract.Address);
        Assert.Equal("primary", marker.Name);
        Assert.Equal(["first", "second"], marker.Aliases);
        Assert.Equal(["third", "fourth"], marker.AlternateAliases);
        Assert.Equal(ContractLevel.Critical, marker.Level);
        Assert.Equal(42, marker.Code);
        Assert.Equal([7, 9], marker.Codes);
        Assert.Contains(typeof(IsExternalInit), count.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
        Assert.NotNull(name.SetMethod);
        Assert.NotNull(address.SetMethod);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "parallel-cache-single-type")]
    public void ParallelRequestsForOneContract_ReturnTheSameImplementationType()
    {
        var builder = new DynamicImplementationBuilderTestDriver();
        var implementations = new Type[64];

        Parallel.For(0, implementations.Length,
            index => implementations[index] = builder.GetImplementationType(typeof(ParallelContract)));

        Assert.All(implementations, implementation => Assert.Same(implementations[0], implementation));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "compatible-inherited-property-merge")]
    public void CompatibleInheritedPropertyDeclarations_AreImplementedOnceForBothContracts()
    {
        var builder = new DynamicImplementationBuilderTestDriver();
        Type implementation = builder.GetImplementationType(typeof(CompatibleContract));
        object instance = Activator.CreateInstance(implementation)!;
        PropertyInfo property = Assert.Single(implementation.GetProperties(), candidate => candidate.Name == "Value");

        property.SetValue(instance, "shared");

        Assert.Equal("shared", Assert.IsAssignableFrom<FirstValueContract>(instance).Value);
        Assert.Equal("shared", Assert.IsAssignableFrom<SecondValueContract>(instance).Value);
    }

    [Theory]
    [MemberData(nameof(UnsupportedContractShapes))]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "unsupported-shape-validation")]
    public void UnsupportedContractShape_IsRejectedBeforeTypeEmission(Type contractType, string expectedReason)
    {
        var builder = new DynamicImplementationBuilderTestDriver();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.GetImplementationType(contractType));

        Assert.Equal("interfaceType", exception.ParamName);
        Assert.Contains(expectedReason, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "required-interface-type")]
    public void MissingInterfaceType_IsRejectedPrecisely()
    {
        var builder = new DynamicImplementationBuilderTestDriver();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder.GetImplementationType(null!));

        Assert.Equal("interfaceType", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-BUS-CONTRACT", "stable-valid-marker-type")]
    public void ValidBusMarker_ProducesOneAssignableImplementationType()
    {
        var builder = new DynamicImplementationBuilderTestDriver();

        Type first = builder.GetBusInstanceType(typeof(IContractBus));
        Type second = builder.GetBusInstanceType(typeof(IContractBus));

        Assert.Same(first, second);
        Assert.True(first.IsPublic);
        Assert.True(first.IsSealed);
        Assert.True(typeof(IContractBus).IsAssignableFrom(first));
        Assert.Equal(typeof(BusInstance<IContractBus>), first.BaseType);
        PropertyInfo label = first.GetProperty(nameof(IContractBus.Label))!;
        Assert.NotNull(label.GetMethod);
        Assert.NotNull(label.SetMethod);
        ConstructorInfo constructor = Assert.Single(first.GetConstructors());
        Assert.Equal([typeof(IBusControl)], constructor.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Theory]
    [MemberData(nameof(UnsupportedBusContractShapes))]
    [RequirementCoverage("REQ-VSB-DYNAMIC-BUS-CONTRACT", "unsupported-shape-validation")]
    public void UnsupportedBusContractShape_IsRejectedAtTheOwningBoundary(Type? contractType, Type exceptionType, string reason)
    {
        var builder = new DynamicImplementationBuilderTestDriver();

        Exception exception = Assert.Throws(exceptionType, () => builder.GetBusInstanceType(contractType!));

        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Equal("interfaceType", ((ArgumentException)exception).ParamName);
        Assert.Contains(reason, exception.Message, StringComparison.Ordinal);
    }

    public static TheoryData<Type, string> UnsupportedContractShapes => new()
    {
        { typeof(ConcreteContract), "interfaces" },
        { typeof(OpenContract<>), "closed" },
        { typeof(MethodContract), "methods" },
        { typeof(EventContract), "methods" },
        { typeof(IndexedContract), "indexers" },
        { typeof(SetterOnlyContract), "readable" },
        { typeof(DefaultImplementationContract), "default implementations" },
        { typeof(StaticPropertyContract), "instance" },
        { typeof(ConflictingContract), "conflicting" },
        { typeof(CaseConflictingContract), "case-insensitive" },
    };

    public static TheoryData<Type?, Type, string> UnsupportedBusContractShapes => new()
    {
        { null, typeof(ArgumentNullException), "interfaceType" },
        { typeof(ConcreteContract), typeof(ArgumentException), "interfaces" },
        { typeof(IGenericBus<>), typeof(ArgumentException), "generic" },
        { typeof(INotABus), typeof(ArgumentException), nameof(IBus) },
    };

    public interface BaseContract
    {
        [ContractMarker(
            "primary",
            "first",
            "second",
            AlternateAliases = ["third", "fourth"],
            Level = ContractLevel.Critical,
            Code = 42,
            Codes = [7, 9])]
        string Name { get; }
    }

    public interface AttributedContract : BaseContract
    {
        int Count { get; init; }

        Uri Address { get; }
    }

    public interface ParallelContract
    {
        int Value { get; }
    }

    public interface IContractBus : IBus
    {
        string Label { get; }
    }

    public interface IGenericBus<T> : IBus;

    public interface INotABus;

    public interface FirstValueContract
    {
        string Value { get; }
    }

    public interface SecondValueContract
    {
        string Value { get; }
    }

    public interface CompatibleContract : FirstValueContract, SecondValueContract;

    public sealed class ConcreteContract;

    public interface OpenContract<T>
    {
        T Value { get; }
    }

    public interface MethodContract
    {
        string Execute();
    }

    public interface EventContract
    {
        event EventHandler Changed;
    }

    public interface IndexedContract
    {
        string this[int index] { get; }
    }

    public interface SetterOnlyContract
    {
        string Value { set; }
    }

    public interface DefaultImplementationContract
    {
        string Value => "computed";
    }

    public interface StaticPropertyContract
    {
        static abstract string Value { get; }
    }

    public interface FirstConflictingContract
    {
        string Value { get; }
    }

    public interface SecondConflictingContract
    {
        int Value { get; }
    }

    public interface ConflictingContract : FirstConflictingContract, SecondConflictingContract;

    public interface UpperCaseContract
    {
        string Name { get; }
    }

    public interface LowerCaseContract
    {
        string name { get; }
    }

    public interface CaseConflictingContract : UpperCaseContract, LowerCaseContract;

    public enum ContractLevel
    {
        Standard,
        Critical,
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ContractMarkerAttribute(string name, params string[] aliases) : Attribute
    {
        public string Name { get; } = name;

        public string[] Aliases { get; } = aliases;

        public ContractLevel Level { get; set; }

        public string[] AlternateAliases { get; set; } = [];

        public int Code;

        public int[] Codes = [];
    }

    [Serializable]
    private sealed class SerializableFlagProbe;
}
