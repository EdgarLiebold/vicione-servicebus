using System.Text.Json;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization.Json.Converters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class JsonMessageTypeMappingRegistryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-MAPPING", "valid-closed-mapping-round-trips-concrete-representation")]
    public void ClosedMapping_DeserializesAndSerializesThroughTheConcreteRepresentation()
    {
        JsonMessageTypeMappingRegistry.Register<ClosedMappedContract, ClosedMappedImplementation>();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SystemTextJsonConverterFactory());

        ClosedMappedContract? deserialized = JsonSerializer.Deserialize<ClosedMappedContract>("{\"Value\":42}", options);
        string serialized = JsonSerializer.Serialize<ClosedMappedContract>(new ClosedMappedImplementation { Value = 73 }, options);

        ClosedMappedImplementation implementation = Assert.IsType<ClosedMappedImplementation>(deserialized);
        Assert.Equal(42, implementation.Value);
        Assert.Equal("{\"Value\":73}", serialized);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-MAPPING", "valid-open-mapping-round-trips-concrete-representation")]
    public void OpenGenericMapping_DeserializesAndSerializesThroughTheConcreteRepresentation()
    {
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(GenericContract<>), typeof(GenericImplementation<>));
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SystemTextJsonConverterFactory());

        GenericContract<int>? deserialized = JsonSerializer.Deserialize<GenericContract<int>>("{\"Value\":42}", options);
        string serialized = JsonSerializer.Serialize<GenericContract<int>>(new GenericImplementation<int> { Value = 73 }, options);

        GenericImplementation<int> implementation = Assert.IsType<GenericImplementation<int>>(deserialized);
        Assert.Equal(42, implementation.Value);
        Assert.Equal("{\"Value\":73}", serialized);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-MAPPING", "open-base-contract-mapping-supports-indirect-matching-inheritance")]
    public void OpenBaseContractMapping_RoundTripsAnIndirectMatchingImplementation()
    {
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(
            typeof(GenericBaseContract<>),
            typeof(DerivedBaseImplementation<>));
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SystemTextJsonConverterFactory());

        GenericBaseContract<int>? deserialized = JsonSerializer.Deserialize<GenericBaseContract<int>>("{\"Value\":42}", options);
        string serialized = JsonSerializer.Serialize<GenericBaseContract<int>>(
            new DerivedBaseImplementation<int> { Value = 73 },
            options);

        DerivedBaseImplementation<int> implementation = Assert.IsType<DerivedBaseImplementation<int>>(deserialized);
        Assert.Equal(42, implementation.Value);
        Assert.Equal("{\"Value\":73}", serialized);

        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(
                typeof(PairBaseContract<,>),
                typeof(ReorderedPairBaseImplementation<,>))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-MAPPING", "rejects-nonconforming-implementations-at-registration")]
    public void Registration_RejectsImplementationsThatCannotRepresentTheContract()
    {
        var factory = new SystemTextJsonConverterFactory();

        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(GenericContract<>), typeof(UnrelatedImplementation<>))).ParamName);
        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(PairContract<,>), typeof(ReorderedPairImplementation<,>))).ParamName);
        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(GenericContract<>), typeof(AbstractGenericImplementation<>))).ParamName);
        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.Register<ClosedContract, AbstractClosedImplementation>()).ParamName);
        Assert.Equal("typeToConvert", Assert.Throws<ArgumentNullException>(() => factory.CanConvert(null!)).ParamName);
        Assert.Equal("typeToConvert", Assert.Throws<ArgumentNullException>(() => factory.CreateConverter(null!, new JsonSerializerOptions())).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() => factory.CreateConverter(typeof(GenericContract<int>), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-MAPPING", "open-registration-validates-every-type-shape")]
    public void OpenRegistration_RejectsMissingClosedAndMismatchedArityTypes()
    {
        Assert.Equal("contractType", Assert.Throws<ArgumentNullException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(null!, typeof(ShapeImplementation<>))).ParamName);
        Assert.Equal("implementationType", Assert.Throws<ArgumentNullException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(ShapeContract<>), null!)).ParamName);

        ArgumentException closedContract = Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(ShapeContract<int>), typeof(ShapeImplementation<>)));
        ArgumentException closedImplementation = Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(ShapeContract<>), typeof(ShapeImplementation<int>)));
        ArgumentException mismatchedArity = Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(ShapeContract<>), typeof(ThreeArgumentImplementation<,,>)));

        Assert.Contains("open generic type definitions", closedContract.Message, StringComparison.Ordinal);
        Assert.Contains("open generic type definitions", closedImplementation.Message, StringComparison.Ordinal);
        Assert.Contains("same generic arity", mismatchedArity.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-MAPPING", "registration-is-idempotent-and-rejects-conflicts")]
    public void Registration_AcceptsTheSameMappingAgainAndRejectsAConflictingMapping()
    {
        JsonMessageTypeMappingRegistry.Register<ConflictingContract, FirstConflictingImplementation>();
        JsonMessageTypeMappingRegistry.Register<ConflictingContract, FirstConflictingImplementation>();

        InvalidOperationException closedConflict = Assert.Throws<InvalidOperationException>(() =>
            JsonMessageTypeMappingRegistry.Register<ConflictingContract, SecondConflictingImplementation>());

        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(ConflictingGenericContract<>), typeof(FirstConflictingGenericImplementation<>));
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(ConflictingGenericContract<>), typeof(FirstConflictingGenericImplementation<>));

        InvalidOperationException openConflict = Assert.Throws<InvalidOperationException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(
                typeof(ConflictingGenericContract<>),
                typeof(SecondConflictingGenericImplementation<>)));

        Assert.Contains(nameof(ConflictingContract), closedConflict.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ConflictingGenericContract<int>).Split('`')[0], openConflict.Message, StringComparison.Ordinal);
    }

    public interface GenericContract<T>
    {
        T? Value { get; }
    }

    public sealed class GenericImplementation<T> :
        GenericContract<T>
    {
        public T? Value { get; set; }
    }

    public abstract class GenericBaseContract<T>
    {
        public abstract T? Value { get; set; }
    }

    public abstract class IntermediateBaseImplementation<T> : GenericBaseContract<T>;

    public sealed class DerivedBaseImplementation<T> : IntermediateBaseImplementation<T>
    {
        public override T? Value { get; set; }
    }

    public sealed class UnrelatedImplementation<T>;

    public interface PairContract<TFirst, TSecond>;

    public sealed class ReorderedPairImplementation<TFirst, TSecond> :
        PairContract<TSecond, TFirst>;

    public abstract class PairBaseContract<TFirst, TSecond>;

    public sealed class ReorderedPairBaseImplementation<TFirst, TSecond> :
        PairBaseContract<TSecond, TFirst>;

    public abstract class AbstractGenericImplementation<T> :
        GenericContract<T>
    {
        public abstract T? Value { get; }
    }

    public interface ClosedContract;

    public abstract class AbstractClosedImplementation :
        ClosedContract;

    public interface ClosedMappedContract
    {
        int Value { get; }
    }

    public sealed class ClosedMappedImplementation :
        ClosedMappedContract
    {
        public int Value { get; set; }
    }

    public interface ShapeContract<T>;

    public sealed class ShapeImplementation<T> :
        ShapeContract<T>;

    public sealed class ThreeArgumentImplementation<TFirst, TSecond, TThird>;

    public interface ConflictingContract;

    public sealed class FirstConflictingImplementation :
        ConflictingContract;

    public sealed class SecondConflictingImplementation :
        ConflictingContract;

    public interface ConflictingGenericContract<T>;

    public sealed class FirstConflictingGenericImplementation<T> :
        ConflictingGenericContract<T>;

    public sealed class SecondConflictingGenericImplementation<T> :
        ConflictingGenericContract<T>;
}
