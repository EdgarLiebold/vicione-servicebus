using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Initializers.Variables;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderFactoryContractMatrixTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-FACTORY", "task-converter-bidirectional-matrix")]
    public async Task TaskConverters_WrapAwaitAndConvertInBothDirectionsAsync()
    {
        var factory = new PropertyProviderFactory<FactoryInput>();
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;

        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<Task<int>, int>? wrapExact));
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<Task<long>, int>? wrapConverted));
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<int, Task<int>>? awaitExact));
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<long, Task<int>>? awaitConverted));

        Task<int>? exactTask = await wrapExact.ConvertAsync(context, 27, token);
        Task<long>? convertedTask = await wrapConverted.ConvertAsync(context, 28, token);
        Assert.NotNull(exactTask);
        Assert.NotNull(convertedTask);
        Assert.Equal(27, await exactTask);
        Assert.Equal(28L, await convertedTask);
        Assert.Equal(29, await awaitExact.ConvertAsync(context, Task.FromResult(29), token));
        Assert.Equal(30L, await awaitConverted.ConvertAsync(context, Task.FromResult(30), token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-FACTORY", "message-data-source-and-preservation-matrix")]
    public async Task MessageDataProviders_HandleBuiltInObjectAndExistingValueRoutesAsync()
    {
        var payload = new Payload("payload");
        MessageData<string> existingText = new PutMessageData<string>("existing");
        MessageData<Payload> existingPayload = new PutMessageData<Payload>(payload);
        var input = new MessageDataInput("text", [1, 2, 3], Stream.Null, payload, existingText, existingPayload);
        var reader = PropertyProviderTestContext.For(input);

        MessageData<byte[]> fromText = await reader.ReadAsync<MessageData<byte[]>>(nameof(MessageDataInput.Text));
        MessageData<byte[]> fromBytes = await reader.ReadAsync<MessageData<byte[]>>(nameof(MessageDataInput.Bytes));
        MessageData<Stream> fromStream = await reader.ReadAsync<MessageData<Stream>>(nameof(MessageDataInput.Stream));
        MessageData<string> fromString = await reader.ReadAsync<MessageData<string>>(nameof(MessageDataInput.Text));
        MessageData<Payload> fromPayload = await reader.ReadAsync<MessageData<Payload>>(nameof(MessageDataInput.Payload));
        MessageData<string> preservedText = await reader.ReadAsync<MessageData<string>>(nameof(MessageDataInput.ExistingText));
        MessageData<Payload> preservedPayload = await reader.ReadAsync<MessageData<Payload>>(nameof(MessageDataInput.ExistingPayload));

        Assert.Equal("text", System.Text.Encoding.UTF8.GetString(Assert.IsType<byte[]>(await fromText.Value)));
        Assert.Equal([1, 2, 3], await fromBytes.Value);
        Assert.Same(Stream.Null, await fromStream.Value);
        Assert.Equal("text", await fromString.Value);
        Assert.Same(payload, await fromPayload.Value);
        Assert.Same(existingText, preservedText);
        Assert.Same(existingPayload, preservedPayload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-FACTORY", "named-value-direct-and-converted-matrix")]
    public async Task NamedInitializerValues_ExposeTheirNameDirectlyAndThroughScalarConversionAsync()
    {
        var reader = PropertyProviderTestContext.For(new NamedValueInput(new NamedValue("42")));

        string name = await reader.ReadAsync<string>(nameof(NamedValueInput.Value));
        int number = await reader.ReadAsync<int>(nameof(NamedValueInput.Value));

        Assert.Equal("42", name);
        Assert.Equal(42, number);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-FACTORY", "nullable-variable-collection-and-object-converter-matrix")]
    public void ConverterSelection_CoversEveryCompositeSourceAndResultFamily()
    {
        var factory = new PropertyProviderFactory<FactoryInput>();

        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<int?, int>? toNullable));
        Assert.IsType<ToNullablePropertyConverter<int>>(toNullable);
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<long?, int>? convertedToNullable));
        Assert.IsType<TypePropertyConverter<long?, int>>(convertedToNullable);
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<int, int?>? fromNullable));
        Assert.IsType<FromNullablePropertyConverter<int>>(fromNullable);
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<long, int?>? convertedFromNullable));
        Assert.IsType<FromNullablePropertyConverter<long, int>>(convertedFromNullable);

        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<Guid, IdVariable>? variable));
        Assert.IsType<VariablePropertyConverter<Guid, IdVariable>>(variable);
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<string, IdVariable>? convertedVariable));
        Assert.IsType<VariablePropertyConverter<string, IdVariable, Guid>>(convertedVariable);
        Assert.False(factory.TryGetPropertyConverter(out IPropertyConverter<DateOnly, IdVariable>? unsupportedVariable));
        Assert.Null(unsupportedVariable);

        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<string, NamedValue>? named));
        Assert.IsType<NamedInitializerValuePropertyConverter<NamedValue>>(named);
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<int, NamedValue>? convertedNamed));
        Assert.IsType<NamedInitializerValuePropertyConverter<int, NamedValue>>(convertedNamed);
        Assert.False(factory.TryGetPropertyConverter(out IPropertyConverter<DateOnly, NamedValue>? unsupportedNamed));
        Assert.Null(unsupportedNamed);

        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<long[], int[]>? convertedArray));
        Assert.IsType<ArrayPropertyConverter<long, int>>(convertedArray);
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<List<int>, int[]>? exactList));
        Assert.IsType<ListPropertyConverter<int>>(exactList);
        Assert.True(factory.TryGetPropertyConverter(out IPropertyConverter<List<long>, int[]>? convertedList));
        Assert.IsType<ListPropertyConverter<long, int>>(convertedList);
        Assert.False(factory.TryGetPropertyConverter(out IPropertyConverter<DateOnly, int[]>? unsupportedArray));
        Assert.Null(unsupportedArray);

        Assert.True(factory.TryGetPropertyConverter(
            out IPropertyConverter<IReadOnlyDictionary<string, string>, IDictionary<string, string>>? exactDictionary));
        Assert.IsType<DictionaryPropertyConverter<string, string>>(exactDictionary);
        Assert.True(factory.TryGetPropertyConverter(
            out IPropertyConverter<IDictionary<int, string>, IDictionary<string, string>>? convertedKeys));
        Assert.IsType<DictionaryKeyPropertyConverter<int, string, string>>(convertedKeys);
        Assert.True(factory.TryGetPropertyConverter(
            out IPropertyConverter<IDictionary<string, object>, IDictionary<string, string>>? convertedValues));
        Assert.IsType<DictionaryPropertyConverter<string, object, string>>(convertedValues);
        Assert.True(factory.TryGetPropertyConverter(
            out IPropertyConverter<IDictionary<int, object>, IDictionary<string, string>>? convertedKeysAndValues));
        Assert.IsType<DictionaryPropertyConverter<int, object, string, string>>(convertedKeysAndValues);
        Assert.True(factory.TryGetPropertyConverter(
            out IPropertyConverter<DictionaryContract, IDictionary<string, object>>? objectGraph));
        Assert.IsType<InitializePropertyConverter<DictionaryContract, IDictionary<string, object>>>(objectGraph);
        Assert.False(factory.TryGetPropertyConverter(
            out IPropertyConverter<DateOnly, IDictionary<string, string>>? unsupportedDictionary));
        Assert.Null(unsupportedDictionary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-FACTORY", "concrete-dictionary-object-graph")]
    public async Task DictionaryObjectGraphs_InitializeValidConcreteMessagePropertiesAsync()
    {
        var input = new ConcreteGraphInput(new Dictionary<string, object>
        {
            [nameof(ConcreteDictionaryContract.Id)] = 47,
            [nameof(ConcreteDictionaryContract.Name)] = "concrete",
        });
        var reader = PropertyProviderTestContext.For(input);

        ConcreteDictionaryContract value = await reader.ReadAsync<ConcreteDictionaryContract>(nameof(ConcreteGraphInput.Value));

        Assert.Equal(47, value.Id);
        Assert.Equal("concrete", value.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-FACTORY", "unsupported-shapes-and-null-property-boundary")]
    public void UnsupportedFactoryRoutes_ReturnFalseAndMissingMetadataFailsAtTheBoundary()
    {
        var factory = new PropertyProviderFactory<FactoryInput>();
        var messageDataFactory = new PropertyProviderFactory<IntInput>();
        var arrayFactory = new PropertyProviderFactory<ArrayInput>();
        var dictionaryFactory = new PropertyProviderFactory<DictionaryInput>();
        var valueProperty = typeof(FactoryInput).GetProperty(nameof(FactoryInput.Value))!;
        var intProperty = typeof(IntInput).GetProperty(nameof(IntInput.Value))!;
        var arrayProperty = typeof(ArrayInput).GetProperty(nameof(ArrayInput.Values))!;
        var dictionaryProperty = typeof(DictionaryInput).GetProperty(nameof(DictionaryInput.Values))!;

        Assert.False(factory.TryGetPropertyProvider(valueProperty, out IPropertyProvider<FactoryInput, DateOnly>? unsupported));
        Assert.Null(unsupported);
        Assert.False(messageDataFactory.TryGetPropertyProvider(intProperty, out IPropertyProvider<IntInput, MessageData<int>>? unsupportedData));
        Assert.Null(unsupportedData);
        Assert.False(arrayFactory.TryGetPropertyProvider(arrayProperty,
            out IPropertyProvider<ArrayInput, IDictionary<int, int>>? unsupportedArray));
        Assert.Null(unsupportedArray);
        Assert.False(dictionaryFactory.TryGetPropertyProvider(dictionaryProperty,
            out IPropertyProvider<DictionaryInput, int[]>? unsupportedDictionary));
        Assert.Null(unsupportedDictionary);
        Assert.False(factory.TryGetPropertyConverter(out IPropertyConverter<DateOnly, FactoryValue>? unsupportedConverter));
        Assert.Null(unsupportedConverter);
        Assert.Equal("propertyInfo", Assert.Throws<ArgumentNullException>(() =>
            factory.TryGetPropertyProvider<string>(null!, out _)).ParamName);
    }

    static InitializeContext<TestMessage> CreateContext() =>
        new BaseInitializeContext(TestContext.Current.CancellationToken).CreateMessageContext(new TestMessage());

    public sealed record Payload(string Value);

    public interface DictionaryContract
    {
        int Id { get; }
    }

    public sealed class ConcreteDictionaryContract
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    private sealed record ConcreteGraphInput(IDictionary<string, object> Value);

    private sealed record MessageDataInput(
        string Text,
        byte[] Bytes,
        Stream Stream,
        Payload Payload,
        MessageData<string> ExistingText,
        MessageData<Payload> ExistingPayload);

    private sealed record NamedValue(string Name) : INamedInitializerValue;

    private sealed record NamedValueInput(NamedValue Value);

    private sealed record FactoryInput(FactoryValue Value);

    private sealed record FactoryValue(string Text);

    private sealed record IntInput(int Value);

    private sealed record ArrayInput(int[] Values);

    private sealed record DictionaryInput(IDictionary<int, int> Values);

    private sealed class TestMessage;
}
