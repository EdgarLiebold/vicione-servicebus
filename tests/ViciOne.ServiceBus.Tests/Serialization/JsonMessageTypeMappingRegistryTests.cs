using System.Text.Json;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization.JsonConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class JsonMessageTypeMappingRegistryTests
{
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
    [RequirementCoverage("REQ-VSB-JSON-TYPE-MAPPING", "rejects-nonconforming-implementations-at-registration")]
    public void Registration_RejectsImplementationsThatCannotRepresentTheContract()
    {
        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(GenericContract<>), typeof(UnrelatedImplementation<>))).ParamName);
        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(PairContract<,>), typeof(ReorderedPairImplementation<,>))).ParamName);
        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(GenericContract<>), typeof(AbstractGenericImplementation<>))).ParamName);
        Assert.Equal("implementationType", Assert.Throws<ArgumentException>(() =>
            JsonMessageTypeMappingRegistry.Register<ClosedContract, AbstractClosedImplementation>()).ParamName);
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

    public sealed class UnrelatedImplementation<T>;

    public interface PairContract<TFirst, TSecond>;

    public sealed class ReorderedPairImplementation<TFirst, TSecond> :
        PairContract<TSecond, TFirst>;

    public abstract class AbstractGenericImplementation<T> :
        GenericContract<T>
    {
        public abstract T? Value { get; }
    }

    public interface ClosedContract;

    public abstract class AbstractClosedImplementation :
        ClosedContract;
}
